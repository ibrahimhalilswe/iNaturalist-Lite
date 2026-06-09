using Microsoft.EntityFrameworkCore;
using iNaturalist_Lite.Models;

namespace iNaturalist_Lite.Data;

public class BiodiversityContext(DbContextOptions<BiodiversityContext> options) : DbContext(options)
{
    public DbSet<Plant> Plants { get; set; } = null!;
    public DbSet<AppUser> Users { get; set; } = null!;
    public DbSet<PlantLike> Likes { get; set; } = null!;
    public DbSet<PlantComment> Comments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Plants
        modelBuilder.Entity<Plant>().ToTable("plants");
        modelBuilder.Entity<Plant>().Property(p => p.Id).HasColumnName("id");
        modelBuilder.Entity<Plant>().Property(p => p.Name).HasColumnName("name");
        modelBuilder.Entity<Plant>().Property(p => p.Description).HasColumnName("description");
        modelBuilder.Entity<Plant>().Property(p => p.PhotoUrl).HasColumnName("photourl");
        modelBuilder.Entity<Plant>().Property(p => p.CreatedAt).HasColumnName("createdat");
        modelBuilder.Entity<Plant>().Property(p => p.Location).HasColumnName("location");
        modelBuilder.Entity<Plant>().Property(p => p.UserName).HasColumnName("username");
        modelBuilder.Entity<Plant>().Property(p => p.UserBadge).HasColumnName("userbadge");

        // Users
        modelBuilder.Entity<AppUser>().ToTable("users");
        modelBuilder.Entity<AppUser>().Property(u => u.Id).HasColumnName("id");
        modelBuilder.Entity<AppUser>().Property(u => u.Username).HasColumnName("username");
        modelBuilder.Entity<AppUser>().Property(u => u.Email).HasColumnName("email");
        modelBuilder.Entity<AppUser>().Property(u => u.PasswordHash).HasColumnName("password_hash");
        modelBuilder.Entity<AppUser>().Property(u => u.Badge).HasColumnName("badge");
        modelBuilder.Entity<AppUser>().Property(u => u.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<AppUser>().Property(u => u.OtpCode).HasColumnName("otp_code");
        modelBuilder.Entity<AppUser>().Property(u => u.OtpExpiry).HasColumnName("otp_expiry");
        modelBuilder.Entity<AppUser>().Property(u => u.AvatarUrl).HasColumnName("avatar_url");
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();

        // Likes
        modelBuilder.Entity<PlantLike>().ToTable("plant_likes");
        modelBuilder.Entity<PlantLike>().Property(l => l.Id).HasColumnName("id");
        modelBuilder.Entity<PlantLike>().Property(l => l.PlantId).HasColumnName("plant_id");
        modelBuilder.Entity<PlantLike>().Property(l => l.Username).HasColumnName("username");
        modelBuilder.Entity<PlantLike>().HasIndex(l => new { l.PlantId, l.Username }).IsUnique();
        modelBuilder.Entity<PlantLike>()
            .HasOne<Plant>()
            .WithMany()
            .HasForeignKey(l => l.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comments
        modelBuilder.Entity<PlantComment>().ToTable("plant_comments");
        modelBuilder.Entity<PlantComment>().Property(c => c.Id).HasColumnName("id");
        modelBuilder.Entity<PlantComment>().Property(c => c.PlantId).HasColumnName("plant_id");
        modelBuilder.Entity<PlantComment>().Property(c => c.Username).HasColumnName("username");
        modelBuilder.Entity<PlantComment>().Property(c => c.Text).HasColumnName("text");
        modelBuilder.Entity<PlantComment>().Property(c => c.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<PlantComment>()
            .HasOne<Plant>()
            .WithMany()
            .HasForeignKey(c => c.PlantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
