using GetPlaceBackend.Models;
using MongoDB.Bson;

namespace GetPlaceBackend.Services.Group;

public interface IGroupService
{
    Task<List<GroupModel>> GetAll(string userId);
    Task<GroupModel?> GetByIdAsync(string id);
    Task<string> AddAsync(string name, string userId);
    Task<bool> SoftDeleteAsync(string id);
    Task<bool> RenameAsync(string id, string newName);
    Task<bool> UpdateOrderAsync(string id, int newOrder);
}