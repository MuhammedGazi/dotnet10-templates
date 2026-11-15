using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Common.Email;
using ProjectApp.Services.Features.Auth.Dtos;
using ProjectApp.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Services.Features.Auth;

public class AuthService(
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    ICurrentUserService currentUserService,
    IEmailService emailService,
    ILogger<AuthService> logger,
    TimeProvider timeProvider) : IAuthService
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
        var (accessToken, accessTokenExpiry, refreshToken, refreshTokenExpiry) =
            tokenService.CreateTokens(user, roles);

        var sessionUpdate = await userManager.StoreRefreshTokenAsync(user, refreshToken, refreshTokenExpiry);
        if (!sessionUpdate.Succeeded)
        {
            return ServiceResult<TokenResponse>.Fail(UserSessionExtensions.SessionUpdateFailedMessage, HttpStatusCode.Conflict);
        }

        var response = new TokenResponse(
            accessToken,
            accessTokenExpiry,
            refreshToken,
            refreshTokenExpiry);

        return ServiceResult<TokenResponse>.Success(response);
    }

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingEmail is not null)
        {
            return ServiceResult<RegisterResponse>.Fail("Bu e-posta adresi zaten kayıtlıdır.");
        }

        var existingUserName = await userManager.FindByNameAsync(request.UserName);
        if (existingUserName is not null)
        {
            return ServiceResult<RegisterResponse>.Fail("Bu kullanıcı adı zaten kullanılmaktadır.");
        }

        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            FullName = request.FullName,
            UserName = request.UserName,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(x => x.Description).ToList();
            return ServiceResult<RegisterResponse>.Fail(errors);
        }

        // Rol yoksa Identity hata sonucu yerine exception fırlatır; her iki durumda da
        // rolsüz kullanıcı bırakılmaz, kayıt geri alınır.
        IdentityResult roleResult;
        try
        {
            roleResult = await userManager.AddToRoleAsync(user, RoleNames.User);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Kullanıcıya varsayılan rol atanamadı (rol tanımlı değil).");
            roleResult = IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }

        if (!roleResult.Succeeded)
        {
            logger.LogError("Kullanıcıya varsayılan rol atanamadı: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            await userManager.DeleteAsync(user);
            return ServiceResult<RegisterResponse>.Fail("Kayıt tamamlanamadı, lütfen tekrar deneyin.", HttpStatusCode.InternalServerError);
        }

        return ServiceResult<RegisterResponse>.SuccessAsCreated(new RegisterResponse(user.Id), "/api/auth/me");
    }

    public async Task<ServiceResult<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            return ServiceResult<TokenResponse>.Fail("Geçersiz access token.", HttpStatusCode.Unauthorized);
        }

        var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return ServiceResult<TokenResponse>.Fail("Geçersiz kullanıcı kimliği.", HttpStatusCode.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.RefreshToken ?? string.Empty),
                Encoding.UTF8.GetBytes(TokenService.HashToken(request.RefreshToken))) ||
            user.RefreshTokenExpiryTime <= timeProvider.GetUtcNow().UtcDateTime)
        {
            return ServiceResult<TokenResponse>.Fail("Geçersiz veya süresi dolmuş refresh token.", HttpStatusCode.Unauthorized);
        }

        // Token rotasyonu
        var roles = await userManager.GetRolesAsync(user);
        var (newAccessToken, newAccessTokenExpiry, newRefreshToken, newRefreshTokenExpiry) =
            tokenService.CreateTokens(user, roles);

        var sessionUpdate = await userManager.StoreRefreshTokenAsync(user, newRefreshToken, newRefreshTokenExpiry);
        if (!sessionUpdate.Succeeded)
        {
            return ServiceResult<TokenResponse>.Fail(UserSessionExtensions.SessionUpdateFailedMessage, HttpStatusCode.Conflict);
        }

        var response = new TokenResponse(
            newAccessToken,
            newAccessTokenExpiry,
            newRefreshToken,
            newRefreshTokenExpiry);

        return ServiceResult<TokenResponse>.Success(response);
    }

    public async Task<ServiceResult> RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ServiceResult.Fail("Refresh token belirtilmelidir.", HttpStatusCode.BadRequest);
        }

        var hashedRefreshToken = TokenService.HashToken(request.RefreshToken);
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken, cancellationToken);

        if (user is null)
        {
            return ServiceResult.Fail("Belirtilen refresh token bulunamadı.", HttpStatusCode.NotFound);
        }

        var sessionUpdate = await userManager.ClearSessionAsync(user, rotateSecurityStamp: false);
        if (!sessionUpdate.Succeeded)
        {
            return UserSessionExtensions.SessionUpdateFailed();
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            return ServiceResult.Fail("Oturum açmış kullanıcı bulunamadı.", HttpStatusCode.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(currentUserService.UserId.Value.ToString());
        if (user is null)
        {
            return ServiceResult.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);
        }

        var sessionUpdate = await userManager.ClearSessionAsync(user, rotateSecurityStamp: true);
        if (!sessionUpdate.Succeeded)
        {
            return UserSessionExtensions.SessionUpdateFailed();
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            logger.LogInformation(
                "Şifre sıfırlama talebi oluşturuldu. Kullanıcı: {UserId}",
                user.Id);

            await emailService.SendEmailAsync(
                user.Email!,
                "Şifre Sıfırlama Talebi",
                $"Şifrenizi sıfırlamak için aşağıdaki anahtarı (Token) kullanabilirsiniz: {resetToken}",
                cancellationToken);
        }

        // Enumeration saldırılarını önlemek için her durumda başarılı döner
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return ServiceResult.Fail("Şifre sıfırlama işlemi başarısız oldu. Bilgilerinizi kontrol ediniz.", HttpStatusCode.BadRequest);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(x => x.Description).ToList();
            return ServiceResult.Fail(errors, HttpStatusCode.BadRequest);
        }

        var sessionUpdate = await userManager.ClearSessionAsync(user, rotateSecurityStamp: true);
        if (!sessionUpdate.Succeeded)
        {
            return UserSessionExtensions.SessionUpdateFailed();
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
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
