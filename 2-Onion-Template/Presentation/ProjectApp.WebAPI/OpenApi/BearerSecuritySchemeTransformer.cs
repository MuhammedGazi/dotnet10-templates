using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ProjectApp.WebAPI.OpenApi;

/// <summary>
/// Bearer güvenlik şemasını belgeye ekler ve güvenlik gereksinimini yalnızca [Authorize] uçlara uygular
/// (anonim uçlarda Scalar kilit simgesi göstermez). Yetkili uçlara 401, rol isteyenlere 403 yanıtı eklenir.
/// </summary>
public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.All(authScheme => authScheme.Name != JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "JWT Bearer token. Örnek: \"Bearer {token}\""
        };
    }

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0 || metadata.OfType<IAllowAnonymous>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document, null)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Kimlik doğrulama gerekli (geçerli bir Bearer token gönderin)." });
        if (authorizeData.Any(a => !string.IsNullOrEmpty(a.Roles) || !string.IsNullOrEmpty(a.Policy)))
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Bu işlem için yetkiniz yok." });
        }

        return Task.CompletedTask;
    }
}
