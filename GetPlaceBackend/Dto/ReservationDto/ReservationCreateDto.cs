using MongoDB.Bson;

namespace GetPlaceBackend.Dto.Reservation;

public class ReservationCreateDto
{
    public string PlaceShortId { get; set; }
    public string GridId { get; set; }
    public string BlockId { get; set; }
    public DateTime DateTimeStart { get; set; }
    public DateTime DateTimeEnd { get; set; }
}