using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class Reservation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReservationId { get; set; } = ObjectId.GenerateNewId().ToString();
    public string GridId { get; set; } = ObjectId.GenerateNewId().ToString();
    public string BlockId { get; set; } = ObjectId.GenerateNewId().ToString();
    public DateTime DateTimeStart { get; set; }
    public DateTime DateTimeEnd { get; set; }
}