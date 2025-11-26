using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class Block
{
    [BsonId]
    public ObjectId BlockId { get; set; }
    public int LeftTopX { get; set; }
    public int LeftTopY { get; set; }
    public int RightBottomX { get; set; }
    public int RightBottomY { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#8a7f8e";
}