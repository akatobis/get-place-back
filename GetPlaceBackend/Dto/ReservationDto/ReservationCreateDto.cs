using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Reservation;

public class ReservationCreateDto
{
    public string PlaceShortId { get; set; }
    public ObjectId GridId { get; set; }
    public ObjectId BlockId { get; set; }
    public DateTime DateTimeStart { get; set; }
    public DateTime DateTimeEnd { get; set; }
}