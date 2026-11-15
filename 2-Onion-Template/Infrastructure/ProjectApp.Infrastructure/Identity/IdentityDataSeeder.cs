using ProjectApp.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectApp.Infrastructure.Persistence.Context;

namespace ProjectApp.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    private const string DefaultAdminEmail    = "admin@admin.com";
    private const string DefaultAdminUserName = "admin";
    private const string DefaultAdminFullName = "Administrator";

    public static async Task SeedIdentityDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentityDataSeeder));

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            await SeedRolesAsync(scope.ServiceProvider, logger);
            await SeedAdminUserAsync(scope.ServiceProvider, logger);
        }
        catch (Exception ex)
        {
            // Veritabanı yoksa/erişilemiyorsa uygulama sessizce devam etmemeli; açıkça çökmeli.
            logger.LogError(ex, "Identity verileri tohumlanırken hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Admin şifre kurtarma (her ortamda): <c>Seed__AdminPassword=&lt;yeni şifre&gt;</c> ve <c>Seed__ResetAdminPassword=true</c>
    /// ortam değişkenleriyle uygulamayı bir kez başlatın. Admin şifresi sıfırlanır, kilit kalkar, eski token'lar geçersiz olur
    /// (security stamp yenilenir). Ardından iki değişkeni HEMEN kaldırın.
    /// </summary>
    public static async Task ResetAdminPasswordIfRequestedAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("Seed:ResetAdminPassword"))
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentityDataSeeder));
        await ResetAdminPasswordAsync(scope.ServiceProvider, configuration["Seed:AdminPassword"], logger);
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

    private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var adminPassword = serviceProvider.GetRequiredService<IConfiguration>()["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Seed:AdminPassword tanımlı değil, admin kullanıcı oluşturulmadı.");
            return;
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        var adminUser = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (adminUser is not null)
        {
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
            logger.LogInformation("Admin kullanıcı '{Email}' oluşturuldu ve '{Role}' rolüne atandı.", DefaultAdminEmail, RoleNames.Admin);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Admin kullanıcı oluşturulamadı: {Errors}", errors);
        }
    }
}
