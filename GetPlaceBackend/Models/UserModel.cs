using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class UserModel
{
    public UserModel(string tgid, string username)
    {
        TgId = tgid;
        UserName = username;
    }
    
    [BsonId]
    public ObjectId UserId { get; set; }
    public string TgId { get; set; }
    public string UserName { get; set; }
    public bool IsDeleted { get; set; } = false;
}