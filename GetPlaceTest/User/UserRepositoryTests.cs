using Xunit;
using MongoDB.Driver;
using Mongo2Go;
using System.Threading.Tasks;
using GetPlaceBackend.Models;
using GetPlaceBackend.Services.Place;
using GetPlaceBackend.Services.User;
using Moq;

namespace GetPlaceTest.User;

public class UserRepositoryTests
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoCollection<UserModel> _collection;
    private readonly UserService _repository;
    private readonly Mock<IPlaceService> _placeServiceMock;

    public UserRepositoryTests()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        var db = client.GetDatabase("TestDb");
        _collection = db.GetCollection<UserModel>("users");
        _placeServiceMock = new Mock<IPlaceService>();

        _repository = new UserService(db, _placeServiceMock.Object);
    }

    [Fact]
    public async Task GetById_ShouldReturnUser_WhenExistsAndNotDeleted()
    {
        // arrange
        var user = new UserModel("123", "Test User");

        await _collection.InsertOneAsync(user);

        // act
        var result = await _repository.GetById("123");

        // assert
        Assert.NotNull(result);
        Assert.Equal("123", result.TgId);
        Assert.False(result.IsDeleted);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenUserDeleted()
    {
        var user = new UserModel("777", "Deleted User")
        {
            IsDeleted = true
        };

        await _collection.InsertOneAsync(user);

        var result = await _repository.GetById("777");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenUserNotFound()
    {
        // act
        var result = await _repository.GetById("no_such_user");

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ShouldSearchByExactTgId()
    {
        var user1 = new UserModel("100", "Test User 100");
        var user2 = new UserModel("101", "Test User 101");

        await _collection.InsertManyAsync([user1, user2]);

        var result = await _repository.GetById("101");

        Assert.NotNull(result);
        Assert.Equal("101", result.TgId);
    }
    
    [Fact]
    public async Task GetByUsername_ShouldReturnUser_WhenExistsAndNotDeleted()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user = new UserModel("tg123", "JohnDoe");

        await _collection.InsertOneAsync(user);

        var result = await _repository.GetByUsername("JohnDoe");

        Assert.NotNull(result);
        Assert.Equal("JohnDoe", result.UserName);
        Assert.False(result.IsDeleted);
    }

    [Fact]
    public async Task GetByUsername_ShouldReturnNull_WhenUserDeleted()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user = new UserModel("tg777", "GhostUser")
        {
            IsDeleted = true
        };

        await _collection.InsertOneAsync(user);

        var result = await _repository.GetByUsername("GhostUser");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsername_ShouldReturnNull_WhenUserNotFound()
    {
        await _collection.DeleteManyAsync(_ => true);

        var result = await _repository.GetByUsername("no_such_username");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsername_ShouldReturnCorrectUser_WhenManyUsersExist()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user1 = new UserModel("tg1", "Alice");
        var user2 = new UserModel("tg2", "Bob");
        var user3 = new UserModel("tg3", "Charlie");

        await _collection.InsertManyAsync([user1, user2, user3]);

        var result = await _repository.GetByUsername("Bob");

        Assert.NotNull(result);
        Assert.Equal("Bob", result.UserName);
    }
    
    [Fact]
    public async Task CreateOrUpdate_ShouldInsertNewUser_WhenUserDoesNotExist()
    {
        await _collection.DeleteManyAsync(_ => true);

        await _repository.CreateOrUpdate("100", "Alice");

        var user = await _collection.Find(u => u.TgId == "100").FirstOrDefaultAsync();

        Assert.NotNull(user);
        Assert.Equal("Alice", user.UserName);
    }

    [Fact]
    public async Task CreateOrUpdate_ShouldDoNothing_WhenUserExistsAndUsernameSame()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user = new UserModel("200", "Bob");
        await _collection.InsertOneAsync(user);

        await _repository.CreateOrUpdate("200", "Bob");

        var result = await _collection.Find(u => u.TgId == "200").FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("Bob", result.UserName);

        // Должно НЕ вызываться, т.к. username не менялся
        _placeServiceMock.Verify(
            p => p.UpdateUserNameInAllPlacesAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateOrUpdate_ShouldUpdateUsername_WhenUserExistsWithDifferentUsername()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user = new UserModel("300", "CharlieOld");
        await _collection.InsertOneAsync(user);

        await _repository.CreateOrUpdate("300", "CharlieNew");

        var updated = await _collection.Find(u => u.TgId == "300").FirstOrDefaultAsync();

        Assert.NotNull(updated);
        Assert.Equal("CharlieNew", updated.UserName);

        _placeServiceMock.Verify(
            p => p.UpdateUserNameInAllPlacesAsync("CharlieOld", "CharlieNew"),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateOrUpdate_ShouldCallUpdateInPlaces_WhenUsernameChanged()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user = new UserModel("400", "OldName");
        await _collection.InsertOneAsync(user);

        await _repository.CreateOrUpdate("400", "NewName");

        _placeServiceMock.Verify(
            p => p.UpdateUserNameInAllPlacesAsync("OldName", "NewName"),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateOrUpdate_ShouldNotUpdateDeletedUser()
    {
        await _collection.DeleteManyAsync(_ => true);

        var user = new UserModel("500", "Ghost")
        {
            IsDeleted = true
        };

        await _collection.InsertOneAsync(user);

        // Deleted user -> Create new one
        await _repository.CreateOrUpdate("500", "GhostNew");

        var users = await _collection.Find(_ => true).ToListAsync();

        Assert.Equal(2, users.Count); // старый + новый
        Assert.Contains(users, u => u.UserName == "GhostNew");
    }
}
