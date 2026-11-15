using System.Security.Claims;
using System.Text;
using ProjectApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using ProjectApp.WebAPI.Authorization;
using ProjectApp.Application.Common.Security;

namespace ProjectApp.WebAPI.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>/auth/register ucunun yetkilendirme politikası (bkz. RegistrationOptions).</summary>
    public const string RegisterPolicyName = "Register";

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = configuration.GetSection(JwtOption.SectionName).Get<JwtOption>()
            ?? throw new InvalidOperationException("Jwt ayarları appsettings.json içerisinde bulunamadı.");

        // ValidateOnStart'tan önce burada anahtar kullanıldığı için okunur bir hata verilir
        // (aksi halde boş anahtar kriptografik IDX10703 istisnası üretir).
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey tanımlı değil veya 32 karakterden kısa. Ortam değişkeni Jwt__SigningKey ile verin.");
        }

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
            // Logout, şifre/e-posta değişikliği, şifre sıfırlama ve hesap silme stamp'i yeniler; eski token'lar
            // süresi dolmadan geçersiz olur. Bedeli: kimliği doğrulanmış her istekte kullanıcı başına bir okuma
            // (aynı istekte servisler FindByIdAsync çağırırsa EF change tracker'dan döner, ek sorgu olmaz).
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
                    if (user is null || user.IsDeleted || !string.Equals(user.SecurityStamp, tokenStamp, StringComparison.Ordinal))
                    {
                        context.Fail("Oturum geçersiz kılınmış.");
                    }
                }
            };
        });

        // Kayıt politikası: Registration:AllowPublic true ise herkes, false (varsayılan) ise yalnızca Admin.
        // Ayar IOptions ile okunur; değişiklik yeniden başlatma ister.
        services.Configure<RegistrationOptions>(configuration.GetSection(RegistrationOptions.SectionName));
        services.AddSingleton<IAuthorizationHandler, RegistrationAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(RegisterPolicyName, policy => policy.AddRequirements(new RegistrationRequirement()));

        return services;
    }
}
