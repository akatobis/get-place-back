using GetPlaceBackend.Models;

namespace GetPlaceBackend.Services.User;

public interface IUserService
{
    Task<UserModel?> GetById(string tgId);
    Task<UserModel?> GetByUsername(string username);
    Task CreateOrUpdate(string tgId, string username);
}