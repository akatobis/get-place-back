using GetPlaceBackend.Models.Enums;

namespace GetPlaceBackend.Dto.UserAccess;

public class UserAccessAddDto
{
    public string PlaceShortId { get; set; }
    public string UserName { get; set; }
    public AccessUserEnum AccessUser { get; set; }
}