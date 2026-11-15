using ProjectApp.Services.Features.Users.Dtos;

namespace ProjectApp.Services.Features.Users;

public interface IUserService
{
    Task<ServiceResult<UserProfileDto>> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
