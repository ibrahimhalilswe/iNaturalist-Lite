namespace iNaturalist_Lite.DTOs;

public class PlantInput
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? PhotoUrl { get; set; }
    public string? UserName { get; set; }
    public string? UserBadge { get; set; }
}

public class CommentInput
{
    public required string Text { get; set; }
}
