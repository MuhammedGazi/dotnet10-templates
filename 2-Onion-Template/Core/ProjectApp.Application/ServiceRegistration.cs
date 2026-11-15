using System.Reflection;
using ProjectApp.Application.Features.Categories;
using ProjectApp.Application.Features.Products;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectApp.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Mapster nesne eşleme yapılandırması
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        // FluentValidation (otomatik doğrulama WebAPI katmanındaki FluentValidationFilter ile yapılır)
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Feature-Service Deseni Servis Kayıtları
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}
