using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using iNaturalist_Lite.Models;
using Microsoft.IdentityModel.Tokens;

namespace iNaturalist_Lite.Services;

public interface IAuthService
{
    string GenerateJwtToken(AppUser user);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateJwtToken(AppUser user)
    {
        var keyStr = _config["Jwt:Key"] ?? "super_secret_naturalist_key_minimum_32_chars_123456789";
        var issuer = _config["Jwt:Issuer"] ?? "iNaturalistLite";
        var audience = _config["Jwt:Audience"] ?? "iNaturalistLite";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(keyStr);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("badge", user.Badge ?? "🌱"),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddDays(30),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
