using ProjectApp.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProjectApp.WebAPI.ExceptionHandlers;

public class CriticalExceptionHandler(ILogger<CriticalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is CriticalException criticalException)
        {
            logger.LogCritical(criticalException, "Kritik seviye bir hata oluştu: {Message}", criticalException.Message);
            return ValueTask.FromResult(false); // GlobalExceptionHandler ProblemDetails dönebilsin diye false
        }

        return ValueTask.FromResult(false);
    }
}
