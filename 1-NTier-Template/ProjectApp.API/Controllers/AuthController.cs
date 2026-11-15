using ProjectApp.Services.Features.Auth;
using ProjectApp.Services.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.API.Controllers;

public class AuthController(IAuthService authService) : CustomBaseController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.LoginAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken) =>
        CreateActionResult(await authService.GetCurrentUserAsync(cancellationToken));
}
