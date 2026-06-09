namespace iNaturalist_Lite.Models;

public class AppUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Badge { get; set; } = "🌱";
    public DateTime CreatedAt { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public string? AvatarUrl { get; set; }
}
