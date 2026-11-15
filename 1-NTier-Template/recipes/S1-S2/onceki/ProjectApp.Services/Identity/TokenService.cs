using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ProjectApp.Repositories.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProjectApp.Services.Identity;

public class TokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    /// <summary>
    /// Access token'a gömülen security stamp claim'i; API katmanında OnTokenValidated ile doğrulanır.
    /// </summary>
    public const string SecurityStampClaimType = "security_stamp";

    public (string AccessToken, DateTime AccessTokenExpiry) CreateAccessToken(
        AppUser user,
        IEnumerable<string> roles)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var accessTokenExpiry = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(SecurityStampClaimType, user.SecurityStamp ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim("fullName", user.FullName));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessTokenExpiry,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return (accessToken, accessTokenExpiry);
    }
}
