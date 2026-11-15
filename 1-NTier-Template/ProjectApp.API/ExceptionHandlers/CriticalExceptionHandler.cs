using ProjectApp.Services.ExceptionHandlers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProjectApp.API.ExceptionHandlers;

public class CriticalExceptionHandler(ILogger<CriticalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is CriticalException)
        {
            logger.LogCritical(exception, "Kritik seviye hata oluştu! Acil bildirim gönderiliyor...");
        }

        // Zincirin GlobalExceptionHandler'a devam etmesi için false döner
        return ValueTask.FromResult(false);
    }
}
