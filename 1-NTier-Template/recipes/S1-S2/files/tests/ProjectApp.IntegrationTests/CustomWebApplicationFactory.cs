using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Repositories.Interceptors;

namespace ProjectApp.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Tüm DbContext örnekleri aynı açık bağlantıyı paylaşır; böylece bellek içi SQLite verisi korunur.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    static CustomWebApplicationFactory()
    {
        // Program.cs yapılandırmayı build öncesinde okuduğu için ortam değişkenleri kullanılır.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-signing-key-en-az-32-karakter-uzunlugunda!");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=test;Database=test");
        Environment.SetEnvironmentVariable("RateLimiting__Fixed__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "100000");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // SQL Server kayıtlarını (DbContext, options ve yapılandırmaları) kaldır
            var toRemove = services
                .Where(d => d.ServiceType == typeof(AppDbContext) ||
                            (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext))))
                .ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            // DbContextOptions<AppDbContext> kaydı için standart AddDbContext; ardından
            // AppDbContext istekleri RowVersion'ı düzelten TestAppDbContext'e yönlendirilir.
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditDbContextInterceptor>());
            });
            services.AddScoped<AppDbContext>(sp => ActivatorUtilities.CreateInstance<TestAppDbContext>(sp));
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        // Roller (Admin/User) AppRoleConfiguration.HasData ile EnsureCreated sırasında gelir.

        return host;
    }

    /// <summary>
    /// Test kullanıcısı açıp giriş yapar (kayıt admin üzerinden yapılır). Eski testler bu yardımcıyı kullanır;
    /// yeni testlerde doğrudan <see cref="TestHelpers.RegisterAndLoginAsync"/> kullanın.
    /// </summary>
    public Task<(string Email, TokenData Tokens)> CreateUserAndLoginAsync(HttpClient client, string routePrefix) =>
        TestHelpers.RegisterAndLoginAsync(this, client, routePrefix);

    public async Task<string> GetAdminAccessTokenAsync(HttpClient client, string routePrefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"admin-{suffix}@example.com";

        using (var scope = Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = new AppUser { Id = Guid.CreateVersion7(), UserName = $"admin{suffix}", Email = email, EmailConfirmed = true };
            var created = await userManager.CreateAsync(admin, TestHelpers.Password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
            var role = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            Assert.True(role.Succeeded);
        }

        var login = await client.PostAsJsonAsync($"{routePrefix}/auth/login", new { email, password = TestHelpers.Password });
        login.EnsureSuccessStatusCode();
        return (await login.ReadDataAsync<TokenData>()).AccessToken;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
