using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Block;

public class BlockDeleteDto
{
    public BlockDeleteDto(string placeShortId, string gridId, string blockId)
    {
        PlaceShortId = placeShortId;
        GridId = gridId;
        BlockId = blockId;
    }

    public string PlaceShortId { get; set; }
    public string GridId { get; set; }
    public string BlockId { get; set; }
}