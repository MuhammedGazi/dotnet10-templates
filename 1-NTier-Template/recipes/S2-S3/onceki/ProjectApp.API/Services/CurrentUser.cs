using System.Security.Claims;
using ProjectApp.Repositories;
using Microsoft.AspNetCore.Http;

namespace ProjectApp.API.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    // Her erişimde güncel HttpContext.User okunur. Kurucuda sabitlenirse, servis UseAuthentication()
    // öncesinde çözüldüğünde (ör. erken bir middleware) anonim principal'da takılı kalır ve
    // kullanıcı kimliği sessizce null döner.
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedGuid) ? parsedGuid : null;

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
