using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class Reservation
{
    [BsonId]
    public ObjectId ReservationId { get; set; }
    public ObjectId GridId { get; set; }
    public ObjectId BlockId { get; set; }
    public DateTime DateTimeStart { get; set; }
    public DateTime DateTimeEnd { get; set; }
}