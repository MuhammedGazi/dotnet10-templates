using ProjectApp.Domain.Events;

namespace ProjectApp.Application.Contracts.Infrastructure;

public interface IServiceBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IEventOrMessage;
}
