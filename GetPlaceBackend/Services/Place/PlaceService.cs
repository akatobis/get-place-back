using GetPlaceBackend.Dto.Block;
using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Dto.Reservation;
using GetPlaceBackend.Dto.UserAccess;
using GetPlaceBackend.Models;
using GetPlaceBackend.Models.Enums;
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

    private Grid GetGridById(string gridId, PlaceModel place)
    {
        var grid = place.Grids.FirstOrDefault(g => g.GridId == gridId);
        
        if (grid == null)
            throw new KeyNotFoundException($"Grid {gridId} не найден");

        return grid;
    }

    private Block GetBlockById(string blockId, Grid grid)
    {
        var block = grid.Blocks.FirstOrDefault(b => b.BlockId == blockId);
        if (block == null)
            throw new KeyNotFoundException($"Block {blockId} не найден");
        return block;
    }
    
    private string CheckIntersectionBlocks(Block block, Grid grid, string? ignoredBlockId)
    {
        foreach (var existing in grid.Blocks)
        {
            if (existing.BlockId == ignoredBlockId)
                continue;
            
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
    
    public async Task<List<PlaceModel>> GetPlacesByGroupIdAsync(string? userId = null)
    {
        var filter = Builders<PlaceModel>.Filter.Where(p => !p.IsDeleted);
        if (!string.IsNullOrEmpty(userId))
        {
            filter = Builders<PlaceModel>.Filter.Where(p => !p.IsDeleted && p.OwnerId == userId);
        }

        return await placesCollection
            .Find(filter)
            .ToListAsync();
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

    public async Task<string> CreatePlaceAsync(PlaceCreateDto dto)
    {
        var newPlace = new PlaceModel(GenerateShortId(), dto.OwnerId, dto.Name, dto.Description);
        await placesCollection.InsertOneAsync(newPlace);
        return newPlace.PlaceShortId;
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

    public async Task UpdatePlaceNameAsync(PlaceUpdateNameDto dto)
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
    
    public async Task UpdatePlaceAccessAsync(PlaceAccessUpdateDto dto)
    {
        var updateBuilder = Builders<PlaceModel>.Update;
        UpdateDefinition<PlaceModel>? update = null;

        if (dto.NameAccessPlaceEnum == NameAccessPlaceEnum.Editable)
            update = updateBuilder.Set(p => p.Editable, dto.AccessPlaceEnum);
        if (dto.NameAccessPlaceEnum == NameAccessPlaceEnum.Visible)
            update = updateBuilder.Set(p => p.Visible, dto.AccessPlaceEnum);
        if (dto.NameAccessPlaceEnum == NameAccessPlaceEnum.Reservable)
            update = updateBuilder.Set(p => p.Reservable, dto.AccessPlaceEnum);

        if (update == null)
            throw new ArgumentException("Некорректный NameAccessPlaceEnum");

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
        var place = await GetPlaceById(dto.PlaceShortId);
        var grid = GetGridById(dto.GridId, place);

        var newBlock = new Block
        {
            BlockId = ObjectId.GenerateNewId().ToString(),
            LeftTopX = dto.LeftTopX,
            LeftTopY = dto.LeftTopY,
            RightBottomX = dto.RightBottomX,
            RightBottomY = dto.RightBottomY,
            Name = dto.Name,
        };

        // проверка пересечений
        var intersectionBlockId = CheckIntersectionBlocks(newBlock, grid, null);
        if (intersectionBlockId != null)
            throw new InvalidOperationException(
                $"Блок пересекается с существующим блоком {intersectionBlockId}"
            );

        // добавление в Blocks внутри найденного Grid
        var update = Builders<PlaceModel>.Update
            .AddToSet("Grids.$.Blocks", newBlock);

        var result = await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId &&
                 p.Grids.Any(g => g.GridId == dto.GridId),
            update
        );

        if (result.ModifiedCount == 0)
            throw new InvalidOperationException("Не удалось добавить блок");
    }
    
    public async Task UpdateBlockCoordinatesAsync(BlockUpdateCoordinatesDto dto)
    {
        // 1. Получаем место и проверяем, что всё существует
        var place = await GetPlaceById(dto.PlaceShortId);
        var grid = GetGridById(dto.GridId, place);
        var block = GetBlockById(dto.BlockId, grid);

        // 2. Создаем новый объект для проверки пересечений
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

        // Проверяем пересечение, исключаем текущий блок
        var intersectionBlockId = CheckIntersectionBlocks(updated, grid, dto.BlockId);
        if (intersectionBlockId != null)
            throw new InvalidOperationException($"Новые координаты блока пересекаются с блоком {intersectionBlockId}");

        // 3. Применяем изменение в памяти
        block.LeftTopX = dto.LeftTopX;
        block.LeftTopY = dto.LeftTopY;
        block.RightBottomX = dto.RightBottomX;
        block.RightBottomY = dto.RightBottomY;

        // 4. Заменяем документ целиком (или можно делать ReplaceOneAsync с фильтром по placeId)
        var replaceFilter = Builders<PlaceModel>.Filter.Eq(p => p.PlaceId, place.PlaceId);
        var replaceResult = await placesCollection.ReplaceOneAsync(replaceFilter, place);

        if (replaceResult.ModifiedCount == 0)
            throw new InvalidOperationException($"Не удалось обновить BlockId={dto.BlockId} (координаты могли быть такими же)");
    }

    
    public async Task UpdateBlockNameAsync(BlockUpdateNameDto dto)
    {
        var place = await placesCollection.Find(p => p.PlaceShortId == dto.PlaceShortId).FirstOrDefaultAsync();
        var grid = place.Grids.First(g => g.GridId == dto.GridId);
        var block = grid.Blocks.First(b => b.BlockId == dto.BlockId);
        block.Name = dto.Name;
        await placesCollection.ReplaceOneAsync(p => p.PlaceShortId == dto.PlaceShortId, place);
    }
    
    public async Task DeleteBlockAsync(BlockDeleteDto dto)
    {
        var place = await placesCollection
            .Find(p => p.PlaceShortId == dto.PlaceShortId)
            .FirstOrDefaultAsync();

        if (place == null)
            throw new KeyNotFoundException($"Place {dto.PlaceShortId} не найден");

        var grid = place.Grids.FirstOrDefault(g => g.GridId == dto.GridId);
        if (grid == null)
            throw new KeyNotFoundException($"Grid {dto.GridId} не найден");

        var block = grid.Blocks.FirstOrDefault(b => b.BlockId == dto.BlockId);
        if (block == null)
            throw new KeyNotFoundException($"Block {dto.BlockId} не найден");

        // Удаляем блок из массива
        grid.Blocks.Remove(block);

        // Сохраняем документ целиком
        var result = await placesCollection.ReplaceOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId,
            place
        );

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

        var reservationExists = place.Reservations
            .Any(r => r.ReservationId == dto.ReservationId);

        if (!reservationExists)
            throw new KeyNotFoundException($"Бронь {dto.ReservationId} не найдена");

        var update = Builders<PlaceModel>.Update
            .PullFilter(p => p.Reservations, r => r.ReservationId == dto.ReservationId);

        var result = await placesCollection.UpdateOneAsync(
            p => p.PlaceShortId == dto.PlaceShortId,
            update
        );

        if (result.ModifiedCount == 0)
            throw new InvalidOperationException(
                $"Не удалось удалить бронь {dto.ReservationId}"
            );
    }
    
    public async Task UpdateUserNameInAllPlacesAsync(string oldUserName, string newUserName)
    {
        if (string.IsNullOrWhiteSpace(oldUserName) || string.IsNullOrWhiteSpace(newUserName))
            throw new ArgumentException("Usernames не может быть пустым");

        var filter = Builders<PlaceModel>.Filter.ElemMatch(
            p => p.UserAccesses,
            ua => ua.UserName == oldUserName
        );

        var update = Builders<PlaceModel>.Update
            .Set("UserAccesses.$[elem].UserName", newUserName);

        var arrayFilter = new[]
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(
                new BsonDocument("elem.UserName", oldUserName)
            )
        };

        var options = new UpdateOptions
        {
            ArrayFilters = arrayFilter,
            IsUpsert = false
        };

        var result = await placesCollection.UpdateManyAsync(filter, update, options);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Не найдено ни одного PlaceModel с UserName = '{oldUserName}'");
    }
}