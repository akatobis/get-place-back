using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Block;

public class BlockDeleteDto
{
    public string PlaceShortId { get; set; }
    public ObjectId GridId { get; set; }
    public ObjectId BlockId { get; set; }
}