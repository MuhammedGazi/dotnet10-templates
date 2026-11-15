using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ProjectApp.WebAPI.Extensions;

public static class ConfigurePipelineExtensions
{
    public static WebApplication UseConfigurePipeline(this WebApplication app)
    {
        // Reverse Proxy Başlıkları (en başta olmalı)
        app.UseForwardedHeaders();

        // Korelasyon kimliği (X-Correlation-Id): istek logundan önce olmalı ki tüm satırlara eklensin
        app.UseCorrelationId();

        // Serilog HTTP İstek Loglaması
        // (Exception handler'ın dışında olmalı; aksi halde handler'ın döndüğü 409/499 yanıtları 500 olarak loglanır)
        app.UseSerilogRequestLogging();

        // Global Exception Handler
        app.UseExceptionHandler();

        // HTTP Güvenlik Başlıkları
        app.UseSecurityHeaders();

        // OpenAPI & Scalar UI Dokümantasyonu
        app.UseOpenApiAndScalar();

        // HSTS & HTTPS Yönlendirme
        if (!app.Environment.IsDevelopment())
        {
            // Development'ta (http profili) https portu olmadığı için yönlendirme yapılmaz; aksi halde her açılışta uyarı loglanır.
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // CORS
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(CorsExtensions.DevPolicyName);
        }
        else
        {
            app.UseCors(CorsExtensions.ProdPolicyName);
        }

        // Kimlik Doğrulama → Rate Limiter → Yetkilendirme
        // (Rate limiter, kullanıcı kimliğine göre bölümleme yapabilmek için Authentication'dan sonra çalışır)
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        // Uç Noktalar
        app.MapControllers();
        app.MapHealthCheckEndpoints();

        return app;
    }
}
