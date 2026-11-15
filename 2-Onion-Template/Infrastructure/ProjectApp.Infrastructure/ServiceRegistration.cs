using ProjectApp.Application.Contracts.Identity;
using ProjectApp.Application.Contracts.Infrastructure;
using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Infrastructure.Identity;
using ProjectApp.Infrastructure.Persistence;
using ProjectApp.Infrastructure.Persistence.Context;
using ProjectApp.Infrastructure.Persistence.Interceptors;
using ProjectApp.Infrastructure.Persistence.Repositories;
using ProjectApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectApp.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tek anahtar: boş/eksik bağlantı dizesi açılışta yakalanır (ilk DB isteğinde 500 yerine).
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil veya boş. Ortam değişkeni ConnectionStrings__DefaultConnection ile verin.");
        }

        // Interceptor ICurrentUserService kullandığından Scoped çözülmeli
        services.AddScoped<AuditDbContextInterceptor>();

        // EF Core AppDbContext
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
            options.AddInterceptors(sp.GetRequiredService<AuditDbContextInterceptor>());
        });

        // ASP.NET Core Identity & Lockout
        services.AddIdentityCore<AppUser>(options =>
        {
            // Güçlendirilmiş Parola Politikası
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;

            // Brute-Force Koruma Politikası (Hesap Kilitleme)
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddErrorDescriber<TurkishIdentityErrorDescriber>();

        // JWT Ayarları (başlangıçta doğrulanır) & Kimlik Doğrulama / Kullanıcı Servisleri
        services.AddOptions<JwtOption>()
            .Bind(configuration.GetSection(JwtOption.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // Repository & UnitOfWork Kayıtları
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // E-Posta Servisi
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
