using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Common.Email;
using ProjectApp.Services.Features.Users.Dtos;
using ProjectApp.Services.Identity;

namespace ProjectApp.Services.Features.Users;

public class UserService(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ICurrentUserService currentUserService,
    IEmailService emailService,
    ILogger<UserService> logger,
    TimeProvider timeProvider) : IUserService
{
    private const string AccountLockedMessage =
        "Çok fazla hatalı deneme yapıldı, hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.";

    private const string UserNotFoundMessage = "Kullanıcı bulunamadı.";

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

    public async Task<ServiceResult> ChangeEmailAsync(ChangeEmailRequest request, CancellationToken cancellationToken = default)
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

        if (string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.Fail("Yeni e-posta adresi mevcut e-posta adresinizle aynı olamaz.", HttpStatusCode.BadRequest);
        }

        var existingEmailUser = await userManager.FindByEmailAsync(request.NewEmail);
        if (existingEmailUser is not null && existingEmailUser.Id != user.Id)
        {
            return ServiceResult.Fail("Bu e-posta adresi başka bir kullanıcı tarafından kullanılmaktadır.", HttpStatusCode.BadRequest);
        }

        // E-posta hemen değiştirilmez; yeni adrese doğrulama anahtarı gönderilir (confirm-email-change ile tamamlanır).
        var changeEmailToken = await userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
        await emailService.SendEmailAsync(
            request.NewEmail,
            "E-posta Değişikliği Doğrulaması",
            $"E-posta adresinizi değiştirmek için aşağıdaki anahtarı (Token) kullanabilirsiniz: {changeEmailToken}",
            cancellationToken);
        logger.LogInformation("E-posta değişikliği için doğrulama e-postası oluşturuldu. Kullanıcı: {UserId}", user.Id);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request, CancellationToken cancellationToken = default)
    {
        var userResult = await GetAuthenticatedUserAsync();
        if (userResult.IsFail)
        {
            return ServiceResult.Fail(userResult.ErrorMessage!, userResult.Status);
        }

        var user = userResult.Data!;

        // Diğer hassas işlemlerle aynı kural: kilitli hesap işlem yapamaz, hatalı token lockout sayacını artırır.
        if (await userManager.IsLockedOutAsync(user))
        {
            return ServiceResult.Fail(AccountLockedMessage, HttpStatusCode.Forbidden);
        }

        var changeResult = await userManager.ChangeEmailAsync(user, request.NewEmail, request.Token);
        if (!changeResult.Succeeded)
        {
            await userManager.AccessFailedAsync(user);
            return ServiceResult.Fail("Doğrulama anahtarı geçersiz veya süresi dolmuş.", HttpStatusCode.BadRequest);
        }

        var sessionUpdate = await userManager.ClearSessionAsync(user, rotateSecurityStamp: true);
        if (!sessionUpdate.Succeeded)
        {
            return UserSessionExtensions.SessionUpdateFailed();
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteAccountAsync(DeleteAccountRequest request, CancellationToken cancellationToken = default)
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

        var isAdmin = await userManager.IsInRoleAsync(user, RoleNames.Admin);

        // Anonimleştirme ve oturum temizleme tek transaction'da: ara adım başarısız olursa hesap yarım silinmiş kalmaz.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Sistemde en az bir aktif Admin kalmalı; son aktif Admin kendi hesabını silemez (Admin olmayan kilit almaz).
        if (isAdmin && await LockAdminRoleAndCountActiveAdminsAsync(cancellationToken) <= 1)
        {
            return ServiceResult.Fail(
                "Son yöneticinin hesabı silinemez; önce başka bir kullanıcıya Admin rolü verin.",
                HttpStatusCode.BadRequest);
        }

        // Kişisel veriler anonimleştirilir (silinen hesabın e-posta/kullanıcı adı yeniden kullanılabilsin)
        var suffix = user.Id.ToString("N");
        var anonymizeUserName = await userManager.SetUserNameAsync(user, $"deleted_{suffix}");
        var anonymizeEmail = await userManager.SetEmailAsync(user, $"deleted_{suffix}@deleted.local");
        if (!anonymizeUserName.Succeeded || !anonymizeEmail.Succeeded)
        {
            var anonymizeErrors = anonymizeUserName.Errors.Concat(anonymizeEmail.Errors).Select(e => e.Description).ToList();
            return ServiceResult.Fail(anonymizeErrors, HttpStatusCode.BadRequest);
        }

        user.FullName = null;
        user.PhoneNumber = null;
        user.IsDeleted = true;
        user.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        // Refresh token silinir, security stamp yenilenir ve tüm alanlar tek yazımda kaydedilir.
        var updateResult = await userManager.ClearSessionAsync(user, rotateSecurityStamp: true);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description).ToList();
            return ServiceResult.Fail(errors, HttpStatusCode.BadRequest);
        }

        await transaction.CommitAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult<UserRolesDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FindActiveUserAsync(userId);
        if (user is null)
        {
            return ServiceResult<UserRolesDto>.Fail(UserNotFoundMessage, HttpStatusCode.NotFound);
        }

        var roles = await userManager.GetRolesAsync(user);
        return ServiceResult<UserRolesDto>.Success(new UserRolesDto(user.Id, roles.Order(StringComparer.Ordinal).ToList()));
    }

    public async Task<ServiceResult> UpdateUserRolesAsync(Guid userId, UpdateUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindActiveUserAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail(UserNotFoundMessage, HttpStatusCode.NotFound);
        }

        var requestedNames = (request.Roles ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requestedNames.Count == 0)
        {
            return ServiceResult.Fail("En az bir rol belirtilmelidir.", HttpStatusCode.BadRequest);
        }

        // İstenen roller veritabanındaki adlarına çözülür (büyük/küçük harf farkı önemsiz); olmayan rol atanmaz.
        var requestedRoles = new List<string>();
        var missingRoles = new List<string>();
        foreach (var roleName in requestedNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role?.Name is null)
            {
                missingRoles.Add(roleName);
            }
            else
            {
                requestedRoles.Add(role.Name);
            }
        }

        if (missingRoles.Count > 0)
        {
            return ServiceResult.Fail($"Şu roller bulunamadı: {string.Join(", ", missingRoles)}.", HttpStatusCode.BadRequest);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
        if (rolesToRemove.Count == 0 && rolesToAdd.Count == 0)
        {
            return ServiceResult.Success(HttpStatusCode.NoContent);
        }

        // Kaldırma, ekleme ve oturum iptali tek transaction'da: biri başarısız olursa kullanıcı yarım rol setiyle kalmaz.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Sistemde Admin kalmazsa yönetim uçlarına kimse erişemez; son aktif Admin'in rolü kaldırılamaz (kendisi dahil).
        if (rolesToRemove.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase))
        {
            var activeAdminCount = await LockAdminRoleAndCountActiveAdminsAsync(cancellationToken);
            if (activeAdminCount <= 1)
            {
                return ServiceResult.Fail(
                    "Son yöneticinin Admin rolü kaldırılamaz; önce başka bir kullanıcıya Admin rolü verin.",
                    HttpStatusCode.BadRequest);
            }
        }

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return RoleUpdateFailed(removeResult);
            }
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return RoleUpdateFailed(addResult);
            }
        }

        // Eski access token'lar eski rolleri taşır: security stamp yenilenir (401) ve refresh token iptal edilir.
        var sessionUpdate = await userManager.ClearSessionAsync(user, rotateSecurityStamp: true);
        if (!sessionUpdate.Succeeded)
        {
            return UserSessionExtensions.SessionUpdateFailed();
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Kullanıcı rolleri güncellendi. Hedef: {TargetUserId}, İşlemi yapan: {AdminUserId}, Eklenen: {AddedRoles}, Kaldırılan: {RemovedRoles}",
            user.Id, currentUserService.UserId, rolesToAdd, rolesToRemove);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Açık bir transaction içinde çağrılır. Eşzamanlı son-admin yarışı: iki Admin aynı anda birbirinin rolünü kaldırırsa
    /// (veya hesabını silerse) ikisi de 2 sayıp 0 Admin bırakır. Admin rol satırına UPDATE (yeni ConcurrencyStamp; SQL Server değişmeyen UPDATE kilidini bırakabilir) satırı
    /// commit'e kadar kilitler; ikinci istek bekler ve güncel aktif Admin sayısını görür.
    /// </summary>
    private async Task<int> LockAdminRoleAndCountActiveAdminsAsync(CancellationToken cancellationToken)
    {
        var adminRoleName = roleManager.NormalizeKey(RoleNames.Admin);
        var newStamp = Guid.NewGuid().ToString();
        await context.Roles
            .Where(r => r.NormalizedName == adminRoleName)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ConcurrencyStamp, newStamp), cancellationToken);

        return (await userManager.GetUsersInRoleAsync(RoleNames.Admin)).Count(u => !u.IsDeleted);
    }

    /// <summary>Silinmiş (soft delete) kullanıcılar yönetim uçlarında da yok sayılır.</summary>
    private async Task<AppUser?> FindActiveUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null || user.IsDeleted ? null : user;
    }

    /// <summary>Eşzamanlı güncelleme (ConcurrencyStamp) 409, diğer Identity hataları 400 döner.</summary>
    private static ServiceResult RoleUpdateFailed(IdentityResult result)
    {
        var errors = result.Errors.Select(e => e.Description).ToList();
        var status = result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure))
            ? HttpStatusCode.Conflict
            : HttpStatusCode.BadRequest;
        return ServiceResult.Fail(errors, status);
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
        if (user is null || user.IsDeleted)
        {
            return ServiceResult<AppUser>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);
        }

        return ServiceResult<AppUser>.Success(user);
    }
}
