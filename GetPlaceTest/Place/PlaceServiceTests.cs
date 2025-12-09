

using Xunit;
using MongoDB.Driver;
using Mongo2Go;
using System.Threading.Tasks;
using System.Collections.Generic;
using GetPlaceBackend.Dto.Block;
using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Dto.Reservation;
using GetPlaceBackend.Dto.UserAccess;
using GetPlaceBackend.Models;
using GetPlaceBackend.Models.Enums;
using GetPlaceBackend.Services.Place;
using MongoDB.Bson;

namespace GetPlaceTest.Place;

public class PlaceServiceTests
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoCollection<PlaceModel> _collection;
    private readonly PlaceService _service;

    public PlaceServiceTests()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        var db = client.GetDatabase("TestDb");

        _collection = db.GetCollection<PlaceModel>("places");
        _service = new PlaceService(db);
    }

    [Fact]
    public async Task GetPlacesByGroupIdAsync_ShouldReturnAll_WhenUserIdNull()
    {
        // Arrange
        await _collection.DeleteManyAsync(_ => true);

        var place1 = new PlaceModel("ABCD", "user1", "Place 1", "Desc 1");
        var place2 = new PlaceModel("EFGH", "user2", "Place 2", "Desc 2");
        await _collection.InsertManyAsync(new List<PlaceModel> { place1, place2 });

        // Act
        var result = await _service.GetPlacesByGroupIdAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPlacesByGroupIdAsync_ShouldReturnOnlyUserPlaces_WhenUserIdProvided()
    {
        await _collection.DeleteManyAsync(_ => true);

        var place1 = new PlaceModel("ABCD", "user1", "Place 1", "Desc 1");
        var place2 = new PlaceModel("EFGH", "user2", "Place 2", "Desc 2");
        await _collection.InsertManyAsync(new List<PlaceModel> { place1, place2 });

        var result = await _service.GetPlacesByGroupIdAsync("user1");

        Assert.Single(result);
        Assert.Equal("user1", result[0].OwnerId);
    }

    [Fact]
    public async Task GetPlacesByGroupIdAsync_ShouldExcludeDeletedPlaces()
    {
        await _collection.DeleteManyAsync(_ => true);

        var place1 = new PlaceModel("ABCD", "user1", "Place 1", "Desc 1") { IsDeleted = false };
        var place2 = new PlaceModel("EFGH", "user2", "Place 2", "Desc 2") { IsDeleted = true };
        await _collection.InsertManyAsync(new List<PlaceModel> { place1, place2 });

        var result = await _service.GetPlacesByGroupIdAsync();

        Assert.Single(result);
        Assert.False(result[0].IsDeleted);
    }
    
    [Fact]
    public async Task GetPlaceAccessAsync_ShouldReturnPlaceAccess_WhenPlaceExists()
    {
        // Arrange
        await _collection.DeleteManyAsync(_ => true);

        var place = new PlaceModel("V1F3", "ownerId", "name", "description")
        {
            Visible = AccessPlaceEnum.some,
            Editable = AccessPlaceEnum.all,
            Reservable = AccessPlaceEnum.nobody
        };

        await _collection.InsertOneAsync(place);

        // Act
        var result = await _service.GetPlaceAccessAsync("V1F3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("V1F3", result.Id);
        Assert.Equal(AccessPlaceEnum.some, result.Visible);
        Assert.Equal(AccessPlaceEnum.all, result.Editable);
        Assert.Equal(AccessPlaceEnum.nobody, result.Reservable);
    }
    
    [Fact]
    public async Task CreatePlaceAsync_ShouldInsertPlaceAndReturnShortId()
    {
        // Arrange
        await _collection.DeleteManyAsync(_ => true); // очистка коллекции
        var dto = new PlaceCreateDto
        {
            OwnerId = "owner123",
            Name = "Test Place",
            Description = "Description"
        };

        // Act
        var shortId = await _service.CreatePlaceAsync(dto);

        // Assert
        Assert.False(string.IsNullOrEmpty(shortId)); // PlaceShortId должен быть сгенерирован

        var insertedPlace = await _collection.Find(p => p.PlaceShortId == shortId).FirstOrDefaultAsync();
        Assert.NotNull(insertedPlace);
        Assert.Equal(dto.OwnerId, insertedPlace.OwnerId);
        Assert.Equal(dto.Name, insertedPlace.Name);
        Assert.Equal(dto.Description, insertedPlace.Description);
    }

    [Fact]
    public async Task CreatePlaceAsync_ShouldGenerateUniqueShortId()
    {
        // Arrange
        await _collection.DeleteManyAsync(_ => true);
        var dto1 = new PlaceCreateDto { OwnerId = "owner1", Name = "Place 1", Description = "Desc 1" };
        var dto2 = new PlaceCreateDto { OwnerId = "owner2", Name = "Place 2", Description = "Desc 2" };

        // Act
        var shortId1 = await _service.CreatePlaceAsync(dto1);
        var shortId2 = await _service.CreatePlaceAsync(dto2);

        // Assert
        Assert.NotEqual(shortId1, shortId2); // Короткие ID должны быть уникальны
    }
    
    [Fact]
    public async Task UpdatePlaceAccessAsync_ShouldUpdateEditable()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "Test", "Desc");
        await _collection.InsertOneAsync(place);

        var dto = new PlaceAccessUpdateDto
        {
            PlaceId = "V1F3",
            NameAccessPlaceEnum = NameAccessPlaceEnum.Editable,
            AccessPlaceEnum = AccessPlaceEnum.some
        };

        // Act
        await _service.UpdatePlaceAccessAsync(dto);

        // Assert
        var updated = await _collection.Find(p => p.PlaceShortId == "V1F3").FirstOrDefaultAsync();
        Assert.Equal(AccessPlaceEnum.some, updated.Editable);
    }

    [Fact]
    public async Task UpdatePlaceAccessAsync_ShouldUpdateVisible()
    {
        var place = new PlaceModel("V2F3", "owner2", "Test2", "Desc2");
        await _collection.InsertOneAsync(place);

        var dto = new PlaceAccessUpdateDto
        {
            PlaceId = "V2F3",
            NameAccessPlaceEnum = NameAccessPlaceEnum.Visible,
            AccessPlaceEnum = AccessPlaceEnum.nobody
        };

        await _service.UpdatePlaceAccessAsync(dto);

        var updated = await _collection.Find(p => p.PlaceShortId == "V2F3").FirstOrDefaultAsync();
        Assert.Equal(AccessPlaceEnum.nobody, updated.Visible);
    }

    [Fact]
    public async Task UpdatePlaceAccessAsync_ShouldUpdateReservable()
    {
        var place = new PlaceModel("V3F3", "owner3", "Test3", "Desc3");
        await _collection.InsertOneAsync(place);

        var dto = new PlaceAccessUpdateDto
        {
            PlaceId = "V3F3",
            NameAccessPlaceEnum = NameAccessPlaceEnum.Reservable,
            AccessPlaceEnum = AccessPlaceEnum.all
        };

        await _service.UpdatePlaceAccessAsync(dto);

        var updated = await _collection.Find(p => p.PlaceShortId == "V3F3").FirstOrDefaultAsync();
        Assert.Equal(AccessPlaceEnum.all, updated.Reservable);
    }

    [Fact]
    public async Task UpdatePlaceAccessAsync_ShouldThrowKeyNotFound_WhenPlaceNotFound()
    {
        var dto = new PlaceAccessUpdateDto
        {
            PlaceId = "NON_EXIST",
            NameAccessPlaceEnum = NameAccessPlaceEnum.Editable,
            AccessPlaceEnum = AccessPlaceEnum.all
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdatePlaceAccessAsync(dto)
        );
    }

    [Fact]
    public async Task UpdatePlaceAccessAsync_ShouldThrowArgumentException_WhenInvalidEnum()
    {
        var place = new PlaceModel("V4F3", "owner4", "Test4", "Desc4");
        await _collection.InsertOneAsync(place);

        var dto = new PlaceAccessUpdateDto
        {
            PlaceId = "V4F3",
            NameAccessPlaceEnum = (NameAccessPlaceEnum)999, // неверное значение
            AccessPlaceEnum = AccessPlaceEnum.all
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdatePlaceAccessAsync(dto)
        );
    }
    
    [Fact]
    public async Task AddUserAccessAsync_ShouldAddUserAccess_WhenUserNotExists()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "Test", "Desc");
        await _collection.InsertOneAsync(place);

        var dto = new UserAccessAddDto
        {
            PlaceShortId = "V1F3",
            UserName = "User1",
            AccessUser = AccessUserEnum.view
        };

        // Act
        await _service.AddUserAccessAsync(dto);

        // Assert
        var updated = await _collection.Find(p => p.PlaceShortId == "V1F3").FirstOrDefaultAsync();
        var access = updated.UserAccesses.FirstOrDefault(u => u.UserName == "User1");

        Assert.NotNull(access);
        Assert.Equal(AccessUserEnum.view, access.AccessUserEnum);
    }

    [Fact]
    public async Task AddUserAccessAsync_ShouldThrowException_WhenUserAlreadyExists()
    {
        // Arrange
        var place = new PlaceModel("V2F3", "owner2", "Test2", "Desc2");
        place.UserAccesses.Add(new UserAccess
        {
            UserName = "UserExist",
            AccessUserEnum = AccessUserEnum.view
        });
        await _collection.InsertOneAsync(place);

        var dto = new UserAccessAddDto
        {
            PlaceShortId = "V2F3",
            UserName = "UserExist",
            AccessUser = AccessUserEnum.editable
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => _service.AddUserAccessAsync(dto)
        );

        Assert.Contains("уже имеет доступ", ex.Message);
    }

    [Fact]
    public async Task AddUserAccessAsync_ShouldNotAffectOtherUsers()
    {
        // Arrange
        var place = new PlaceModel("V3F3", "owner3", "Test3", "Desc3");
        place.UserAccesses.Add(new UserAccess { UserName = "ExistingUser", AccessUserEnum = AccessUserEnum.view });
        await _collection.InsertOneAsync(place);

        var dto = new UserAccessAddDto
        {
            PlaceShortId = "V3F3",
            UserName = "NewUser",
            AccessUser = AccessUserEnum.editable
        };

        // Act
        await _service.AddUserAccessAsync(dto);

        // Assert
        var updated = await _collection.Find(p => p.PlaceShortId == "V3F3").FirstOrDefaultAsync();
        Assert.Equal(2, updated.UserAccesses.Count);
        Assert.Contains(updated.UserAccesses, u => u.UserName == "ExistingUser");
        Assert.Contains(updated.UserAccesses, u => u.UserName == " ");
    }
    
    [Fact]
    public async Task AddBlockAsync_ShouldAddBlock_WhenNoIntersection()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "TestPlace", "Desc");
        await _collection.InsertOneAsync(place);

        var dto = new BlockCreateDto
        {
            PlaceShortId = "V1F3",
            GridId = place.Grids[0].GridId,
            Name = "Block1",
            LeftTopX = 0,
            LeftTopY = 0,
            RightBottomX = 10,
            RightBottomY = 10
        };

        // Act
        var blockId = await _service.AddBlockAsync(dto);

        // Assert
        var updatedPlace = await _collection.Find(p => p.PlaceShortId == "V1F3").FirstOrDefaultAsync();
        var addedBlock = updatedPlace.Grids.First().Blocks.FirstOrDefault(b => b.BlockId == blockId);

        Assert.NotNull(addedBlock);
        Assert.Equal("Block1", addedBlock.Name);
    }

    [Fact]
    public async Task AddBlockAsync_ShouldThrow_WhenIntersectingBlockExists()
    {
        // Arrange
        var place = new PlaceModel("V2F3", "owner2", "TestPlace2", "Desc2");

        var existingBlock = new Block
        {
            BlockId = ObjectId.GenerateNewId().ToString(),
            Name = "B1",
            LeftTopX = 0,
            LeftTopY = 0,
            RightBottomX = 10,
            RightBottomY = 10
        };
        place.Grids[0].Blocks.Add(existingBlock);
        await _collection.InsertOneAsync(place);
        
        var dto = new BlockCreateDto
        {
            PlaceShortId = "V2F3",
            GridId = place.Grids[0].GridId,
            Name = "Block2",
            LeftTopX = 5,   // пересекается с существующим блоком B1
            LeftTopY = 5,
            RightBottomX = 15,
            RightBottomY = 15
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddBlockAsync(dto)
        );

        Assert.Contains("пересекается с существующим блоком", ex.Message);
    }
    
    [Fact]
    public async Task UpdateBlockCoordinatesAsync_ShouldUpdateCoordinates_WhenNoIntersection()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "Place1", "Desc1");
        var block = new Block
        {
            LeftTopX = 0,
            LeftTopY = 0,
            RightBottomX = 10,
            RightBottomY = 10,
            Name = "Block1"
        };
        place.Grids[0].Blocks.Add(block);
        await _collection.InsertOneAsync(place);

        var dto = new BlockUpdateCoordinatesDto
        {
            PlaceShortId = place.PlaceShortId,
            GridId = place.Grids[0].GridId,
            BlockId = block.BlockId,
            LeftTopX = 20,
            LeftTopY = 20,
            RightBottomX = 30,
            RightBottomY = 30
        };

        // Act
        await _service.UpdateBlockCoordinatesAsync(dto);

        // Assert
        var updatedPlace = await _collection.Find(p => p.PlaceShortId == place.PlaceShortId).FirstOrDefaultAsync();
        var updatedBlock = updatedPlace.Grids.First().Blocks.First(b => b.BlockId == block.BlockId);
        Assert.Equal(20, updatedBlock.LeftTopX);
        Assert.Equal(20, updatedBlock.LeftTopY);
        Assert.Equal(30, updatedBlock.RightBottomX);
        Assert.Equal(30, updatedBlock.RightBottomY);
    }

    [Fact]
    public async Task UpdateBlockCoordinatesAsync_ShouldThrow_WhenIntersectionOccurs()
    {
        // Arrange
        var place = new PlaceModel("V2F3", "owner2", "Place2", "Desc2");
        List<Block> newBlocks =
        [
            new Block { LeftTopX = 0, LeftTopY = 0, RightBottomX = 10, RightBottomY = 10 },
            new Block { LeftTopX = 20, LeftTopY = 20, RightBottomX = 30, RightBottomY = 30 }
        ];
        place.Grids[0].Blocks.AddRange(newBlocks);
        await _collection.InsertOneAsync(place);

        var dto = new BlockUpdateCoordinatesDto
        {
            PlaceShortId = place.PlaceShortId,
            GridId = place.Grids[0].GridId,
            BlockId = newBlocks[0].BlockId,
            LeftTopX = 25,   // пересекает B2
            LeftTopY = 25,
            RightBottomX = 35,
            RightBottomY = 35
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateBlockCoordinatesAsync(dto));
        Assert.Contains("пересекаются с блоком", ex.Message);
    }
    
    [Fact]
    public async Task DeleteBlockAsync_ShouldRemoveBlock_WhenBlockExists()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "Place1", "Desc1");
        List<Block> newBlocks =
        [
            new Block { Name = "Block1" },
            new Block { Name = "Block2" }
        ];
        place.Grids[0].Blocks.AddRange(newBlocks);
        await _collection.InsertOneAsync(place);

        var dto = new BlockDeleteDto("V1F3", place.Grids[0].GridId, "B1");

        // Act
        await _service.DeleteBlockAsync(dto);

        // Assert
        var updatedPlace = await _collection.Find(p => p.PlaceShortId == "V1F3").FirstOrDefaultAsync();
        var updatedGrid = updatedPlace.Grids.First(g => g.GridId == place.Grids[0].GridId);
        Assert.DoesNotContain(updatedGrid.Blocks, b => b.BlockId == place.Grids[0].Blocks[0].BlockId);
        Assert.Contains(updatedGrid.Blocks, b => b.BlockId == place.Grids[0].Blocks[1].BlockId); // другой блок остался
    }

    [Fact]
    public async Task DeleteBlockAsync_ShouldThrow_WhenPlaceNotFound()
    {
        // Arrange
        var dto = new BlockDeleteDto("NonExistent", "G1", "B1");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteBlockAsync(dto));
        Assert.Contains("Place NonExistent не найден", ex.Message);
    }
    
    [Fact]
    public async Task AddReservationAsync_ShouldAddReservation_WhenNoOverlap()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "Place1", "Desc1");
        var grid = new Grid { GridId = "G1", Blocks = [] };
        place.Grids.Add(grid);
        await _collection.InsertOneAsync(place);

        var dto = new ReservationCreateDto
        {
            PlaceShortId = "V1F3",
            GridId = "G1",
            BlockId = "B1",
            DateTimeStart = new DateTime(2025, 12, 10, 10, 0, 0),
            DateTimeEnd = new DateTime(2025, 12, 10, 12, 0, 0)
        };

        // Act
        var reservationId = await _service.AddReservationAsync(dto);

        // Assert
        var updatedPlace = await _collection.Find(p => p.PlaceShortId == "V1F3").FirstOrDefaultAsync();
        var addedReservation = updatedPlace.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
        Assert.NotNull(addedReservation);
        Assert.Equal(dto.BlockId, addedReservation.BlockId);
        Assert.Equal(dto.GridId, addedReservation.GridId);
        Assert.Equal(dto.DateTimeStart, addedReservation.DateTimeStart);
        Assert.Equal(dto.DateTimeEnd, addedReservation.DateTimeEnd);
    }

    [Fact]
    public async Task AddReservationAsync_ShouldThrow_WhenOverlappingReservationExists()
    {
        // Arrange
        var place = new PlaceModel("V2F3", "owner2", "Place2", "Desc2");
        place.Reservations.Add(new Reservation
        {
            ReservationId = "R1",
            GridId = "G1",
            BlockId = "B1",
            DateTimeStart = new DateTime(2025, 12, 10, 10, 0, 0),
            DateTimeEnd = new DateTime(2025, 12, 10, 12, 0, 0)
        });
        await _collection.InsertOneAsync(place);

        var dto = new ReservationCreateDto
        {
            PlaceShortId = "V2F3",
            GridId = "G1",
            BlockId = "B1",
            DateTimeStart = new DateTime(2025, 12, 10, 11, 0, 0),
            DateTimeEnd = new DateTime(2025, 12, 10, 13, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddReservationAsync(dto));
        Assert.Equal("Время бронирования пересекается с существующим", ex.Message);
    }

    [Fact]
    public async Task AddReservationAsync_ShouldAllowNonOverlappingReservations_OnSameBlock()
    {
        // Arrange
        var place = new PlaceModel("V3F3", "owner3", "Place3", "Desc3");
        place.Reservations.Add(new Reservation
        {
            ReservationId = "R1",
            GridId = "G1",
            BlockId = "B1",
            DateTimeStart = new DateTime(2025, 12, 10, 8, 0, 0),
            DateTimeEnd = new DateTime(2025, 12, 10, 10, 0, 0)
        });
        await _collection.InsertOneAsync(place);

        var dto = new ReservationCreateDto
        {
            PlaceShortId = "V3F3",
            GridId = "G1",
            BlockId = "B1",
            DateTimeStart = new DateTime(2025, 12, 10, 10, 0, 0),
            DateTimeEnd = new DateTime(2025, 12, 10, 12, 0, 0)
        };

        // Act
        var reservationId = await _service.AddReservationAsync(dto);

        // Assert
        var updatedPlace = await _collection.Find(p => p.PlaceShortId == "V3F3").FirstOrDefaultAsync();
        var addedReservation = updatedPlace.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
        Assert.NotNull(addedReservation);
    }
    
    [Fact]
    public async Task DeleteReservationAsync_ShouldDeleteReservation_WhenExists()
    {
        // Arrange
        var place = new PlaceModel("V1F3", "owner1", "Place1", "Desc1");
        var reservation = new Reservation
        {
            ReservationId = "R1",
            GridId = "G1",
            BlockId = "B1",
            DateTimeStart = new DateTime(2025, 12, 10, 10, 0, 0),
            DateTimeEnd = new DateTime(2025, 12, 10, 12, 0, 0)
        };
        place.Reservations.Add(reservation);
        await _collection.InsertOneAsync(place);

        var dto = new ReservationDeleteDto("V1F3", "R1");

        // Act
        await _service.DeleteReservationAsync(dto);

        // Assert
        var updatedPlace = await _collection.Find(p => p.PlaceShortId == "V1F3").FirstOrDefaultAsync();
        Assert.DoesNotContain(updatedPlace.Reservations, r => r.ReservationId == "R1");
    }

    [Fact]
    public async Task DeleteReservationAsync_ShouldThrow_WhenReservationNotFound()
    {
        // Arrange
        var place = new PlaceModel("V2F3", "owner2", "Place2", "Desc2");
        await _collection.InsertOneAsync(place);

        var dto = new ReservationDeleteDto("V2F3", "NonExistentR");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteReservationAsync(dto));
        Assert.Equal("Бронь NonExistentR не найдена", ex.Message);
    }

    [Fact]
    public async Task DeleteReservationAsync_ShouldThrow_WhenPlaceNotFound()
    {
        // Arrange
        var dto = new ReservationDeleteDto("NoPlace", "R1");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteReservationAsync(dto));
    }
}

