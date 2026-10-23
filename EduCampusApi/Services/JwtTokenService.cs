using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using module1_4.Models;

namespace module1_4.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(AppUser user, IList<string> roles, int expireMinutes = 60);
    string GenerateExpiredToken(AppUser user, IList<string> roles);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(AppUser user, IList<string> roles, int expireMinutes = 60)
    {
        var expires = DateTime.UtcNow.AddMinutes(expireMinutes);
        var token = CreateToken(user, roles, expires, DateTime.UtcNow);
        return (token, expires);
    }

    public string GenerateExpiredToken(AppUser user, IList<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(-10);
        var notBefore = DateTime.UtcNow.AddMinutes(-30);
        return CreateToken(user, roles, expires, notBefore);
    }

    private string CreateToken(AppUser user, IList<string> roles, DateTime expires, DateTime notBefore)
    {
        var jwtSecret = _configuration["Jwt:Key"] ?? "CampusUniversitySecureKeyForJwtTokens2026SuperSecretString!";
        var issuer = _configuration["Jwt:Issuer"] ?? "EduCampusApi";
        var audience = _configuration["Jwt:Audience"] ?? "EduCampusClients";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("fullName", user.FullName)
        };

        if (user.StudentId.HasValue)
        {
            claims.Add(new Claim("studentId", user.StudentId.Value.ToString()));
        }

        if (user.TeacherId.HasValue)
        {
            claims.Add(new Claim("teacherId", user.TeacherId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = notBefore,
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var secToken = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(secToken);
    }
}