using ProjectApp.Services.Features.Users.Dtos;

namespace ProjectApp.Services.Features.Users;

public interface IUserService
{
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
