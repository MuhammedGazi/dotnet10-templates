using ProjectApp.WebAPI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.WebAPI.Extensions;

public static class ControllerExtensions
{
    public static IServiceCollection AddControllersWithFilters(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationFilter>();
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        return services;
    }
}
