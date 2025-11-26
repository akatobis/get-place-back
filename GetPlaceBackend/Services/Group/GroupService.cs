using GetPlaceBackend.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GetPlaceBackend.Services.Group;

public class GroupService : IGroupService
{
    private readonly IMongoCollection<GroupModel> _collectionDb;

    public GroupService(IMongoDatabase db)
    {
        _collectionDb = db.GetCollection<GroupModel>("groups");
    }

    public async Task<List<GroupModel>> GetAll(ObjectId userId)
    {
        return await _collectionDb
            .Find(g => g.UserId == userId && !g.IsDeleted)
            .SortBy(g => g.Order)
            .ToListAsync();
    }

    public async Task<GroupModel?> GetByIdAsync(ObjectId id)
    {
        return await _collectionDb
            .Find(g => g.GroupId == id && !g.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(string name, ObjectId userId)
    {
        var maxOrder = await _collectionDb
            .Find(_ => true)
            .SortByDescending(g => g.Order)
            .Limit(1)
            .Project(g => g.Order)
            .FirstOrDefaultAsync();

        var newGroup = new GroupModel
        {
            Name = name,
            Order = maxOrder + 1,
            UserId = userId,
        };

        await _collectionDb.InsertOneAsync(newGroup);
    }

    public async Task<bool> SoftDeleteAsync(ObjectId id)
    {
        var update = Builders<GroupModel>.Update
            .Set(g => g.IsDeleted, true);

        var result = await _collectionDb.UpdateOneAsync(
            g => g.GroupId == id,
            update
        );

        return result.ModifiedCount > 0;
    }

    public async Task<bool> RenameAsync(ObjectId id, string newName)
    {
        var group = await _collectionDb.Find(g => g.GroupId == id && !g.IsDeleted)
            .FirstOrDefaultAsync();

        if (group == null)
            return false;

        var update = Builders<GroupModel>.Update.Set(g => g.Name, newName);

        await _collectionDb.UpdateOneAsync(g => g.GroupId == id, update);

        return true;
    }

    public async Task<bool> UpdateOrderAsync(ObjectId id, int newOrder)
    {
        var group = await _collectionDb
            .Find(g => g.GroupId == id && !g.IsDeleted)
            .FirstOrDefaultAsync();
        
        if (group == null)
            return false;

        await _collectionDb.UpdateManyAsync(
            g => g.Order >= newOrder,
            Builders<GroupModel>.Update.Inc(g => g.Order, 1)
        );

        await _collectionDb.UpdateOneAsync(
            g => g.GroupId == id,
            Builders<GroupModel>.Update.Set(g => g.Order, newOrder)
        );

        return true;
    }
}