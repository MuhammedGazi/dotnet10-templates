using ProjectApp.WebAPI.ExceptionHandlers;

namespace ProjectApp.WebAPI.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IServiceCollection AddExceptionHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<CriticalExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
