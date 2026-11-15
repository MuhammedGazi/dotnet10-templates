using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ProjectApp.Services;

namespace ProjectApp.API.Extensions;

/// <summary>
/// Built-in ASP.NET Core RateLimiter (System.Threading.RateLimiting).
/// Global: FixedWindow (varsayılan 60 istek / 1 dakika), IP veya Kullanıcı Kimliği bazlı.
/// </summary>
public static class RateLimiterExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            var section = configuration.GetSection("RateLimiting:Fixed");
            int permitLimit   = section.GetValue<int>("PermitLimit",   60);
            int windowSeconds = section.GetValue<int>("WindowSeconds", 60);
            int queueLimit    = section.GetValue<int>("QueueLimit",    0);

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 429 yanıtı da ServiceResult formatında döner; pencere bilgisi varsa Retry-After eklenir.
            // Not: FixedWindow limiter kalan süreyi değil pencerenin tamamını verir; Retry-After bir üst sınırdır.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var body = ServiceResult.Fail("Çok fazla istek gönderildi, lütfen biraz sonra tekrar deneyin.", HttpStatusCode.TooManyRequests);
                await context.HttpContext.Response.WriteAsJsonAsync(body, cancellationToken);
            };

            // Tüm endpoint'lere varsayılan olarak IP/Kullanıcı bazlı Global Limiter uygula
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserOrIpPartitionKey(ctx),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = permitLimit,
                        Window               = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = queueLimit
                    }));
        });

        return services;
    }

    private static string GetUserOrIpPartitionKey(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";
}
