using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

// 1. KÜLTÜR AYARI (Nokta/Virgül sorununu kökten çözer)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// 2. SERVİSLER
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<BiodiversityContext>(options =>
    options.UseNpgsql("Host=localhost;Database=BiodiversityDB;Username=postgres;Password=12345"));

var app = builder.Build();

// 3. UYGULAMA AYARLARI
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

// 4. API UÇLARI

// GET: Listeleme
app.MapGet("/api/plants", async (BiodiversityContext db) =>
    await db.Plants.OrderByDescending(p => p.CreatedAt).ToListAsync());

// POST: KAYIT (HATA ÇÖZÜLDÜ: Lat/Lng artık double kabul ediyor)
app.MapPost("/api/plants", async ([FromBody] PlantInput input, BiodiversityContext db) =>
{
    // Not: PostGIS fonksiyonunda önce Boylam (Lng), sonra Enlem (Lat) gelir.
    string sql = "INSERT INTO plants (name, description, photourl, createdat, location) VALUES ({0}, {1}, 'demo.jpg', NOW(), ST_SetSRID(ST_MakePoint({2}, {3}), 4326))";

    // Input değerleri double olduğu için veritabanına doğrudan, hatasız gider.
    await db.Database.ExecuteSqlRawAsync(sql, input.Name, input.Description, input.Lng, input.Lat);

    return Results.Ok(new { message = "Kayıt Başarılı", bitki = input.Name });
});

app.Run();

// 5. MODELLER (DÜZELTME BURADA)

public class PlantInput
{
    public string Name { get; set; }
    public string Description { get; set; }

    // DÜZELTME: Frontend sayı gönderdiği için bunları DOUBLE yaptık!
    // Artık hata vermeyecek.
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BiodiversityContext : DbContext
{
    public BiodiversityContext(DbContextOptions<BiodiversityContext> options) : base(options) { }
    public DbSet<Plant> Plants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plant>().ToTable("plants");
        modelBuilder.Entity<Plant>().Property(p => p.Id).HasColumnName("id");
        modelBuilder.Entity<Plant>().Property(p => p.Name).HasColumnName("name");
        modelBuilder.Entity<Plant>().Property(p => p.Description).HasColumnName("description");
        modelBuilder.Entity<Plant>().Property(p => p.PhotoUrl).HasColumnName("photourl");
        modelBuilder.Entity<Plant>().Property(p => p.CreatedAt).HasColumnName("createdat");
    }
}