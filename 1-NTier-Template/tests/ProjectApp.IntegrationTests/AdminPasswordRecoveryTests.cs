using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectApp.API.Infrastructure;
using ProjectApp.Repositories.Identity;

namespace ProjectApp.IntegrationTests;

public class AdminPasswordRecoveryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Api = "/api";
    private const string AdminEmail = "admin@admin.com";
    private const string NewPassword = "YeniSifre12*";

    [Fact]
    public async Task ResetAdminPassword_EskiOturumuIptalEderKilidiKaldirirYeniSifreyleGirisAcilir()
    {
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = new AppUser { Id = Guid.CreateVersion7(), UserName = "admin", Email = AdminEmail, EmailConfirmed = true };
            Assert.True((await userManager.CreateAsync(admin, TestHelpers.Password)).Succeeded);
        }

        // Kurtarmadan önce açılmış (ör. çalınmış) oturum
        var before = await client.PostAsJsonAsync($"{Api}/auth/login", new { email = AdminEmail, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        before.EnsureSuccessStatusCode();
        var oldTokens = await before.ReadDataAsync<TokenData>();

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = await userManager.FindByEmailAsync(AdminEmail);
            await userManager.SetLockoutEndDateAsync(admin!, DateTimeOffset.UtcNow.AddHours(1));

            Assert.True(await IdentityDataSeeder.ResetAdminPasswordAsync(scope.ServiceProvider, NewPassword, NullLogger.Instance));
            Assert.False(await IdentityDataSeeder.ResetAdminPasswordAsync(scope.ServiceProvider, "", NullLogger.Instance));
        }

        using var me = new HttpRequestMessage(HttpMethod.Get, $"{Api}/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oldTokens.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(me, TestContext.Current.CancellationToken)).StatusCode);

        var oldLogin = await client.PostAsJsonAsync($"{Api}/auth/login", new { email = AdminEmail, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        var newLogin = await client.PostAsJsonAsync($"{Api}/auth/login", new { email = AdminEmail, password = NewPassword }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode); // kilit de kalkmış olmalı
    }
}
