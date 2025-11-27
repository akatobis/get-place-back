using System.Reflection.Metadata.Ecma335;
using GetPlaceBackend.Models;
using MongoDB.Driver;

namespace GetPlaceBackend.Services.User;

public class UserService : IUserService
{
    private readonly IMongoCollection<UserModel> _collectionDb;

    public UserService(IMongoDatabase db)
    {
        _collectionDb = db.GetCollection<UserModel>("users");
    }
    
    public async Task<UserModel?> GetById(string tgId)
    {
        return await _collectionDb
            .Find(g => g.TgId == tgId && !g.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<UserModel?> GetByUsername(string username)
    {
        return await _collectionDb
            .Find(g => g.UserName == username && !g.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task CreateOrUpdate(string tgId, string username)
    {
        var findUser = await GetById(tgId);
        if (findUser != null && findUser.UserName == username)
            return;
        
        if (findUser == null)
        {
            var userModel = new UserModel(tgId, username);
            await _collectionDb.InsertOneAsync(userModel);
            return;
        }

        if (findUser.UserName != username)
        {
            // заменить имя пользователя в месте 
            findUser.UserName = username;
            var update = Builders<UserModel>.Update.Set(g => g.UserName, username);
            await _collectionDb.UpdateOneAsync(g => g.TgId == tgId, update);
        }
    }
}