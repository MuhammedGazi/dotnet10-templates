namespace ProjectApp.Domain.Events;

public sealed record ProductAddedEvent(Guid Id, string Name, decimal Price, Guid CategoryId) : IEvent;
