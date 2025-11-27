using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class GroupModel
{
    [BsonId]
    public ObjectId GroupId { get; set; }

    public string Name { get; set; } = "";

    public int Order { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public ObjectId UserId { get; set; }
}