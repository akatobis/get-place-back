namespace GetPlaceBackend.Dto.Place;

public class PlaceCreateDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string OwnerId { get; set; } = "";
}