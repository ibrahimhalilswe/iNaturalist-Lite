using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iNaturalist_Lite.Data;
using iNaturalist_Lite.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace iNaturalist_Lite.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/user/profile", [Authorize] async (HttpContext ctx, BiodiversityContext db) =>
        {
            var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user == null) return Results.NotFound();

            var totalObservations = await db.Plants.CountAsync(p => p.UserName == user.Username);
            var discoveredSpecies = await db.Plants.Where(p => p.UserName == user.Username).Select(p => p.Name).Distinct().CountAsync();

            return Results.Ok(new 
            { 
                user.Username, 
                user.Email, 
                user.Badge, 
                user.AvatarUrl, 
                user.CreatedAt,
                totalObservations,
                discoveredSpecies
            });
        });

        app.MapGet("/api/user/my-plants", [Authorize] async (HttpContext ctx, BiodiversityContext db) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

            var plants = await db.Plants
                .Where(p => p.UserName == username)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id, p.Name, p.Description, p.PhotoUrl, p.UserName, p.UserBadge, p.CreatedAt,
                    Lat = p.Location != null ? p.Location.Y : 0,
                    Lng = p.Location != null ? p.Location.X : 0
                }).ToListAsync();

            return Results.Ok(plants);
        });

        app.MapGet("/api/user/liked-plants", [Authorize] async (HttpContext ctx, BiodiversityContext db) =>
        {
            var username = ctx.User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

            var likedPlants = await db.Likes
                .Where(l => l.Username == username)
                .Join(db.Plants, l => l.PlantId, p => p.Id, (l, p) => p)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id, p.Name, p.Description, p.PhotoUrl, p.UserName, p.UserBadge, p.CreatedAt,
                    Lat = p.Location != null ? p.Location.Y : 0,
                    Lng = p.Location != null ? p.Location.X : 0
                }).ToListAsync();

            return Results.Ok(likedPlants);
        });

        app.MapPost("/api/user/profile/update", [Authorize] async ([FromBody] UpdateProfileInput input, HttpContext ctx, BiodiversityContext db) =>
        {
            var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user == null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(input.Username) && input.Username != user.Username)
            {
                if (await db.Users.AnyAsync(u => u.Username == input.Username))
                    return Results.BadRequest("Bu kullanıcı adı alınmış.");
                user.Username = input.Username;
            }
            
            if (!string.IsNullOrWhiteSpace(input.AvatarUrl))
            {
                user.AvatarUrl = input.AvatarUrl;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, username = user.Username, avatarUrl = user.AvatarUrl });
        });

        // ── HERKESE AÇIK PROFİL (PUBLIC) ──────────────────────────────────

        app.MapGet("/api/users/{username}/profile", async (string username, BiodiversityContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return Results.NotFound("Kullanıcı bulunamadı.");

            var totalObservations = await db.Plants.CountAsync(p => p.UserName.ToLower() == username.ToLower());
            var discoveredSpecies = await db.Plants.Where(p => p.UserName.ToLower() == username.ToLower()).Select(p => p.Name).Distinct().CountAsync();

            return Results.Ok(new 
            { 
                user.Username, 
                user.Badge, 
                user.AvatarUrl, 
                user.CreatedAt,
                totalObservations,
                discoveredSpecies
            });
        });

        app.MapGet("/api/users/{username}/plants", async (string username, BiodiversityContext db) =>
        {
            var plants = await db.Plants
                .Where(p => p.UserName.ToLower() == username.ToLower())
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id, p.Name, p.Description, p.PhotoUrl, p.UserName, p.UserBadge, p.CreatedAt,
                    Lat = p.Location != null ? p.Location.Y : 0,
                    Lng = p.Location != null ? p.Location.X : 0
                }).ToListAsync();

            return Results.Ok(plants);
        });
    }
}
