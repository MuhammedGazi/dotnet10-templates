using ProjectApp.Services.Features.Auth.Dtos;

namespace ProjectApp.Services.Features.Auth;

public interface IAuthService
{
    Task<ServiceResult<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> LogoutAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
