using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class Grid
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string GridId { get; set; } = ObjectId.GenerateNewId().ToString();
    public List<Block> Blocks { get; set; } = [];
}