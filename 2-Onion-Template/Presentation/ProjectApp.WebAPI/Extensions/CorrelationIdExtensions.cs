using Serilog.Context;

namespace ProjectApp.WebAPI.Extensions;

/// <summary>
/// Her isteğe bir korelasyon kimliği atar: istemci geçerli bir X-Correlation-Id gönderdiyse o, yoksa
/// ASP.NET Core TraceIdentifier kullanılır. Kimlik yanıt başlığına yazılır ve isteğin tüm log satırlarına
/// (Serilog LogContext → {CorrelationId}) eklenir; böylece aynı isteğe ait loglar birbirine bağlanabilir.
/// </summary>
public static class CorrelationIdExtensions
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 64;

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers[HeaderName].ToString();
            if (!IsValid(correlationId))
            {
                // Log enjeksiyonunu önlemek için istemci değeri yalnızca güvenli karakterlerden oluşuyorsa kabul edilir.
                correlationId = context.TraceIdentifier;
            }

            context.TraceIdentifier = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }

    private static bool IsValid(string value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
