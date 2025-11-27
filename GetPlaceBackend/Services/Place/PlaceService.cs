using GetPlaceBackend.Dto.Block;
using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Dto.Reservation;
using GetPlaceBackend.Dto.UserAccess;
using GetPlaceBackend.Models;
using Microsoft.AspNetCore.Routing.Constraints;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GetPlaceBackend.Services.Place;

public class PlaceService : IPlaceService
{
    private readonly IMongoCollection<GroupModel> groupsCollection;
    private readonly IMongoCollection<PlaceModel> placesCollection;
    private readonly IMongoCollection<UserModel> usersCollection;

    public PlaceService(IMongoDatabase db)
    {
        groupsCollection = db.GetCollection<GroupModel>("groups");
        placesCollection = db.GetCollection<PlaceModel>("places");
        usersCollection = db.GetCollection<UserModel>("users");
    }

    private async Task<PlaceModel> GetPlaceById(string placeShortId)
    {
        var place = await placesCollection
            .Find(p => p.PlaceShortId == placeShortId && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (place == null)
            throw new KeyNotFoundException($"Place: {placeShortId} не найден");

        return place;
    }
    
    private string GenerateShortId(int length = 4)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        return new string(Enumerable
            .Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    private Grid GetGridById(ObjectId gridId, PlaceModel place)
    {
        var grid = place.Grids.FirstOrDefault(g => g.GridId == gridId);
        
        if (grid == null)
            throw new KeyNotFoundException($"Grid {gridId} не найден");

        return grid;
    }

    private Block GetBlockById(ObjectId blockId, Grid grid)
    {
        var block = grid.Blocks.FirstOrDefault(b => b.BlockId == blockId);
        if (block == null)
            throw new KeyNotFoundException($"Block {blockId} не найден");
        return block;
    }
    
    private ObjectId? CheckIntersectionBlocks(Block block, Grid grid)
    {
        foreach (var existing in grid.Blocks)
        {
            var noOverlap =
                block.RightBottomX <= existing.LeftTopX || // new слева
                block.LeftTopX >= existing.RightBottomX || // new справа
                block.RightBottomY <= existing.LeftTopY || // new сверху
                block.LeftTopY >= existing.RightBottomY;   // new снизу

            if (!noOverlap)
                return existing.BlockId;
        }

        return null;
    }
    
    public async Task<List<CardPlaceDto>> GetPlacesByGroupIdAsync(ObjectId groupId)
    {
        // Фильтруем плейсы, в которых есть этот groupId
        var places = await placesCollection
            .Find(p => p.GroupIds.Contains(groupId) && !p.IsDeleted)
            .ToListAsync();

        if (places.Count == 0)
            return [];

        // Получаем имена Групп, на которые ссылаются Place
        var allGroupIds = places
            .SelectMany(p => p.GroupIds)
            .Distinct()
            .ToList();

        var groups = await groupsCollection
            .Find(g => allGroupIds.Contains(g.GroupId) && !g.IsDeleted)
            .ToListAsync();

        // Преобразуем в словарь для быстрого доступа
        var groupNames = groups.ToDictionary(
            g => g.GroupId,
            g => g.Name
        );

        // Маппинг Place → CardPlaceDto
        var result = places.Select(place => new CardPlaceDto
        {
            Color = place.Color,
            Name = place.Name,
            Description = place.Description,
            GroupNames = place.GroupIds
                .Where(id => groupNames.ContainsKey(id))
                .Select(id => groupNames[id])
                .ToList()
        }).ToList();

        return result;
    }
    
    public async Task<PlaceAccessDto?> GetPlaceAccessAsync(string placeShortId)
    {
        var place = await GetPlaceById(placeShortId);

        return new PlaceAccessDto
        {
            Id = place.PlaceShortId,
            Visible = place.Visible,
            Editable = place.Editable,
            Reservable = place.Reservable
        };
    }
    
    public async Task<PlaceUserAccessDto> GetPlaceUserAccessAsync(string placeShortId)
    {
        var place = await GetPlaceById(placeShortId);
        
        return new PlaceUserAccessDto
        {
            Id = place.PlaceId.ToString(),
            Visible = place.Visible,
            Editable = place.Editable,
            Reservable = place.Reservable,
            UserAccesses = place.UserAccesses.Select(acc => new UserAccessGetDto
            {
                UserName = acc.UserName,
                AccessUser = acc.AccessUserEnum,
            }).ToList()
        };
    }
    
    public async Task<PlaceGridsAndReservationsDto> GetGridsAndReservationsAsync(string placeShortId)
    {
        var place = await GetPlaceById(placeShortId);
        
        return new PlaceGridsAndReservationsDto
        {
            Grids = place.Grids,
            Reservations = place.Reservations,
        };
    }

    public async Task CreatePlaceAsync(PlaceCreateDto dto)
    {
        var newPlace = new PlaceModel(GenerateShortId(), dto.OwnerId, dto.Name, dto.Description);
        await placesCollection.InsertOneAsync(newPlace);
    }

    public async Task DeletePlaceAsync(string placeShortId)
    {
        var update = Builders<PlaceModel>.Update
            .Set(p => p.IsDeleted, true);

        var result = await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == placeShortId,
            update
        );
        
        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Place: {placeShortId} не найден");
    }

    public async Task UpdatePlaceAsync(PlaceUpdateNameDto dto)
    {
        var update = Builders<PlaceModel>.Update
            .Set(p => p.Name, dto.Name)
            .Set(p => p.Description, dto.Description);
        
        var result = await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceId,
            update
        );
        
        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Place: {dto.PlaceId} не найден");
    }
    
