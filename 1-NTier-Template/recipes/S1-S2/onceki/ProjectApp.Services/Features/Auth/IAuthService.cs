using ProjectApp.Services.Features.Auth.Dtos;

namespace ProjectApp.Services.Features.Auth;

public interface IAuthService
{
    Task<ServiceResult<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
