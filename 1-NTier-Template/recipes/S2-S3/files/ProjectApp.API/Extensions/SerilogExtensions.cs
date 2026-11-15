using System.Globalization;
using Serilog;

namespace ProjectApp.API.Extensions;

/// <summary>
/// Serilog loglama yapılandırması.
/// Hedefler: Console + günlük dönen dosya (logs/log-.txt).
/// </summary>
public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    formatProvider: CultureInfo.InvariantCulture,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {CorrelationId}{NewLine}{Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    formatProvider: CultureInfo.InvariantCulture,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
        });

        return builder;
    }
}
