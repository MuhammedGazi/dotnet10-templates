using ProjectApp.Application;
using ProjectApp.Application.Contracts.Identity;
using ProjectApp.Infrastructure;
using ProjectApp.Infrastructure.Identity;
using ProjectApp.WebAPI.Extensions;
using ProjectApp.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Sunucu teknolojisini ifşa eden 'Server: Kestrel' başlığı gönderilmez.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ─── Serilog Yapılandırması ────────────────────────────────────────────────
builder.AddSerilogLogging();

// ─── Altyapı: HttpContext & CurrentUser ────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ─── Onion Katman Servis Kayıtları ─────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ─── WebAPI Sunum Katmanı Kayıtları ───────────────────────────────────────
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddControllersWithFilters();
builder.Services.AddExceptionHandlers();
builder.Services.AddOpenApiDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddForwardedHeadersSetup(builder.Configuration, builder.Environment);
builder.Services.ValidateAllowedHosts(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecksSetup(builder.Configuration);

// ───────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ───────────────────────────────────────────────────────────────────────────

// ─── HTTP İstek Hattı (Middleware Pipeline) ────────────────────────────────
app.UseConfigurePipeline();

// ─── Identity Tohumlama (Yalnızca Development Ortamında) ───────────────────
// Development: migration uygulanır, Seed:AdminPassword verilmişse admin oluşturulur.
// Roller (Admin/User) AppRoleConfiguration.HasData ile migration üzerinden her ortama gelir;
// Production'da şema ve roller 'dotnet ef database update' ile kurulur.
if (app.Environment.IsDevelopment())
{
    await IdentityDataSeeder.SeedIdentityDataAsync(app.Services);
}

// Admin şifre kurtarma (her ortamda; yalnızca Seed:ResetAdminPassword=true iken çalışır, bkz. IdentityDataSeeder).
await IdentityDataSeeder.ResetAdminPasswordIfRequestedAsync(app.Services);

app.Run();
