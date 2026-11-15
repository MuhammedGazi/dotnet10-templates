using ProjectApp.Application.Common.Wrappers;
using ProjectApp.Application.Features.Users.Dtos;

namespace ProjectApp.Application.Contracts.Identity;

public interface IUserService
{
    Task<ServiceResult<UserProfileDto>> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangeEmailAsync(ChangeEmailRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAccountAsync(DeleteAccountRequest request, CancellationToken cancellationToken = default);

    // Yönetici (Admin) uçları: hedef kullanıcı route'taki id ile seçilir; yetki controller'da rol ile denetlenir.
    Task<ServiceResult<UserRolesDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateUserRolesAsync(Guid userId, UpdateUserRolesRequest request, CancellationToken cancellationToken = default);
}
