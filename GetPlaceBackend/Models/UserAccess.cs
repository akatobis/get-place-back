using GetPlaceBackend.Models.Enums;

namespace GetPlaceBackend.Models;

public class UserAccess
{
    public string UserName { get; set; } = "";
    public AccessUserEnum AccessUserEnum { get; set; }
}