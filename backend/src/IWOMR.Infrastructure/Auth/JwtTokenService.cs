using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using IWOMR.Application.Common.Interfaces;
using IWOMR.Domain.Entities;

namespace IWOMR.Infrastructure.Auth;

public class JwtTokenService(IConfiguration config) : ITokenService
{
    private readonly string _secret = config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured.");
    private readonly int _expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

    public string GenerateAccessToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username",                    user.Username),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             "iwomr",
            audience:           "iwomr-mobile",
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public Guid? ValidateRefreshToken(string token)
    {
        // Phase 2: store refresh tokens in Redis with TTL
        return null;
    }
}
