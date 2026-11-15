using ProjectApp.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.API.Extensions;

public static class ControllerExtensions
{
    public static IServiceCollection AddControllersWithFilters(this IServiceCollection services)
    {
        // FluentValidationFilter'ın devreye girebilmesi için varsayılan ModelState filtresini baskıla
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
