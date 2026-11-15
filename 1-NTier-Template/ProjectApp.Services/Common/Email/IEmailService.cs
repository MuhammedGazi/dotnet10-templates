namespace ProjectApp.Services.Common.Email;

public interface IEmailService
{
    Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
