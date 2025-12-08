using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class GroupModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string GroupId { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = "";

    public int Order { get; set; }
    
    public bool IsDeleted { get; set; } = false;

    public string UserId { get; set; } = "";
}