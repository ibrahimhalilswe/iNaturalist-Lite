using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iNaturalist_Lite.Data;
using iNaturalist_Lite.DTOs;
using iNaturalist_Lite.Models;
using NetTopologySuite.Geometries;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using iNaturalist_Lite.Services;

namespace iNaturalist_Lite.Endpoints;

public static class PlantEndpoints
{
    public static void MapPlantEndpoints(this WebApplication app)
    {
        app.MapPost("/api/identify", async (HttpRequest request, IHttpClientFactory httpClientFactory, IConfiguration config) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Form verisi bekleniyor.");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("images");
            var organ = form["organs"].ToString();
            if (string.IsNullOrEmpty(organ)) organ = "flower";

            if (file == null || file.Length == 0)
                return Results.BadRequest("Dosya boş.");

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("Dosya 5MB'dan büyük olamaz.");

            var apiKey = config["PlantNet:ApiKey"];
            var url = $"https://my-api.plantnet.org/v2/identify/all?api-key={apiKey}&include-related-images=false&no-reject=false&lang=tr";

            using var ms = new MemoryStream();
            using (var readStream = file.OpenReadStream())
                await readStream.CopyToAsync(ms);
            ms.Position = 0;

            var client = httpClientFactory.CreateClient();
            try
            {
                var multipart = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(ms.ToArray());
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");
                multipart.Add(imageContent, "images", file.FileName);
                multipart.Add(new StringContent(organ), "organs");

                var response = await client.PostAsync(url, multipart);
                var body = await response.Content.ReadAsStringAsync();
                return Results.Content(body, "application/json", System.Text.Encoding.UTF8, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return Results.Problem("PlantNet hatası: " + ex.Message);
            }
        });

        app.MapPost("/api/plants/upload", async (HttpRequest request, PlantNetValidationService validationService, IStorageService storageService) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Form verisi bekleniyor.");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file == null || file.Length == 0)
                return Results.BadRequest("Dosya boş.");

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("Dosya 5MB sınırını aşıyor.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Results.BadRequest("Sadece jpg, jpeg, png, webp, gif uzantılı dosyalar kabul edilir.");

            var signatures = new Dictionary<string, byte[]>
            {
                { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
                { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
                { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
                { ".gif",  new byte[] { 0x47, 0x49, 0x46 } },
                { ".webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } },
            };

            using var peekStream = file.OpenReadStream();
            var header = new byte[8];
            await peekStream.ReadExactlyAsync(header, 0, header.Length);

            if (signatures.TryGetValue(ext, out var sig))
            {
                if (!header.Take(sig.Length).SequenceEqual(sig))
                    return Results.BadRequest("Dosya içeriği uzantısıyla eşleşmiyor.");
            }

            // AI IMAGE VALIDATION (PlantNet)
            peekStream.Position = 0;
            var aiResult = await validationService.IsPlantImageAsync(peekStream, file.ContentType);
            if (!aiResult.isValid)
            {
                return Results.BadRequest(aiResult.message);
            }

            using var uploadStream = file.OpenReadStream();
            var url = await storageService.SaveFileAsync(uploadStream, ext);

            return Results.Ok(new { url });
        });

        app.MapGet("/api/plants", async (BiodiversityContext db, int page = 1, int pageSize = 20) =>
        {
            try
            {
                var query = db.Plants.AsQueryable();
                var total = await query.CountAsync();
                
                var plants = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.PhotoUrl,
                        p.UserName,
                        p.UserBadge,
                        p.CreatedAt,
                        Lat = p.Location != null ? p.Location.Y : 0,
                        Lng = p.Location != null ? p.Location.X : 0
                    })
                    .ToListAsync();

                return Results.Ok(new { plants, total, page, pageSize });
            }
            catch (Exception ex)
            {
                return Results.Problem("Veri çekilemedi: " + ex.Message);
            }
        });

        app.MapPost("/api/plants", [Authorize] async ([FromBody] PlantInput input, BiodiversityContext db, HttpContext ctx) =>
        {
            try
            {
                if (input == null || string.IsNullOrEmpty(input.Name))
                    return Results.BadRequest("Bitki adı gerekli.");

                var sessionUser = ctx.User.FindFirstValue(ClaimTypes.Name);
                var sessionBadge = ctx.User.FindFirstValue("badge");

                var plant = new Plant
                {
                    Name = input.Name,
                    Description = input.Description,
                    PhotoUrl = input.PhotoUrl,
                    UserName = sessionUser ?? input.UserName ?? "Misafir",
                    UserBadge = sessionBadge ?? input.UserBadge ?? "🌱",
                    Location = new Point(input.Lng, input.Lat) { SRID = 4326 },
                    CreatedAt = DateTime.UtcNow
                };

                db.Plants.Add(plant);
                await db.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                return Results.Problem("Kayıt hatası: " + ex.Message);
            }
        });

        app.MapGet("/api/plants/stats", async (BiodiversityContext db) =>
        {
            try
            {
                var total = await db.Plants.CountAsync();
                var uniquePlants = await db.Plants.Select(p => p.Name).Distinct().CountAsync();
                var activeUsers = await db.Plants.Select(p => p.UserName).Distinct().CountAsync();
                return Results.Ok(new { totalObservations = total, uniquePlants, activeUsers });
            }
            catch (Exception ex)
            {
                return Results.Problem("İstatistik hatası: " + ex.Message);
            }
        });

        app.MapPost("/api/plants/{id}/like", [Authorize] async (int id, BiodiversityContext db, HttpContext ctx) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            if (username == null) return Results.Unauthorized();

            var existing = await db.Likes.FirstOrDefaultAsync(l => l.PlantId == id && l.Username == username);
            bool liked;
            if (existing != null)
            {
                db.Likes.Remove(existing);
                liked = false;
            }
            else
            {
                db.Likes.Add(new PlantLike { PlantId = id, Username = username });
                liked = true;
            }

            await db.SaveChangesAsync();
            var count = await db.Likes.CountAsync(l => l.PlantId == id);
            return Results.Ok(new { liked, count });
        });

        app.MapGet("/api/plants/{id}/likes", async (int id, BiodiversityContext db, HttpContext ctx) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            var count = await db.Likes.CountAsync(l => l.PlantId == id);
            var liked = username != null && await db.Likes.AnyAsync(l => l.PlantId == id && l.Username == username);
            return Results.Ok(new { count, liked });
        });

        app.MapPost("/api/plants/{id}/comments", [Authorize] async (int id, [FromBody] CommentInput input, BiodiversityContext db, HttpContext ctx) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            if (username == null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(input.Text))
                return Results.BadRequest("Yorum boş olamaz.");

            db.Comments.Add(new PlantComment
            {
                PlantId = id,
                Username = username,
                Text = input.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        app.MapGet("/api/plants/{id}/comments", async (int id, BiodiversityContext db) =>
        {
            var comments = await db.Comments
                .Where(c => c.PlantId == id)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new { c.Username, c.Text, c.CreatedAt })
                .ToListAsync();

            return Results.Ok(comments);
        });

        app.MapDelete("/api/plants/{id}", [Authorize] async (int id, BiodiversityContext db, HttpContext ctx) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            if (username == null) return Results.Unauthorized();

            var plant = await db.Plants.FindAsync(id);
            if (plant == null) return Results.NotFound();

            if (plant.UserName != username) return Results.Forbid();

            db.Plants.Remove(plant);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true });
        });

        app.MapDelete("/api/plants/{id}/comments/{commentId}", [Authorize] async (int id, int commentId, BiodiversityContext db, HttpContext ctx) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            if (username == null) return Results.Unauthorized();

            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.PlantId == id);
            if (comment == null) return Results.NotFound();

            if (comment.Username != username) return Results.Forbid();

            db.Comments.Remove(comment);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true });
        });
    }
}
