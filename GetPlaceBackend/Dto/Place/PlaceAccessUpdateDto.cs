using GetPlaceBackend.Models.Enums;

namespace GetPlaceBackend.Dto.Place;

public class PlaceAccessUpdateDto
{
    public string PlaceId { get; set; } = "";
    public NameAccessPlaceEnum NameAccessPlaceEnum { get; set; }
    public AccessPlaceEnum AccessPlaceEnum { get; set; }
}