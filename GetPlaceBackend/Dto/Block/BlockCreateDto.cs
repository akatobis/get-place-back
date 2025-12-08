using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Block;

public class BlockCreateDto
{
    public string PlaceShortId { get; set; }
    public string GridId { get; set; }
    public int LeftTopX { get; set; }
    public int LeftTopY { get; set; }
    public int RightBottomX { get; set; }
    public int RightBottomY { get; set; }
    public string Name { get; set; }
}