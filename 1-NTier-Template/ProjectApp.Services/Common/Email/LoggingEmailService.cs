// S1 şablonunda tüketicisi yoktur; S1-S2 (şifre sıfırlama) ve S2-S3 (e-posta değişikliği) reçeteleri tarafından kullanılır.
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Services.Common.Email;

/// <summary>
/// Varsayılan e-posta servisi: gerçek gönderim yapmaz, loglar.
/// Canlı ortamda SMTP veya harici bir sağlayıcı implementasyonu ile değiştirilmelidir.
/// </summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger, IHostEnvironment environment) : IEmailService
{
    public Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        // İçerik (token barındırabilir) yalnızca geliştirme ortamında loglanır.
        if (environment.IsDevelopment())
        {
            logger.LogInformation("E-posta gönderildi -> Kime: {To} | Konu: {Subject} | İçerik: {Body}", MaskEmail(recipient), subject, body);
        }
        else
        {
            logger.LogInformation("E-posta gönderildi -> Kime: {To} | Konu: {Subject}", MaskEmail(recipient), subject);
        }

        return Task.CompletedTask;
    }

    // Kişisel veri: alıcı adresi loglarda maskelenir (ör. a***@example.com).
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" + email[Math.Max(at, 0)..] : email[0] + "***" + email[at..];
    }
}
