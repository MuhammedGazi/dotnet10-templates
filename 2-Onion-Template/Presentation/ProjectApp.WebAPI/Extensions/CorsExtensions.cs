namespace ProjectApp.WebAPI.Extensions;

/// <summary>
/// CORS politikaları.
/// Development: tüm kaynaklara izin verilir.
/// Diğer ortamlar: yalnızca "Cors:AllowedOrigins" listesindeki kaynaklar. Production'da liste boşsa uygulama açılmaz.
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

        // Canlı ortamda izinli kaynak yoksa açılışta hata verilir; asla AllowAnyOrigin'e düşülmez.
        if (environment.IsProduction() && allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException(
                "Canlı ortamda (Production) CORS için en az bir izin verilen kaynak ('Cors:AllowedOrigins') tanımlanmalıdır.");
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
