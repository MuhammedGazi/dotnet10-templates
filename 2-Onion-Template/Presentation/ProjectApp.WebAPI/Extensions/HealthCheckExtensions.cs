using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProjectApp.WebAPI.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksSetup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tek anahtar: boş/eksik bağlantı dizesi açılışta yakalanır (ilk DB isteğinde 500 yerine).
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
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}
