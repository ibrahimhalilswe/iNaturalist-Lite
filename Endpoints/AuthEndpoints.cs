using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iNaturalist_Lite.Data;
using iNaturalist_Lite.DTOs;
using iNaturalist_Lite.Models;
using iNaturalist_Lite.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace iNaturalist_Lite.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async ([FromBody] RegisterInput input, BiodiversityContext db, IEmailService emailService, IAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password) || string.IsNullOrWhiteSpace(input.Email))
                return Results.BadRequest("Kullanıcı adı, e-posta ve şifre gerekli.");

            if (input.Password.Length < 6)
                return Results.BadRequest("Şifre en az 6 karakter olmalı.");

            var exists = await db.Users.AnyAsync(u => u.Username == input.Username || u.Email == input.Email);
            if (exists)
                return Results.BadRequest("Bu kullanıcı adı veya e-posta alınmış.");

            var user = new AppUser
            {
                Username = input.Username,
                Email = input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                Badge = "🌱",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            Console.WriteLine($"DEBUG: Mail servisi çağrılmak üzere. Endpoint: Register (Alıcı: {user.Email})");
            var mailError = await emailService.SendWelcomeEmailAsync(user.Email, user.Username);
            Console.WriteLine($"DEBUG: Register - Mail Endpoint Sonucu: '{mailError}'");

            var token = authService.GenerateJwtToken(user);
            if (!string.IsNullOrEmpty(mailError))
                return Results.Ok(new { success = true, token, username = user.Username, badge = user.Badge, message = $"Kayıt başarılı ancak mail gönderimi başarısız: {mailError}" });
            
            return Results.Ok(new { success = true, token, username = user.Username, badge = user.Badge });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/login", async ([FromBody] UserInput input, BiodiversityContext db, IAuthService authService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == input.Username || u.Email == input.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
                return Results.Unauthorized();

            var token = authService.GenerateJwtToken(user);
            return Results.Ok(new { success = true, token, username = user.Username, email = user.Email, badge = user.Badge, avatarUrl = user.AvatarUrl });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/forgot-password", async ([FromBody] ForgotPasswordInput input, BiodiversityContext db, IEmailService emailService) =>
        {
            Console.WriteLine("DEBUG: ForgotPassword metodu tetiklendi. Email: " + input.Email);

            if (emailService == null)
            {
                Console.WriteLine("DEBUG: HATA! emailService null.");
                return Results.BadRequest("EmailService tanımlı değil.");
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == input.Email);
            if (user == null) return Results.NotFound("Kullanıcı bulunamadı.");

            Console.WriteLine("DEBUG: Kullanıcı bulundu, mail gönderim bloğuna giriliyor...");

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(15);
            await db.SaveChangesAsync();

            Console.WriteLine("DEBUG: Mail servisi çağrılmak üzere...");
            var mailError = await emailService.SendOtpEmailAsync(user.Email, user.Username, otp);
            Console.WriteLine("DEBUG: Mail servisi çağrıldı, sonuç: " + mailError);
            
            if (!string.IsNullOrEmpty(mailError))
                return Results.BadRequest($"Şifre sıfırlama kodu oluşturuldu ancak mail gönderimi başarısız: {mailError}");

            return Results.Ok(new { success = true });

            return Results.Ok(new { success = true });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/reset-password", async ([FromBody] ResetPasswordInput input, BiodiversityContext db, IEmailService emailService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == input.Email && u.OtpCode == input.OtpCode);
            if (user == null || user.OtpExpiry < DateTime.UtcNow)
                return Results.BadRequest("Geçersiz veya süresi dolmuş kod.");

            if (input.NewPassword.Length < 6)
                return Results.BadRequest("Şifre en az 6 karakter olmalı.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;
            await db.SaveChangesAsync();

            Console.WriteLine($"DEBUG: Mail servisi çağrılmak üzere. Endpoint: Reset Password (Alıcı: {user.Email})");
            var mailError = await emailService.SendPasswordChangedEmailAsync(user.Email, user.Username);
            Console.WriteLine($"DEBUG: Reset Password - Mail Endpoint Sonucu: '{mailError}'");
            if (!string.IsNullOrEmpty(mailError))
                return Results.Ok(new { success = true, message = $"Şifre başarıyla sıfırlandı ancak mail gönderimi başarısız: {mailError}" });

            return Results.Ok(new { success = true });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/change-password", [Authorize] async ([FromBody] ChangePasswordInput input, HttpContext ctx, BiodiversityContext db) =>
        {
            var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user == null) return Results.NotFound();

            if (!BCrypt.Net.BCrypt.Verify(input.OldPassword, user.PasswordHash))
                return Results.BadRequest("Mevcut şifreniz yanlış.");

            if (input.NewPassword.Length < 6)
                return Results.BadRequest("Yeni şifre en az 6 karakter olmalı.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/auth/refresh", [Authorize] async (HttpContext ctx, BiodiversityContext db, IAuthService authService) =>
        {
            var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user == null) return Results.Unauthorized();

            var token = authService.GenerateJwtToken(user);
            return Results.Ok(new { success = true, token });
        });
    }
}
