// S1 şablonunda tüketicisi yoktur; S1-S2 (şifre sıfırlama) ve S2-S3 (e-posta değişikliği) reçeteleri tarafından kullanılır.
namespace ProjectApp.Services.Common.Email;

public interface IEmailService
{
    Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
