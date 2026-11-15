using System.Security.Claims;
using System.Text;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ProjectApp.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt ayarları appsettings.json içerisinde bulunamadı.");

        // ValidateOnStart'tan önce burada anahtar kullanıldığı için okunur bir hata verilir
        // (aksi halde boş anahtar kriptografik IDX10703 istisnası üretir).
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey tanımlı değil veya 32 karakterden kısa. Ortam değişkeni Jwt__SigningKey ile verin.");
        }

        // ASP.NET Core Identity & Lockout
        services.AddIdentityCore<AppUser>(options =>
        {
            // Güçlendirilmiş Parola Politikası
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;

            // Brute-Force Koruma Politikası (Hesap Kilitleme)
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddErrorDescriber<TurkishIdentityErrorDescriber>();

        // JWT Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.Zero
            };

            // Access token anında iptali: token içindeki security stamp veritabanındakiyle karşılaştırılır.
            // Refresh token olmadığı için token ömrü uzundur; şifre değişikliği stamp'i yeniler ve eski
            // token'lar süresi dolmadan geçersiz olur. Bedeli: kimliği doğrulanmış her istekte tek indeksli okuma
            // (anonim ziyaretçi istekleri etkilenmez).
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var tokenStamp = context.Principal?.FindFirstValue(TokenService.SecurityStampClaimType);
                    if (userId is null || tokenStamp is null)
                    {
                        context.Fail("Oturum geçersiz.");
                        return;
                    }

                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user is null || !string.Equals(user.SecurityStamp, tokenStamp, StringComparison.Ordinal))
                    {
                        context.Fail("Oturum geçersiz kılınmış.");
                    }
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
