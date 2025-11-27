using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Block;

public class BlockUpdateNameDto
{
    public string PlaceShortId { get; set; }
    public ObjectId GridId { get; set; }
    public ObjectId BlockId { get; set; }
    public string Name { get; set; }
}