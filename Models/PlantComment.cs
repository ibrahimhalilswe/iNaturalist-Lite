namespace iNaturalist_Lite.Models;

public class PlantComment
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public required string Username { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; }
}
