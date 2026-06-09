using NetTopologySuite.Geometries;

namespace iNaturalist_Lite.Models;

public class Plant
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = "Misafir";
    public string UserBadge { get; set; } = "🌱";
    public Point? Location { get; set; }
}
