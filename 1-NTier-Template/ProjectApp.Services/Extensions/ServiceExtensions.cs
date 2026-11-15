using System.Reflection;
using ProjectApp.Services.Common.Email;
using ProjectApp.Services.Features.Auth;
using ProjectApp.Services.Features.Categories;
using ProjectApp.Services.Features.Products;
using ProjectApp.Services.Features.Users;
using ProjectApp.Services.Identity;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectApp.Services.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Mapster konfigürasyonu
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(typeof(ServiceAssembly).Assembly);
        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        // FluentValidation (otomatik doğrulama API katmanındaki FluentValidationFilter ile yapılır)
        services.AddValidatorsFromAssembly(typeof(ServiceAssembly).Assembly);

        // Options
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // E-posta Servisi (varsayılan: loglama)
        services.AddScoped<IEmailService, LoggingEmailService>();

        // Feature Servisleri
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}
