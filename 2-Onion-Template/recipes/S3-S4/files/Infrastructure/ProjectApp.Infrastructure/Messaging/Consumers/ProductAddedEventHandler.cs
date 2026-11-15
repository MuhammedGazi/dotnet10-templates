using ProjectApp.Application.Contracts.Infrastructure;
using ProjectApp.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Infrastructure.Messaging.Consumers;

public class ProductAddedEventHandler(
    IEmailService emailService,
    ILogger<ProductAddedEventHandler> logger) : IEventHandler<ProductAddedEvent>
{
    public async Task HandleAsync(ProductAddedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Yeni ürün eklendi olayı işleniyor: {Id} - {Name} (Fiyat: {Price})", eventData.Id, eventData.Name, eventData.Price);
        await emailService.SendEmailAsync(
            "admin@admin.com",
            "Yeni Ürün Eklendi",
            $"Yeni ürün başarıyla eklendi: {eventData.Name} (Fiyat: {eventData.Price:C})",
            cancellationToken);
    }
}
