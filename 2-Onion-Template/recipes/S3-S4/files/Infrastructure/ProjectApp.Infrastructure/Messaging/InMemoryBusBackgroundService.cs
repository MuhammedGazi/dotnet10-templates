using System.Collections.Concurrent;
using System.Reflection;
using ProjectApp.Application.Contracts.Infrastructure;
using ProjectApp.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectApp.Infrastructure.Messaging;

/// <summary>
/// Kuyruktaki mesajları sırayla tüketir. Kalıcı değildir; kapanışta kuyruktaki mesajlar kaybolur ve hata alan
/// mesaj yeniden denenmez. Garanti gereken işler için outbox kullanın.
/// </summary>
public class InMemoryBusBackgroundService(
    InMemoryMessageQueue queue,
    IServiceProvider serviceProvider,
    ILogger<InMemoryBusBackgroundService> logger) : BackgroundService
{
    // Mesaj tipi başına bir kez üretilen çağırıcı: her mesajda MakeGenericType/GetMethod/Invoke yapılmaz ve
    // handler istisnaları TargetInvocationException ile sarılmaz.
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, Func<object, IEventOrMessage, CancellationToken, Task> Invoke)> Dispatchers = new();

    private static readonly MethodInfo CreateInvokerMethod =
        typeof(InMemoryBusBackgroundService).GetMethod(nameof(CreateInvoker), BindingFlags.NonPublic | BindingFlags.Static)!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("In-Memory Service Bus arka plan servisi başlatıldı.");

        try
        {
            await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
            {
                var messageType = message.GetType();
                var dispatcher = Dispatchers.GetOrAdd(messageType, static type => (
                    typeof(IEventHandler<>).MakeGenericType(type),
                    (Func<object, IEventOrMessage, CancellationToken, Task>)CreateInvokerMethod.MakeGenericMethod(type).Invoke(null, null)!));

                using var scope = serviceProvider.CreateScope();
                foreach (var handler in scope.ServiceProvider.GetServices(dispatcher.HandlerType))
                {
                    if (handler is null) continue;

                    // Her handler ayrı denenir: biri hata verirse aynı mesajın diğer handler'ları yine çalışır.
                    try
                    {
                        await dispatcher.Invoke(handler, message, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // Uygulama kapanıyor: işlenmekte olan mesaj iptal edildi, hata olarak loglanmaz; dış catch sessizce bitirir.
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Mesaj işlenirken hata oluştu: {MessageType} / {Handler}", messageType.Name, handler.GetType().Name);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Kapanış (ya da uygulama hiç açılamadı, ör. port dolu): iptal normaldir, FTL/hata olarak loglanmaz;
            // asıl başlatma hatası host tarafından loglanır.
            logger.LogInformation("In-Memory Service Bus arka plan servisi durduruluyor.");
        }
    }

    private static Func<object, IEventOrMessage, CancellationToken, Task> CreateInvoker<TEvent>() where TEvent : IEventOrMessage =>
        static (handler, message, cancellationToken) => ((IEventHandler<TEvent>)handler).HandleAsync((TEvent)message, cancellationToken);
}
