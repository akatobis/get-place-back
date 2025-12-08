using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Reservation;

public class ReservationDeleteDto
{
    public ReservationDeleteDto(string placeShortId, string reservationId)
    {
        PlaceShortId = placeShortId;
        ReservationId = reservationId;
    }

    public string PlaceShortId { get; set; }
    public string ReservationId  { get; set; }
}