using System.Net;
using Microsoft.AspNetCore.Identity;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Features.Users.Dtos;
using ProjectApp.Services.Identity;

namespace ProjectApp.Services.Features.Users;

public class UserService(
    UserManager<AppUser> userManager,
    ICurrentUserService currentUserService) : IUserService
{
    private const string AccountLockedMessage =
        "Çok fazla hatalı deneme yapıldı, hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.";

    public async Task<ServiceResult<UserProfileDto>> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
    {
        var userResult = await GetAuthenticatedUserAsync();
        if (userResult.IsFail)
        {
            return ServiceResult<UserProfileDto>.Fail(userResult.ErrorMessage!, userResult.Status);
        }

        var user = userResult.Data!;
        var roles = await userManager.GetRolesAsync(user);

        var profileDto = new UserProfileDto(
            user.Id,
            user.FullName ?? string.Empty,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            roles.ToList());

        return ServiceResult<UserProfileDto>.Success(profileDto);
    }

    public async Task<ServiceResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userResult = await GetAuthenticatedUserAsync();
        if (userResult.IsFail)
        {
            return ServiceResult.Fail(userResult.ErrorMessage!, userResult.Status);
        }

        var user = userResult.Data!;

        if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await userManager.FindByNameAsync(request.UserName);
            if (existingUser is not null && existingUser.Id != user.Id)
            {
                return ServiceResult.Fail("Bu kullanıcı adı başka bir kullanıcı tarafından kullanılmaktadır.", HttpStatusCode.BadRequest);
            }

            var setUserNameResult = await userManager.SetUserNameAsync(user, request.UserName);
            if (!setUserNameResult.Succeeded)
            {
                var errors = setUserNameResult.Errors.Select(e => e.Description).ToList();
                return ServiceResult.Fail(errors, HttpStatusCode.BadRequest);
            }
        }

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description).ToList();
            return ServiceResult.Fail(errors, HttpStatusCode.BadRequest);
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userResult = await GetAuthenticatedUserAsync();
        if (userResult.IsFail)
        {
            return ServiceResult.Fail(userResult.ErrorMessage!, userResult.Status);
        }

        var user = userResult.Data!;

        var passwordCheck = await VerifyCurrentPasswordAsync(user, request.CurrentPassword);
        if (passwordCheck is not null)
        {
            return passwordCheck;
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ServiceResult.Fail(errors, HttpStatusCode.BadRequest);
        }

        var sessionUpdate = await userManager.ClearSessionAsync(user, rotateSecurityStamp: true);
        if (!sessionUpdate.Succeeded)
        {
            return UserSessionExtensions.SessionUpdateFailed();
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Hassas işlemlerde mevcut şifreyi doğrular. Hatalı denemeler giriş ekranındaki gibi
    /// lockout sayacını artırır; hesap kilitliyse işlem yapılmaz.
    /// </summary>
    private async Task<ServiceResult?> VerifyCurrentPasswordAsync(AppUser user, string currentPassword)
    {
        if (await userManager.IsLockedOutAsync(user))
        {
            return ServiceResult.Fail(AccountLockedMessage, HttpStatusCode.Forbidden);
        }

        if (!await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            return ServiceResult.Fail("Mevcut şifreniz hatalı.", HttpStatusCode.BadRequest);
        }

        if (user.AccessFailedCount > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        return null;
    }

    private async Task<ServiceResult<AppUser>> GetAuthenticatedUserAsync()
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            return ServiceResult<AppUser>.Fail("Oturum açmış kullanıcı bulunamadı.", HttpStatusCode.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(currentUserService.UserId.Value.ToString());
        if (user is null)
        {
            return ServiceResult<AppUser>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);
        }

        return ServiceResult<AppUser>.Success(user);
    }
}
