using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectApp.Repositories.Identity;
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

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.ChangeEmailAsync(request, cancellationToken));

    [HttpPost("confirm-email-change")]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.ConfirmEmailChangeAsync(request, cancellationToken));

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.DeleteAccountAsync(request, cancellationToken));

    // Yönetici uçları: hedef kullanıcı id'si yalnızca Admin rolüyle kabul edilir (self-servis uçlarında id alınmaz).
    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet("{id:guid}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid id, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.GetUserRolesAsync(id, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> UpdateUserRoles(Guid id, [FromBody] UpdateUserRolesRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await userService.UpdateUserRolesAsync(id, request, cancellationToken));
}
