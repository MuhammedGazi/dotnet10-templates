using Microsoft.AspNetCore.Identity;

namespace ProjectApp.Repositories.Identity;

/// <summary>
/// ASP.NET Core Identity hata mesajlarının Türkçe karşılıkları (API yanıtlarında İngilizce mesaj sızmaması için).
/// </summary>
public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Beklenmeyen bir hata oluştu." };
    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "Kayıt başka bir işlem tarafından değiştirildi, lütfen tekrar deneyin." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Mevcut şifreniz hatalı." };
    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "Doğrulama anahtarı geçersiz veya süresi dolmuş." };
    public override IdentityError LoginAlreadyAssociated() => new() { Code = nameof(LoginAlreadyAssociated), Description = "Bu harici giriş başka bir hesaba bağlı." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"Kullanıcı adı '{userName}' geçersiz; yalnızca harf ve rakam içerebilir." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"E-posta adresi '{email}' geçersiz." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"Kullanıcı adı '{userName}' zaten kullanılıyor." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"E-posta adresi '{email}' zaten kayıtlı." };
    public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = $"Rol adı '{role}' geçersiz." };
    public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = $"Rol adı '{role}' zaten mevcut." };
    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "Kullanıcının zaten bir şifresi var." };
    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "Bu kullanıcı için hesap kilitleme etkin değil." };
    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = $"Kullanıcı zaten '{role}' rolünde." };
    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = $"Kullanıcı '{role}' rolünde değil." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalıdır." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifre en az {uniqueChars} farklı karakter içermelidir." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir özel karakter içermelidir." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam içermelidir." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf içermelidir." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf içermelidir." };
    public override IdentityError RecoveryCodeRedemptionFailed() => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Kurtarma kodu kullanılamadı." };
}
