using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class Grid
{
    [BsonId]
    public ObjectId GridId { get; set; }
    public List<Block> Blocks { get; set; } = [];
}