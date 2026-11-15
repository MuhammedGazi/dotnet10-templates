using ProjectApp.Repositories;
using ProjectApp.Repositories.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectApp.API.Infrastructure;

/// <summary>
/// Uygulama başladığında varsayılan Admin rol ve kullanıcısını oluşturur.
/// Program.cs'de <c>await app.SeedIdentityDataAsync();</c> olarak çağrılır.
/// </summary>
public static class IdentityDataSeeder
{
    private const string DefaultAdminEmail    = "admin@admin.com";
    private const string DefaultAdminUserName = "admin";
    private const string DefaultAdminFullName  = "Administrator";

    public static async Task SeedIdentityDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            await SeedRolesAsync(scope.ServiceProvider, logger);
            await SeedAdminUserAsync(scope.ServiceProvider, app.Configuration, logger);
        }
        catch (Exception ex)
        {
            // Veritabanı yoksa/erişilemiyorsa uygulama sessizce devam etmemeli; açıkça çökmeli.
            logger.LogError(ex, "Identity veri tohumlama sırasında hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Admin şifre kurtarma (her ortamda): <c>Seed__AdminPassword=&lt;yeni şifre&gt;</c> ve <c>Seed__ResetAdminPassword=true</c>
    /// ortam değişkenleriyle uygulamayı bir kez başlatın. Admin şifresi sıfırlanır, kilit kalkar, eski token'lar geçersiz olur
    /// (security stamp yenilenir). Ardından iki değişkeni HEMEN kaldırın.
    /// </summary>
    public static async Task ResetAdminPasswordIfRequestedAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Seed:ResetAdminPassword"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await ResetAdminPasswordAsync(scope.ServiceProvider, app.Configuration["Seed:AdminPassword"], logger);
    }

    /// <summary>Varsayılan admin hesabının şifresini sıfırlar; başarılıysa true döner.</summary>
    public static async Task<bool> ResetAdminPasswordAsync(IServiceProvider services, string? newPassword, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            logger.LogWarning("Seed:ResetAdminPassword açık ama Seed:AdminPassword tanımlı değil; sıfırlama yapılmadı.");
            return false;
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (admin is null)
        {
            logger.LogWarning("Sıfırlanacak admin kullanıcı ({Email}) bulunamadı.", DefaultAdminEmail);
            return false;
        }

        // Refresh token da iptal edilir (aksi halde çalınmış bir refresh token yeni access token alabilirdi).
        // ResetPasswordAsync kullanıcıyı kaydederken bu alanlar da aynı yazımda kaydedilir.
        admin.RefreshToken = null;
        admin.RefreshTokenExpiryTime = null;

        var token = await userManager.GeneratePasswordResetTokenAsync(admin);
        var result = await userManager.ResetPasswordAsync(admin, token, newPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("Admin şifresi sıfırlanamadı: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        await userManager.SetLockoutEndDateAsync(admin, null);
        await userManager.ResetAccessFailedCountAsync(admin);

        logger.LogWarning("Admin şifresi Seed:ResetAdminPassword ile sıfırlandı. Seed__AdminPassword ve Seed__ResetAdminPassword değişkenlerini şimdi kaldırın.");
        return true;
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

        string[] roles = [RoleNames.Admin, RoleNames.User];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole { Name = role });
                logger.LogInformation("'{Role}' rolü oluşturuldu.", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
    {
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Seed:AdminPassword tanımlı değil, admin kullanıcı oluşturulmadı.");
            return;
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        var adminUser = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (adminUser is not null)
        {
            logger.LogInformation("Admin kullanıcı zaten mevcut, tohumlama atlandı.");
            return;
        }

        adminUser = new AppUser
        {
            Id = Guid.CreateVersion7(),
            FullName = DefaultAdminFullName,
            UserName = DefaultAdminUserName,
            Email = DefaultAdminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
            logger.LogInformation("Admin kullanıcı '{Email}' oluşturuldu ve '{Role}' rolüne atandı.",
                DefaultAdminEmail, RoleNames.Admin);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Admin kullanıcı oluşturulamadı: {Errors}", errors);
        }
    }
}
