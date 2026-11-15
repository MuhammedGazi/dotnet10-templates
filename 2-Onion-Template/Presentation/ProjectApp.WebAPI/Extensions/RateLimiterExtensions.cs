using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ProjectApp.Application.Common.Wrappers;

namespace ProjectApp.WebAPI.Extensions;

/// <summary>
/// Built-in ASP.NET Core RateLimiter (System.Threading.RateLimiting).
/// - Global: FixedWindow (60 istek / 1 dakika), IP veya Kullanıcı Adı bazlı.
/// - Auth: FixedWindow (varsayılan 15 istek / 1 dakika, RateLimiting:Auth:PermitLimit), Brute-Force koruması.
/// </summary>
public static class RateLimiterExtensions
{
    public const string FixedPolicyName = "fixed";
    public const string AuthPolicyName  = "auth-policy";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting:Fixed");
        int permitLimit   = section.GetValue<int>("PermitLimit",   60);
        int windowSeconds = section.GetValue<int>("WindowSeconds", 60);
        int queueLimit    = section.GetValue<int>("QueueLimit",    0);

        services.AddRateLimiter(options =>
        {
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

            // [Hazır örnek] Uca özel sabit pencere politikası: [EnableRateLimiting(RateLimiterExtensions.FixedPolicyName)]
            // Not: Global limiter tüm uçlara zaten uygulanır; bu politikayı bir uca eklerseniz istek her iki limitte de sayılır.
            // Bu yüzden uca özel daha sıkı bir limit gerektiğinde, değerleri global limitten farklı ayarlayarak kullanın.
            // Global limiter gibi kullanıcı/IP bazlı bölümlenir; bölümlenmemiş tek kova tüm kullanıcıların aynı havuzu
            // paylaşmasına yol açardı.
            options.AddPolicy(FixedPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserOrIpPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = permitLimit,
                        Window               = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = queueLimit
                    }));

            // Kimlik doğrulama uç noktaları için katı Brute-Force koruma politikası
            int authPermitLimit = configuration.GetValue<int>("RateLimiting:Auth:PermitLimit", 15);
            options.AddPolicy(AuthPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = authPermitLimit,
                        Window               = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    }));

            // Tüm endpoint'lere varsayılan olarak IP/Kullanıcı bazlı Global Limiter uygula.
            // auth-policy taşıyan uçlar muaftır: zaten daha sıkı IP bazlı auth limitiyle korunurlar; genel trafik
            // aynı IP'nin login'ini 429'a düşürmesin (ortak IP'de DoS). Yol değil endpoint metadata'sı kontrol edilir.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                ctx.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == AuthPolicyName
                    ? RateLimitPartition.GetNoLimiter("auth-exempt")
                    : RateLimitPartition.GetFixedWindowLimiter(
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
