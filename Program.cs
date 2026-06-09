using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using iNaturalist_Lite.Data;
using iNaturalist_Lite.Services;
using iNaturalist_Lite.Endpoints;

// --- 1. KÜLTÜR AYARI ---
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day));

// --- 2. SERVİSLER ---
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader()));

// JWT (Auth için)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_naturalist_key_minimum_32_chars_123456789";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "iNaturalistLite";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "iNaturalistLite";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddHttpClient();

// Kendi yazdığımız servisleri register edelim
builder.Services.AddScoped<PlantNetValidationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();

builder.Services.AddAuthorization();

// Veritabanı
builder.Services.AddDbContext<BiodiversityContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        x => x.UseNetTopologySuite());
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();

// Otomatik Veritabanı Migrations (Buluta tabloları kurmak için)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<iNaturalist_Lite.Data.BiodiversityContext>();
    db.Database.Migrate();
}


// --- 3. PİPELİNE ---
// app.UseHttpsRedirection(); // Mobil cihazlarda yerel IP üzerinden self-signed sertifika hatası verdiği için kapatıldı.
app.UseDefaultFiles();
app.UseStaticFiles(); // wwwroot
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// --- 4. ENDPOINTLER ---
// Render'da uyku modunu engellemek için basit HealthCheck (Cron Job veya Uptime Robot ile ping atılabilir)
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// Sistem Bağımlılıkları Test Endpoint'i (Neon DB & Cloudinary)
app.MapGet("/api/system-health", async (iNaturalist_Lite.Data.BiodiversityContext db, IConfiguration config) =>
{
    var health = new System.Collections.Generic.Dictionary<string, object>();
    
    // DB Test
    try
    {
        bool canConnect = await db.Database.CanConnectAsync();
        health["Database"] = canConnect ? "Connected (Neon)" : "Disconnected";
    }
    catch (Exception ex)
    {
        health["Database"] = $"Error: {ex.Message}";
    }

    // Cloudinary Test
    try
    {
        var cloudName = config["Cloudinary:CloudName"] ?? Environment.GetEnvironmentVariable("Cloudinary__CloudName");
        var apiKey = config["Cloudinary:ApiKey"] ?? Environment.GetEnvironmentVariable("Cloudinary__ApiKey");
        health["Cloudinary"] = (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey)) 
            ? $"Configured ({cloudName})" 
            : "Missing Credentials";
    }
    catch (Exception ex)
    {
         health["Cloudinary"] = $"Error: {ex.Message}";
    }

    return Results.Ok(new {
        status = "System Check Completed",
        details = health,
        time = DateTime.UtcNow
    });
});
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPlantEndpoints();

app.Run();
