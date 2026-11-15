using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ProjectApp.API.Extensions;

/// <summary>
/// CORS politikaları.
/// Development: tüm kaynaklara izin verilir.
/// Diğer ortamlar: yalnızca "Cors:AllowedOrigins" listesindeki kaynaklar. Production'da liste boşsa uygulama açılır,
/// hiçbir kaynağa izin verilmez ve uyarı loglanır.
/// Not: Yapılandırma dizileri indeks bazında birleştiği için geliştirme kaynakları (localhost) yalnızca
/// appsettings.Development.json içinde tutulur; appsettings.json'daki liste boş kalmalıdır.
/// </summary>
public static class CorsExtensions
{
    public const string DevPolicyName  = "DevCorsPolicy";
    public const string ProdPolicyName = "ProdCorsPolicy";

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var allowedOrigins = (configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [])
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        // Canlı ortamda izinli kaynak yoksa uygulama açılır ama hiçbir kaynağa izin verilmez (asla AllowAnyOrigin'e düşülmez);
        // seçenekler açılışta bir kez oluşturulduğu için uyarı da bir kez yazılır.
        if (environment.IsProduction() && allowedOrigins.Length == 0)
        {
            services.AddOptions<CorsOptions>().Configure<ILoggerFactory>((_, loggerFactory) =>
                loggerFactory.CreateLogger(typeof(CorsExtensions)).LogWarning(
                    "Cors:AllowedOrigins boş; tarayıcıdan gelen çapraz kaynak (CORS) isteklerine izin verilmeyecek."));
        }

        services.AddCors(options =>
        {
            options.AddPolicy(DevPolicyName, policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());

            // Liste boşsa (ör. Testing ortamı) hiçbir kaynağa izin verilmez.
            options.AddPolicy(ProdPolicyName, policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials());
        });

        return services;
    }
}
