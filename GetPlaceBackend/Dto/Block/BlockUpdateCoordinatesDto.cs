using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Block;

public class BlockUpdateCoordinatesDto
{
    public string PlaceShortId { get; set; }
    public ObjectId GridId { get; set; }
    public ObjectId BlockId { get; set; }

    public int LeftTopX { get; set; }
    public int LeftTopY { get; set; }
    public int RightBottomX { get; set; }
    public int RightBottomY { get; set; }
}