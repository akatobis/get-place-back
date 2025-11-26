using GetPlaceBackend.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class PlaceModel
{
    [BsonId]
    public ObjectId PlaceId { get; set; }
    public string PlaceShortId { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Color { get; set; } = "#8a7f8e";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    
    [BsonRepresentation(BsonType.Int32)] 
    public AccessPlaceEnum Visible { get; set; }
    
    [BsonRepresentation(BsonType.Int32)] 
    public AccessPlaceEnum Editable { get; set; }
    
    [BsonRepresentation(BsonType.Int32)] 
    public AccessPlaceEnum Reservable { get; set; }

    public List<ObjectId> GroupIds { get; set; } = [];
    public List<UserAccess> UserAccesses { get; set; } = [];
    public List<Grid> Grids { get; set; } = [];
    public List<Reservation> Reservations { get; set; } = [];
}