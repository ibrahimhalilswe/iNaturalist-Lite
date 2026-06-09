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
builder.Services.AddScoped<IStorageService, LocalDiskStorageService>();

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

// --- 3. PİPELİNE ---
// app.UseHttpsRedirection(); // Mobil cihazlarda yerel IP üzerinden self-signed sertifika hatası verdiği için kapatıldı.
app.UseDefaultFiles();
app.UseStaticFiles(); // wwwroot
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Uploads klasörünü dışa aç
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

// --- 4. ENDPOINTLER ---
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPlantEndpoints();

app.Run();
