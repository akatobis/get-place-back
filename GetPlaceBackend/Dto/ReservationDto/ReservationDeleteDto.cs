using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Reservation;

public class ReservationDeleteDto
{
    public string PlaceShortId { get; set; }
    public ObjectId GridId { get; set; }
    public ObjectId BlockId { get; set; }
    public DateTime DateTimeStart { get; set; }
    public DateTime DateTimeEnd { get; set; }
}