namespace GetPlaceBackend.Dto.Place;

public class CardPlaceDto
{
    public CardPlaceDto(string color, string name, string description, List<string> groupNames)
    {
        Color = color;
        Name = name;
        Description = description;
        GroupNames = groupNames;
    }

    public string Color { get; set; }
    public string Name { get; set; }
    public string Description { get; set;}
    public List<string> GroupNames { get; set;}
}