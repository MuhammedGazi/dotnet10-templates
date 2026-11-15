using System.Security.Claims;
using ProjectApp.Application.Features.Auth.Dtos;

namespace ProjectApp.Infrastructure.Identity;

public interface ITokenService
{
    TokenDto CreateTokens(AppUser user, IList<string> roles);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
