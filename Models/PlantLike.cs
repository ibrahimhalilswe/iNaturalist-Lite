namespace iNaturalist_Lite.Models;

public class PlantLike
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public required string Username { get; set; }
}
