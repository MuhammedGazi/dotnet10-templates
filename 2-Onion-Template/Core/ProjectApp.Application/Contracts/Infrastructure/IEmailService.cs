namespace ProjectApp.Application.Contracts.Infrastructure;

public interface IEmailService
{
    Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
