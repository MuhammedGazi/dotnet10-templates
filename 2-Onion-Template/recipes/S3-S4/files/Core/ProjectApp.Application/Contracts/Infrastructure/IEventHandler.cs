using ProjectApp.Domain.Events;

namespace ProjectApp.Application.Contracts.Infrastructure;

public interface IEventHandler<in TEvent> where TEvent : IEventOrMessage
{
    Task HandleAsync(TEvent eventData, CancellationToken cancellationToken = default);
}
