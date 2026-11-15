using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectApp.Services.Features.Users;
using ProjectApp.Services.Features.Users.Dtos;

namespace ProjectApp.API.Controllers;

[Authorize]
public class UsersController(IUserService userService) : CustomBaseController
{
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.ChangePasswordAsync(request, cancellationToken));
}
