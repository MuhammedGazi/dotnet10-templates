using System.Security.Claims;
using ProjectApp.Repositories.Identity;

namespace ProjectApp.Services.Identity;

public interface ITokenService
{
    (string AccessToken, DateTime AccessTokenExpiry, string RefreshToken, DateTime RefreshTokenExpiry) CreateTokens(
        AppUser user,
        IEnumerable<string> roles);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
