using GetPlaceBackend.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GetPlaceBackend.Models;

public class PlaceModel
{
    public PlaceModel(string placeShortId, string ownerId, string name, string description)
    {
        PlaceId = ObjectId.GenerateNewId().ToString();
        PlaceShortId = placeShortId;
        OwnerId = ownerId;
        Name = name;
        Description = description;
        Grids = [ new Grid() ];
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PlaceId { get; set; }
    public string PlaceShortId { get; set; }
    public string OwnerId { get; set; }
    public string Color { get; set; } = "#8a7f8e";
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsDeleted { get; set; } = false;

    [BsonRepresentation(BsonType.Int32)] 
    public AccessPlaceEnum Visible { get; set; } = AccessPlaceEnum.all;
    
    [BsonRepresentation(BsonType.Int32)] 
    public AccessPlaceEnum Editable { get; set; } = AccessPlaceEnum.nobody;
    
    [BsonRepresentation(BsonType.Int32)] 
    public AccessPlaceEnum Reservable { get; set; } = AccessPlaceEnum.all;

    public List<string> GroupIds { get; set; } = [];
    public List<UserAccess> UserAccesses { get; set; } = [];
    public List<Grid> Grids { get; set; } = [];
    public List<Reservation> Reservations { get; set; } = [];
}