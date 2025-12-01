using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Globalization;
using Microsoft.Extensions.FileProviders;

// --- Kultur Ayarı ---
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// --- Servisler ---
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<BiodiversityContext>(options =>
    options.UseNpgsql(
        "Host=localhost;Database=BiodiversityDB;Username=postgres;Password=12345",
        x => x.UseNetTopologySuite()));

var app = builder.Build();

// --- Pipeline ---
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

// --- 🔥 Uploads klasörünü static file olarak yayınla ---
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

// ---------------------------------------------------------
//  FOTOĞRAF YÜKLE: /api/plants/upload
// ---------------------------------------------------------
app.MapPost("/api/plants/upload", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Form-data olmalı.");

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null || file.Length == 0)
        return Results.BadRequest("Dosya bulunamadı.");

    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
    if (!Directory.Exists(uploadDir))
        Directory.CreateDirectory(uploadDir);

    var savePath = Path.Combine(uploadDir, file.FileName);

    using (var stream = new FileStream(savePath, FileMode.Create))
        await file.CopyToAsync(stream);

    var url = "/Uploads/" + file.FileName;

    return Results.Ok(new { url });
});

// ---------------------------------------------------------
// TÜM KAYITLARI GETİR
// ---------------------------------------------------------
app.MapGet("/api/plants", async (BiodiversityContext db) =>
{
    try
    {
        var plants = await db.Plants
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.PhotoUrl,
                p.UserName,
                p.UserBadge,
                p.CreatedAt,
                Lat = p.Lat != 0 ? p.Lat : (p.Location != null ? p.Location.Y : 0),
                Lng = p.Lng != 0 ? p.Lng : (p.Location != null ? p.Location.X : 0)
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Results.Ok(plants);
    }
    catch (Exception ex)
    {
        return Results.Problem("Veri alınamadı: " + ex.Message);
    }
});

// ---------------------------------------------------------
// YENİ BİTKİ KAYDET
// ---------------------------------------------------------
app.MapPost("/api/plants", async ([FromBody] PlantInput input, BiodiversityContext db) =>
{
    try
    {
        if (input == null)
            return Results.BadRequest("Veri yok.");

        if (double.IsNaN(input.Lat) || double.IsNaN(input.Lng))
            return Results.BadRequest("Koordinat hatalı.");

        var plant = new Plant
        {
            Name = input.Name,
            Description = input.Description,
            PhotoUrl = input.PhotoUrl,
            UserName = input.UserName ?? "Misafir",
            UserBadge = input.UserBadge ?? "🌱",
            Lat = input.Lat,
            Lng = input.Lng,
            CreatedAt = DateTime.UtcNow,
            Location = new Point(input.Lng, input.Lat) { SRID = 4326 }
        };

        db.Plants.Add(plant);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Kayıt tamamlandı." });
    }
    catch (Exception ex)
    {
        return Results.Problem("Kayıt hatası: " + ex.Message);
    }
});

app.Run();

// ----------------------- MODELLER -----------------------

public class PlantInput
{
    public string Name { get; set; }
    public string Description { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string PhotoUrl { get; set; }
    public string UserName { get; set; }
    public string UserBadge { get; set; }
}

public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string UserName { get; set; }
    public string UserBadge { get; set; }
    public Point Location { get; set; }
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
        modelBuilder.Entity<Plant>().Property(p => p.Lat).HasColumnName("lat");
        modelBuilder.Entity<Plant>().Property(p => p.Lng).HasColumnName("lng");
        modelBuilder.Entity<Plant>().Property(p => p.UserName).HasColumnName("username");
        modelBuilder.Entity<Plant>().Property(p => p.UserBadge).HasColumnName("userbadge");
        modelBuilder.Entity<Plant>().Property(p => p.Location).HasColumnName("location");
    }
}
