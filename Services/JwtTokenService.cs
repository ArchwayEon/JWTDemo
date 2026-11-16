namespace JWTDemo.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JWTDemo.Models.Entities;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Builds and signs a JWT (header.payload.signature) for a user.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _config.GetSection("Jwt");

        // The SAME secret key is used to sign here and to verify in Program.cs.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims = statements about the user that go in the payload.
        // Remember: the payload is only Base64URL-encoded, NOT encrypted.
        // Never put passwords or other secrets in here!
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.DisplayName));
        }

        // Add role claims using the short JWT name "role".
        // (If you use ClaimTypes.Role here instead, the token will literally contain
        // "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" as the claim
        // name, and RoleClaimType in Program.cs would have to match that.)
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(jwtSettings["ExpiresInMinutes"]!)),
            signingCredentials: creds
        );

        // Serialize to the compact xxxxx.yyyyy.zzzzz string.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
