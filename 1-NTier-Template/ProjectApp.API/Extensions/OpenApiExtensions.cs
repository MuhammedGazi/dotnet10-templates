using ProjectApp.API.Infrastructure.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace ProjectApp.API.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }

    public static WebApplication UseOpenApiAndScalar(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("ProjectApp API")
                    .WithTheme(ScalarTheme.Moon);
            });
            app.MapGet("/scalar", () => Microsoft.AspNetCore.Http.Results.Redirect("/scalar/v1")).ExcludeFromDescription();
        }

        return app;
    }
}
