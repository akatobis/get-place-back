using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Block;

public class BlockUpdateNameDto
{
    public string PlaceShortId { get; set; }
    public string GridId { get; set; }
    public string BlockId { get; set; }
    public string Name { get; set; }
}