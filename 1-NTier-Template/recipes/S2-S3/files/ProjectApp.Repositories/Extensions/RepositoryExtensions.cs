using ProjectApp.Repositories.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectApp.Repositories.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        // Boş/eksik bağlantı dizesi açılışta yakalanır (ilk DB isteğinde 500 yerine).
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil veya boş. Ortam değişkeni ConnectionStrings__DefaultConnection ile verin.");
        }

        // interceptor ICurrentUserService'e bağımlı olduğu için DI üzerinden (scoped) çözülür
        services.AddScoped<AuditDbContextInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString,
                sqlServerOptionsAction =>
                {
                    sqlServerOptionsAction.MigrationsAssembly(typeof(RepositoryAssembly).Assembly.FullName);
                });

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditDbContextInterceptor>());
        });

        return services;
    }
}
