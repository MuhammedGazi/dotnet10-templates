using ProjectApp.Application.Common.Wrappers;
using ProjectApp.Application.Features.Auth.Dtos;
using ProjectApp.Application.Features.Auth.Login;
using ProjectApp.Application.Features.Auth.Register;

namespace ProjectApp.Application.Contracts.Identity;

public interface IAuthService
{
    Task<ServiceResult<TokenDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<TokenDto>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> LogoutAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
