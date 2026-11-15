using System.Net;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Features.Auth.Dtos;
using ProjectApp.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Services.Features.Auth;

public class AuthService(
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    ICurrentUserService currentUserService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<ServiceResult<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // E-posta loglanmaz (kişisel veri); yalnızca olay kaydedilir.
            logger.LogWarning("Bilinmeyen hesapla başarısız giriş denemesi.");
            SimulatePasswordCheck(request.Password);
            return ServiceResult<TokenResponse>.Fail("E-posta veya şifre hatalı.", HttpStatusCode.Unauthorized);
        }

        // Hesap kilitli mi kontrolü (Brute-force koruması).
        // Kilit bilgisi istemciye sızdırılmaz (tek tip yanıt); yalnızca loglanır ve sayaç artırılmaz.
        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Kilitli hesap için giriş denemesi yapıldı. Kullanıcı: {UserId}", user.Id);
            SimulatePasswordCheck(request.Password);
            return ServiceResult<TokenResponse>.Fail("E-posta veya şifre hatalı.", HttpStatusCode.Unauthorized);
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            // Hatalı giriş sayacını artır (Eşik aşılırsa otomatik kilitler)
            await userManager.AccessFailedAsync(user);
            logger.LogWarning("Hatalı şifreyle başarısız giriş. Kullanıcı: {UserId}", user.Id);
            return ServiceResult<TokenResponse>.Fail("E-posta veya şifre hatalı.", HttpStatusCode.Unauthorized);
        }

        // Başarılı girişte sayacı sıfırla
        await userManager.ResetAccessFailedCountAsync(user);
        logger.LogInformation("Başarılı giriş. Kullanıcı: {UserId}", user.Id);

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessTokenExpiry) = tokenService.CreateAccessToken(user, roles);

        var response = new TokenResponse(
            accessToken,
            accessTokenExpiry);

        return ServiceResult<TokenResponse>.Success(response);
    }

    public async Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            return ServiceResult<CurrentUserResponse>.Fail("Oturum açmış kullanıcı bulunamadı.", HttpStatusCode.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(currentUserService.UserId.Value.ToString());
        if (user is null)
        {
            return ServiceResult<CurrentUserResponse>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);
        }

        var roles = await userManager.GetRolesAsync(user);
        var response = new CurrentUserResponse(
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            roles.ToList());

        return ServiceResult<CurrentUserResponse>.Success(response);
    }

    // Var olmayan / kilitli hesaplarda da şifre hash doğrulaması kadar süre harcanır;
    // böylece yanıt süresinden hesabın varlığı veya kilit durumu anlaşılamaz.
    private static string? _dummyPasswordHash;
    private static readonly Lock DummyHashLock = new();

    private void SimulatePasswordCheck(string password)
    {
        var dummyUser = new AppUser();
        var dummyHash = Volatile.Read(ref _dummyPasswordHash);
        if (dummyHash is null)
        {
            // İlk eşzamanlı isteklerde PBKDF2 hesaplamasının birden fazla kez yapılmaması için kilitli ilklendirme.
            lock (DummyHashLock)
            {
                dummyHash = _dummyPasswordHash ??= userManager.PasswordHasher.HashPassword(dummyUser, "Timing-Equalizer-Password-1!");
            }
        }

        userManager.PasswordHasher.VerifyHashedPassword(dummyUser, dummyHash, password);
    }
}
