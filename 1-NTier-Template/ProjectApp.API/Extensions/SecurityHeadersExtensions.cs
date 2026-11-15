namespace ProjectApp.API.Extensions;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Başlıklar yanıt gönderilmeden hemen önce (OnStarting) eklenir. Böylece exception handler'ın
            // yanıtı temizlediği (Response.Clear) 500/409 yanıtlarında da güvenlik başlıkları korunur.
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

                // Scalar UI ve OpenAPI belgesi satır içi script/stil kullandığı için CSP bu yollara uygulanmaz.
                var path = context.Request.Path;
                if (!path.StartsWithSegments("/scalar") && !path.StartsWithSegments("/openapi"))
                {
                    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                }

                return Task.CompletedTask;
            });

            await next();
        });
    }
}
