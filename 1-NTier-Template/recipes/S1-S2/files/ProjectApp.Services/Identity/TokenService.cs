using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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

    /// <summary>
    /// Refresh token veritabanında düz metin saklanmaz; SHA-256 özeti saklanır (istemciye ham token döner).
    /// </summary>
    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public (string AccessToken, DateTime AccessTokenExpiry, string RefreshToken, DateTime RefreshTokenExpiry) CreateTokens(
        AppUser user,
        IEnumerable<string> roles)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var accessTokenExpiry = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiry = now.AddDays(_options.RefreshTokenDays);

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

        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        return (accessToken, accessTokenExpiry, refreshToken, refreshTokenExpiry);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            ValidateLifetime = false // Token süresi geçmiş olsa bile claim'leri okuyabilmek için
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            // Bozuk / başka anahtarla imzalanmış token: 500 yerine servis 401 döner.
            return null;
        }
    }
}
