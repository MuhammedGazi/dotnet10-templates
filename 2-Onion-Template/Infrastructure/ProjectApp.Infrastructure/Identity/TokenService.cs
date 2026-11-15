using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ProjectApp.Application.Features.Auth.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProjectApp.Infrastructure.Identity;

public class TokenService(IOptions<JwtOption> jwtOptions, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOption _jwtOption = jwtOptions.Value;

    /// <summary>
    /// Access token'a gömülen security stamp claim'i; WebAPI'de OnTokenValidated ile doğrulanır.
    /// </summary>
    public const string SecurityStampClaimType = "security_stamp";

    /// <summary>
    /// Refresh token veritabanında düz metin saklanmaz; SHA-256 özeti saklanır (istemciye ham token döner).
    /// </summary>
    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public TokenDto CreateTokens(AppUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(SecurityStampClaimType, user.SecurityStamp ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var accessTokenExpiry = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_jwtOption.AccessTokenMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = accessTokenExpiry,
            Issuer = _jwtOption.Issuer,
            Audience = _jwtOption.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var refreshTokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(refreshTokenBytes);
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);
        var refreshTokenExpiry = timeProvider.GetUtcNow().UtcDateTime.AddDays(_jwtOption.RefreshTokenDays);

        return new TokenDto(accessToken, accessTokenExpiry, refreshToken, refreshTokenExpiry);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOption.Issuer,
            ValidAudience = _jwtOption.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.SigningKey)),
            ValidateLifetime = false // Süresi dolmuş token claimlerini okuyabilmek için
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
