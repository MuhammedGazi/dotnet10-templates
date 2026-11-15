using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectApp.Services.Features.Users;
using ProjectApp.Services.Features.Users.Dtos;

namespace ProjectApp.API.Controllers;

[Authorize]
public class UsersController(IUserService userService) : CustomBaseController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserProfile(CancellationToken cancellationToken) =>
        CreateActionResult(await userService.GetCurrentUserProfileAsync(cancellationToken));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.UpdateProfileAsync(request, cancellationToken));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.ChangePasswordAsync(request, cancellationToken));
}
