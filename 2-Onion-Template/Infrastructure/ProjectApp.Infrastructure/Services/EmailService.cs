using ProjectApp.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger, IHostEnvironment environment) : IEmailService
{
    public Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Varsayılan: gerçek gönderim yok, loglanır. Canlıda SMTP veya harici sağlayıcı bağlanmalıdır.
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
