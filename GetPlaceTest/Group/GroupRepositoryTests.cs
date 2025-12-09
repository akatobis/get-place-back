using Xunit;
using Mongo2Go;
using MongoDB.Driver;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using GetPlaceBackend.Models;
using GetPlaceBackend.Services.Group;

namespace GetPlaceTest;

public class GroupRepositoryTests
{
    private readonly MongoDbRunner _mongoRunner;
    private readonly IMongoCollection<GroupModel> _collection;
    private readonly GroupService _repository;

    public GroupRepositoryTests()
    {
        _mongoRunner = MongoDbRunner.Start(); // поднимаем in-memory MongoDB

        var client = new MongoClient(_mongoRunner.ConnectionString);
        var db = client.GetDatabase("TestDb");

        _collection = db.GetCollection<GroupModel>("groups");

        _repository = new GroupService(db);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyUserGroups_AndOrdered()
    {
        // Arrange
        var userId = "user1";

        var data = new List<GroupModel>
        {
            new GroupModel { GroupId = "6937f6ede3281e9ef6844ff3", UserId = userId, Name="A", Order = 2, IsDeleted = false },
            new GroupModel { GroupId = "6937f6ede3281e9ef6844ff4", UserId = userId, Name="B", Order = 1, IsDeleted = false },
            new GroupModel { GroupId = "6937f6ede3281e9ef6844ff5", UserId = "other", Name="C", Order = 3, IsDeleted = false },
            new GroupModel { GroupId = "6937f6ede3281e9ef6844ff6", UserId = userId, Name="D", Order = 3, IsDeleted = true } // удалено
        };

        await _collection.InsertManyAsync(data);

        // Act
        var result = await _repository.GetAll(userId);

        // Assert
        result.Should().HaveCount(2); // только 2 валидные группы
        result[0].Order.Should().Be(1); // сортировка по Order
        result[1].Order.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoGroups()
    {
        // Arrange
        var userId = "emptyUser";

        // Act
        var result = await _repository.GetAll(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_IgnoresDeletedGroups()
    {
        // Arrange
        var userId = "user2";

        var data = new List<GroupModel>
        {
            new GroupModel { GroupId = "6937f6ede3281e9ef6844ff1", UserId = userId, Name="X", Order = 1, IsDeleted = true },
            new GroupModel { GroupId = "6937f6ede3281e9ef6844ff2", UserId = userId, Name="Y", Order = 2, IsDeleted = false }
        };

        await _collection.InsertManyAsync(data);

        // Act
        var result = await _repository.GetAll(userId);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Y");
    }
    
    [Fact]
    public async Task AddAsync_ShouldCreateGroupWithOrder1_WhenUserHasNoGroups()
    {
        // arrange
        string userId = "user1";

        // act
        var id = await _repository.AddAsync("Test Group", userId);
        var inserted = await _collection.Find(g => g.GroupId == id).FirstOrDefaultAsync();

        // assert
        inserted.Should().NotBeNull();
        inserted.Order.Should().Be(1);
        inserted.Name.Should().Be("Test Group");
        inserted.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task AddAsync_ShouldIncrementOrder_WhenUserAlreadyHasGroups()
    {
        // arrange
        string userId = "user2";

        await _collection.InsertOneAsync(new GroupModel { Name = "G1", Order = 1, UserId = userId });
        await _collection.InsertOneAsync(new GroupModel { Name = "G2", Order = 2, UserId = userId });

        // act
        var id = await _repository.AddAsync("G3", userId);
        var inserted = await _collection.Find(g => g.GroupId == id).FirstOrDefaultAsync();

        // assert
        inserted.Should().NotBeNull();
        inserted.Order.Should().Be(3);
    }

    [Fact]
    public async Task AddAsync_ShouldUseMaxOrder_WhenExistingGroupsAreUnsorted()
    {
        // arrange
        string userId = "user3";

        await _collection.InsertOneAsync(new GroupModel { Name = "A", Order = 5, UserId = userId });
        await _collection.InsertOneAsync(new GroupModel { Name = "B", Order = 2, UserId = userId });

        // act
        var id = await _repository.AddAsync("C", userId);
        var inserted = await _collection.Find(g => g.GroupId == id).FirstOrDefaultAsync();

        // assert
        inserted.Should().NotBeNull();
        inserted.Order.Should().Be(6); // max(5) + 1
    }
    
    [Fact]
    public async Task SoftDeleteAsync_ShouldReturnTrue_WhenGroupExists()
    {
        // arrange
        var group = new GroupModel
        {
            Name = "Test Group",
            UserId = "user1",
            Order = 1,
            IsDeleted = false
        };

        await _collection.InsertOneAsync(group);

        // act
        var result = await _repository.SoftDeleteAsync(group.GroupId);

        // assert
        result.Should().BeTrue();

        var updated = await _collection
            .Find(g => g.GroupId == group.GroupId)
            .FirstOrDefaultAsync();

        updated.Should().NotBeNull();
        updated.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldReturnFalse_WhenGroupDoesNotExist()
    {
        // act
        var result = await _repository.SoftDeleteAsync("000000000000000000000000");

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldNotAffectOtherGroups()
    {
        // arrange
        var g1 = new GroupModel { Name = "G1", UserId = "u", Order = 1, IsDeleted = false };
        var g2 = new GroupModel { Name = "G2", UserId = "u", Order = 2, IsDeleted = false };

        await _collection.InsertOneAsync(g1);
        await _collection.InsertOneAsync(g2);

        // act — удаляем только первую
        await _repository.SoftDeleteAsync(g1.GroupId);

        var updatedG1 = await _collection.Find(g => g.GroupId == g1.GroupId).FirstOrDefaultAsync();
        var updatedG2 = await _collection.Find(g => g.GroupId == g2.GroupId).FirstOrDefaultAsync();

        // assert
        updatedG1.IsDeleted.Should().BeTrue();
        updatedG2.IsDeleted.Should().BeFalse();
    }
    
    [Fact]
    public async Task UpdateOrderAsync_ShouldReturnFalse_WhenGroupNotFound()
    {
        // act
        var result = await _repository.UpdateOrderAsync("000000000000000000000000", 1);

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrderAsync_ShouldReturnTrue_WhenGroupExists()
    {
        // arrange
        var g = new GroupModel { Name = "A", Order = 1, UserId = "u", IsDeleted = false };
        await _collection.InsertOneAsync(g);

        // act
        var result = await _repository.UpdateOrderAsync(g.GroupId, 1);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOrderAsync_ShouldShiftOtherGroups_IfTheirOrderIsGreaterOrEqual()
    {
        /*
            initial:
            g1: order 1
            g2: order 2
            g3: order 3

            call: UpdateOrderAsync(g1.Id, 2)

            expected:
            g1 => order = 2
            g2 => order = 3
            g3 => order = 4
        */

        var g1 = new GroupModel { Name = "G1", Order = 1, UserId = "u" };
        var g2 = new GroupModel { Name = "G2", Order = 2, UserId = "u" };
        var g3 = new GroupModel { Name = "G3", Order = 3, UserId = "u" };

        await _collection.InsertManyAsync(new[] { g1, g2, g3 });

        await _repository.UpdateOrderAsync(g1.GroupId, 2);

        var updated = await _collection.Find(_ => true).SortBy(g => g.Name).ToListAsync();

        updated[0].Order.Should().Be(2); // G1
        updated[1].Order.Should().Be(3); // G2
        updated[2].Order.Should().Be(4); // G3
    }

    [Fact]
    public async Task UpdateOrderAsync_ShouldNotShiftGroups_WithOrderLessThanNewOrder()
    {
        /*
            initial:
            g1: order 1
            g2: order 5

            call: UpdateOrderAsync(g2.Id, 5)

            expected (since only >= 5 shift):
            g2 => 5
            g1 => stays 1
        */

        var g1 = new GroupModel { Name = "G1", Order = 1, UserId = "u" };
        var g2 = new GroupModel { Name = "G2", Order = 5, UserId = "u" };

        await _collection.InsertManyAsync(new[] { g1, g2 });

        await _repository.UpdateOrderAsync(g2.GroupId, 5);

        var updated = await _collection.Find(_ => true).SortBy(g => g.Name).ToListAsync();

        updated[0].Order.Should().Be(1); // G1 stays same
        updated[1].Order.Should().Be(5); // G2 unchanged except reassignment
    }
}
