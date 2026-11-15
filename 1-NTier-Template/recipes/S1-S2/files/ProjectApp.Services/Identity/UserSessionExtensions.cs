using System.Net;
using Microsoft.AspNetCore.Identity;
using ProjectApp.Repositories.Identity;

namespace ProjectApp.Services.Identity;

/// <summary>
/// Oturum (refresh token + security stamp) güncellemelerinin tek kaynağı.
/// AuthService ve UserService aynı kuralı tekrar yazmak yerine bu yardımcıları kullanır.
/// </summary>
internal static class UserSessionExtensions
{
    internal const string SessionUpdateFailedMessage = "Oturum güncellenemedi, lütfen tekrar deneyin.";

    /// <summary>Yeni refresh token'ın özetini ve bitiş zamanını kullanıcıya yazar (login / refresh rotasyonu).</summary>
    internal static Task<IdentityResult> StoreRefreshTokenAsync(
        this UserManager<AppUser> userManager,
        AppUser user,
        string refreshToken,
        DateTime refreshTokenExpiry)
    {
        user.RefreshToken = TokenService.HashToken(refreshToken);
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        return userManager.UpdateAsync(user);
    }

    /// <summary>
    /// Refresh token'ı siler. <paramref name="rotateSecurityStamp"/> true ise security stamp yenilenir ve
    /// mevcut access token'lar da (OnTokenValidated kontrolüyle) anında geçersiz olur. Tek veritabanı yazımı yapılır.
    /// </summary>
    internal static Task<IdentityResult> ClearSessionAsync(
        this UserManager<AppUser> userManager,
        AppUser user,
        bool rotateSecurityStamp)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        return rotateSecurityStamp
            ? userManager.UpdateSecurityStampAsync(user) // kullanıcıyı (refresh token alanlarıyla birlikte) kaydeder
            : userManager.UpdateAsync(user);
    }

    internal static ServiceResult SessionUpdateFailed() =>
        ServiceResult.Fail(SessionUpdateFailedMessage, HttpStatusCode.Conflict);
}
