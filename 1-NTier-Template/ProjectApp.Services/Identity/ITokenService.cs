using ProjectApp.Repositories.Identity;

namespace ProjectApp.Services.Identity;

public interface ITokenService
{
    (string AccessToken, DateTime AccessTokenExpiry) CreateAccessToken(
        AppUser user,
        IEnumerable<string> roles);
}
