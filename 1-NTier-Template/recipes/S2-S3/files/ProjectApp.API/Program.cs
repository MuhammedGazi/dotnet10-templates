using ProjectApp.API.Extensions;
using ProjectApp.API.Infrastructure;
using ProjectApp.API.Services;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Extensions;
using ProjectApp.Services.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Sunucu teknolojisini ifşa eden 'Server: Kestrel' başlığı gönderilmez.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ─── Loglama ───────────────────────────────────────────────────────────────
builder.AddSerilogLogging();

// ─── Altyapı: HttpContext + CurrentUser ───────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUserService, CurrentUser>();

// ─── Katman Servis Kayıtları ───────────────────────────────────────────────
builder.Services.AddRepositories(builder.Configuration);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddAuthenticationAndAuthorization(builder.Configuration, builder.Environment);

// ─── Controller ve Model Doğrulama ────────────────────────────────────────
builder.Services.AddControllersWithFilters();
builder.Services.AddExceptionHandlers();

// ─── OpenAPI / Scalar Dokümantasyonu ──────────────────────────────────────
builder.Services.AddOpenApiDocumentation();

// ─── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);

// ─── Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddRateLimiting(builder.Configuration);

// ─── Reverse Proxy (X-Forwarded-*) ve AllowedHosts ─────────────────────────
builder.Services.AddForwardedHeadersSetup(builder.Configuration, builder.Environment);
builder.Services.ValidateAllowedHosts(builder.Configuration, builder.Environment);

// ─── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecksSetup(builder.Configuration);

// ───────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ───────────────────────────────────────────────────────────────────────────

// ─── Reverse Proxy Başlıkları (en başta olmalı) ───────────────────────────
app.UseForwardedHeaders();

// ─── Korelasyon Kimliği (X-Correlation-Id; istek logundan önce olmalı) ────
app.UseCorrelationId();

// ─── Serilog İstek Loglama ──────────────────────────────────────────────
// Exception handler'ın dışında olmalı; aksi halde handler'ın döndüğü 409/499 yanıtları 500 olarak loglanır.
app.UseSerilogRequestLogging();

// ─── Global Exception Handler ─────────────────────────────────────────────
app.UseExceptionHandler();

// ─── HTTP Güvenlik Başlıkları ─────────────────────────────────────────────
app.UseSecurityHeaders();

// ─── OpenAPI / Scalar ─────────────────────────────────────────────────────
app.UseOpenApiAndScalar();

// ─── HSTS & HTTPS Yönlendirme ─────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    // Development'ta (http profili) https portu olmadığı için yönlendirme yapılmaz; aksi halde her açılışta uyarı loglanır.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// ─── CORS ─────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.UseCors(CorsExtensions.DevPolicyName);
else
    app.UseCors(CorsExtensions.ProdPolicyName);

// ─── Kimlik Doğrulama → Rate Limiting → Yetkilendirme ─────────────────────
// Rate limiter, kullanıcı kimliğine göre bölümleme yapabilmek için Authentication'dan sonra çalışır.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// ─── Endpoint'ler ─────────────────────────────────────────────────────────
app.MapControllers();
app.MapHealthCheckEndpoints();

// ─── Identity Tohumlama (Yalnızca Development Ortamında) ───────────────────
// Development: migration uygulanır, Seed:AdminPassword verilmişse admin oluşturulur.
// Roller (Admin/User) AppRoleConfiguration.HasData ile migration üzerinden her ortama gelir;
// Production'da şema ve roller 'dotnet ef database update' ile kurulur.
if (app.Environment.IsDevelopment())
{
    await app.SeedIdentityDataAsync();
}

// Admin şifre kurtarma (her ortamda; yalnızca Seed:ResetAdminPassword=true iken çalışır, bkz. IdentityDataSeeder).
await app.ResetAdminPasswordIfRequestedAsync();

app.Run();
