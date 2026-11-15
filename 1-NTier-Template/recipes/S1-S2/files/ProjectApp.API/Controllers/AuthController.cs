using ProjectApp.API.Extensions;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Features.Auth;
using ProjectApp.Services.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ProjectApp.API.Controllers;

// Sıkı auth politikası yalnızca kimlik bilgisi/token alan uçlara uygulanır; /me ve logout global limittedir.
// Kayıt (register): Registration:AllowPublic=false (varsayılan) ise yalnızca Admin kullanıcı ekler;
// true ise herkes kayıt olur ve her zaman User rolü alır. Açık kayıtta e-posta doğrulaması ekleyin (login EmailConfirmed'a bakmaz).
public class AuthController(IAuthService authService) : CustomBaseController
{
    [HttpPost("login")]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicyName)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.LoginAsync(request, cancellationToken));

    [Authorize(Policy = AuthenticationExtensions.RegisterPolicyName)]
    [HttpPost("register")]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicyName)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.RegisterAsync(request, cancellationToken));

    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicyName)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.RefreshTokenAsync(request, cancellationToken));

    [HttpPost("revoke-token")]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicyName)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeRefreshTokenRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.RevokeRefreshTokenAsync(request, cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) =>
        CreateActionResult(await authService.LogoutAsync(cancellationToken));

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicyName)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.ForgotPasswordAsync(request, cancellationToken));

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicyName)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await authService.ResetPasswordAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken) =>
        CreateActionResult(await authService.GetCurrentUserAsync(cancellationToken));
}
