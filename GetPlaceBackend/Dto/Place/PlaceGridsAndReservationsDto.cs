using GetPlaceBackend.Models;

namespace GetPlaceBackend.Dto.Place;

public class PlaceGridsAndReservationsDto
{
    public List<Grid> Grids { get; set; }
    public List<Reservation> Reservations { get; set; }
}