using GetPlaceBackend.Models.Enums;

namespace GetPlaceBackend.Dto.UserAccess;

public class UserAccessGetDto
{
    public string UserName { get; set; } = "";
    public AccessUserEnum AccessUser { get; set; }
}