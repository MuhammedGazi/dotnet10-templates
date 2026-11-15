namespace ProjectApp.Domain.Events;

/// <summary>
/// [Mimari iskelet] Kuyruğa (IServiceBus) yazılabilen her şeyin ortak işaretçisi.
/// <see cref="IEvent"/>: olmuş bir şeyi bildirir (ör. ProductAddedEvent), birden çok dinleyicisi olabilir.
/// <see cref="IMessage"/>: bir işin yapılmasını ister (komut, ör. SendWelcomeEmailMessage), tek işleyicisi olur.
/// </summary>
public interface IEventOrMessage;
