using ProjectApp.API.ExceptionHandlers;

namespace ProjectApp.API.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IServiceCollection AddExceptionHandlers(this IServiceCollection services)
    {
        // Sıralama: Critical -> Global
        services.AddExceptionHandler<CriticalExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