    public async Task AddUserAccessAsync(UserAccessAddDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);

        // Проверяем что такого user уже нет
        if (place.UserAccesses.Any(u => u.UserName == dto.UserName))
            throw new Exception($"Пользователь {dto.UserName} уже имеет доступ");

        var newAccess = new UserAccess
        {
            UserName = dto.UserName,
            AccessUserEnum = dto.AccessUser
        };

        var update = Builders<PlaceModel>.Update
            .Push(p => p.UserAccesses, newAccess);

        await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId,
            update
        );
    }
    
    public async Task UpdateUserAccessAsync(UserAccessUpdateDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);

        // Проверяем что пользователь присутствует
        if (place.UserAccesses.All(u => u.UserName != dto.UserName))
            throw new KeyNotFoundException($"Пользователь {dto.UserName} не имеет доступа");

        // Обновляем AccessUserEnum у элемента массива
        var update = Builders<PlaceModel>.Update
            .Set(p => p.UserAccesses[-1].AccessUserEnum, dto.AccessUser);

        await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId && 
                 p.UserAccesses.Any(u => u.UserName == dto.UserName),
            update
        );
    }
    
    public async Task AddBlockAsync(BlockCreateDto dto)
    {
        // 1. Загружаем Place
        var place = await GetPlaceById(dto.PlaceShortId);

        // 2. Находим нужный Grid
        var grid = GetGridById(dto.GridId, place);

        // 3. Новые координаты
        var newBlock = new Block
        {
            BlockId = ObjectId.GenerateNewId(),
            LeftTopX = dto.LeftTopX,
            LeftTopY = dto.LeftTopY,
            RightBottomX = dto.RightBottomX,
            RightBottomY = dto.RightBottomY,
            Name = dto.Name,
            Color = dto.Color
        };

        // 4. Проверяем пересечения с существующими блоками
        var intersectionBlockId = CheckIntersectionBlocks(newBlock, grid);
        if (intersectionBlockId != null)
        {
            throw new InvalidOperationException(
                $"Блок пересекается с существующим блоком {intersectionBlockId}"
            );
        }

        // 5. Добавляем блок в массив
        var update = Builders<PlaceModel>.Update
            .AddToSet(p => p.Grids[-1].Blocks, newBlock);

        await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId && 
                 p.Grids.Any(g => g.GridId == dto.GridId),
            update
        );
    }
    
    public async Task UpdateBlockCoordinatesAsync(BlockUpdateCoordinatesDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);
        var grid = GetGridById(dto.GridId, place);
        var block = GetBlockById(dto.BlockId, grid);
    
        var updated = new Block
        {
            BlockId = block.BlockId,
            Name = block.Name,
            Color = block.Color,
            LeftTopX = dto.LeftTopX,
            LeftTopY = dto.LeftTopY,
            RightBottomX = dto.RightBottomX,
            RightBottomY = dto.RightBottomY
        };
        
        var intersectionBlockId = CheckIntersectionBlocks(updated, grid);
        if (intersectionBlockId != null)
        {
            throw new InvalidOperationException(
                $"Блок пересекается с существующим блоком {intersectionBlockId}"
            );
        }
    
        var filter = Builders<PlaceModel>.Filter.And(
            Builders<PlaceModel>.Filter.Eq(p => p.PlaceShortId, dto.PlaceShortId),
            Builders<PlaceModel>.Filter.ElemMatch(p => p.Grids, g => g.GridId == dto.GridId),
            Builders<PlaceModel>.Filter.Eq("Grids.Blocks.BlockId", dto.BlockId)
        );
    
        var update = Builders<PlaceModel>.Update
            .Set("Grids.$[].Blocks.$[b].LeftTopX", updated.LeftTopX)
            .Set("Grids.$[].Blocks.$[b].LeftTopY", updated.LeftTopY)
            .Set("Grids.$[].Blocks.$[b].RightBottomX", updated.RightBottomX)
            .Set("Grids.$[].Blocks.$[b].RightBottomY", updated.RightBottomY);
    
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new JsonArrayFilterDefinition<BsonDocument>("{ 'b.BlockId': ObjectId('" + dto.BlockId + "') }")
        };
    
        var options = new UpdateOptions { ArrayFilters = arrayFilters };
    
        await placesCollection.UpdateOneAsync(filter, update, options);
    }
    
    public async Task UpdateBlockNameAsync(BlockUpdateNameDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);
        var grid = GetGridById(dto.GridId, place);
        // var block = GetBlockById(dto.BlockId, grid);

        var filter = Builders<PlaceModel>.Filter.And(
            Builders<PlaceModel>.Filter.Eq(p => p.PlaceShortId, dto.PlaceShortId),
            Builders<PlaceModel>.Filter.ElemMatch(p => p.Grids, g => g.GridId == dto.GridId),
            Builders<PlaceModel>.Filter.Eq("Grids.Blocks.BlockId", dto.BlockId)
        );

        var update = Builders<PlaceModel>.Update
            .Set("Grids.$[].Blocks.$[b].Name", dto.Name);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new JsonArrayFilterDefinition<BsonDocument>("{ 'b.BlockId': ObjectId('" + dto.BlockId + "') }")
        };

        var options = new UpdateOptions { ArrayFilters = arrayFilters };

        await placesCollection.UpdateOneAsync(filter, update, options);
    }
    
    public async Task DeleteBlockAsync(BlockDeleteDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);

        var filter = Builders<PlaceModel>.Filter.And(
            Builders<PlaceModel>.Filter.Eq(p => p.PlaceShortId, dto.PlaceShortId),
            Builders<PlaceModel>.Filter.ElemMatch(p => p.Grids, g => g.GridId == dto.GridId)
        );

        var update = Builders<PlaceModel>.Update
            .PullFilter("Grids.$.Blocks", Builders<BsonDocument>.Filter.Eq("BlockId", dto.BlockId));

        var result = await placesCollection.UpdateOneAsync(filter, update);

        if (result.ModifiedCount == 0)
            throw new InvalidOperationException($"Не удалось удалить блок {dto.BlockId}");
    }
    
    public async Task AddReservationAsync(ReservationCreateDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);

        var overlappingReservation = place.Reservations.Any(r =>
            r.BlockId == dto.BlockId &&
            r.DateTimeStart < dto.DateTimeEnd &&
            dto.DateTimeStart < r.DateTimeEnd
        );

        if (overlappingReservation)
            throw new InvalidOperationException("Время бронирования пересекается с существующим");

        var newReservation = new Reservation
        {
            GridId = dto.GridId,
            BlockId = dto.BlockId,
            DateTimeStart = dto.DateTimeStart,
            DateTimeEnd = dto.DateTimeEnd
        };

        var update = Builders<PlaceModel>.Update
            .Push(p => p.Reservations, newReservation);

        await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId,
            update
        );
    }

    public async Task DeleteReservationAsync(ReservationDeleteDto dto)
    {
        var place = await GetPlaceById(dto.PlaceShortId);

        var reservationExists = place.Reservations.Any(r =>
            r.GridId == dto.GridId &&
            r.BlockId == dto.BlockId &&
            r.DateTimeStart == dto.DateTimeStart &&
            r.DateTimeEnd == dto.DateTimeEnd
        );

        if (!reservationExists)
            throw new KeyNotFoundException("Бронь не найдена");

        var update = Builders<PlaceModel>.Update
            .PullFilter(
                p => p.Reservations,
                r => r.GridId == dto.GridId &&
                     r.BlockId == dto.BlockId &&
                     r.DateTimeStart == dto.DateTimeStart &&
                     r.DateTimeEnd == dto.DateTimeEnd
            );

        var result = await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId,
            update
        );

        if (result.ModifiedCount == 0)
            throw new InvalidOperationException("Не удалось удалить бронь");
    }
}