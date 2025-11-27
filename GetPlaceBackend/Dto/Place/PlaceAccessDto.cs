using GetPlaceBackend.Models.Enums;

namespace GetPlaceBackend.Dto.Place;

public class PlaceAccessDto
{
    public string Id { get; set; }
    public AccessPlaceEnum Visible { get; set; }
    public AccessPlaceEnum Editable { get; set; }
    public AccessPlaceEnum Reservable { get; set; }
}