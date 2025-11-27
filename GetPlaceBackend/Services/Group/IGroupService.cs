using GetPlaceBackend.Models;
using MongoDB.Bson;

namespace GetPlaceBackend.Services.Group;

public interface IGroupService
{
    Task<List<GroupModel>> GetAll(ObjectId userId);
    Task<GroupModel?> GetByIdAsync(ObjectId id);
    Task AddAsync(string name, ObjectId userId);
    Task<bool> SoftDeleteAsync(ObjectId id);
    Task<bool> RenameAsync(ObjectId id, string newName);
    Task<bool> UpdateOrderAsync(ObjectId id, int newOrder);
}