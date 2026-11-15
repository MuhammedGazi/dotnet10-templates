using ProjectApp.Application.Contracts.Infrastructure;
using ProjectApp.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Infrastructure.Messaging;

/// <summary>
/// Kalıcı değildir; uygulama kapanınca kuyruktaki mesajlar kaybolur.
/// Garanti gereken işler için outbox kullanın.
/// Best-effort çalışır: kuyruk doluysa mesaj düşürülür ve uyarı loglanır; çağıran (HTTP isteği) asla bekletilmez.
/// </summary>
public class InMemoryServiceBus(InMemoryMessageQueue queue, ILogger<InMemoryServiceBus> logger) : IServiceBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IEventOrMessage
    {
        if (!queue.Writer.TryWrite(message))
        {
            logger.LogWarning("In-memory kuyruk dolu, mesaj düşürüldü: {MessageType}", typeof(T).Name);
        }

        return Task.CompletedTask;
    }
}
