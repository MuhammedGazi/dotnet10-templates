using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProjectApp.API.Extensions;

/// <summary>
/// Sağlık denetimleri.
/// GET /health → liveness  (uygulama ayakta mı?)
/// GET /health/ready → readiness (SQL bağlantısı hazır mı?)
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksSetup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil veya boş. Ortam değişkeni ConnectionStrings__DefaultConnection ile verin.");
        }

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
                name: "sql-server",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"]);

        return services;
    }

    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Liveness — uygulama çalışıyor mu?
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false   // hiçbir check çalıştırma, sadece 200 döner
        }).AllowAnonymous();

        // Readiness — tüm checkler (DB dahil)
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}
