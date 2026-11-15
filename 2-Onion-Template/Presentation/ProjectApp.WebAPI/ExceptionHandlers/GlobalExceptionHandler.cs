using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Application.Common.Wrappers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProjectApp.WebAPI.ExceptionHandlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // İstemci isteği iptal ettiyse hata değildir (499 Client Closed Request)
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("İstek istemci tarafından iptal edildi: {Path}", httpContext.Request.Path);
            httpContext.Response.StatusCode = 499;
            return true;
        }

        // Eşzamanlılık çakışması (RowVersion) -> 409
        if (exception is DbUpdateConcurrencyException)
        {
            logger.LogWarning(exception, "Eşzamanlılık çakışması oluştu.");
            var concurrency = ServiceResult.Fail("Kayıt başka bir işlem tarafından değiştirildi, lütfen yenileyip tekrar deneyin.", HttpStatusCode.Conflict);
            httpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(concurrency, cancellationToken);
            return true;
        }

        // Benzersizlik (unique index) ihlali -> 409
        if (exception is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } })
        {
            logger.LogWarning(exception, "Benzersizlik ihlali oluştu.");
            httpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
            httpContext.Response.ContentType = "application/json";
            var conflict = ServiceResult.Fail("Kayıt zaten mevcut (benzersizlik ihlali).", HttpStatusCode.Conflict);
            await httpContext.Response.WriteAsJsonAsync(conflict, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Sunucu tarafında işlenmeyen hata meydana geldi: {Message}", exception.Message);

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var result = ServiceResult.Fail("Sunucuda beklenmeyen bir hata meydana geldi.", HttpStatusCode.InternalServerError);
        await httpContext.Response.WriteAsJsonAsync(result, cancellationToken);

        return true;
    }
}
