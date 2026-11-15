using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProjectApp.IntegrationTests;

/// <summary>Registration:AllowPublic=true ile çalışan uygulama (açık kayıt modu).</summary>
public class PublicRegistrationWebApplicationFactory : CustomWebApplicationFactory
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Registration:AllowPublic"] = "true" }));
        return base.CreateHost(builder);
    }
}

internal static class RegistrationTestData
{
    public const string Api = "/api";

    /// <summary>Gövdeye rol alanı da eklenir: istemci rol seçememeli (yok sayılmalı).</summary>
    public static (string Email, object Body) NewUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"u-{suffix}@example.com";
        return (email, new { fullName = "Test Kullanıcı", userName = $"user{suffix}", email, password = TestHelpers.Password, role = "Admin" });
    }

    public static async Task<List<string>> LoginAndGetRolesAsync(HttpClient client, string email)
    {
        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        var tokens = await login.ReadDataAsync<TokenData>();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Api}/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await client.SendAsync(request, TestContext.Current.CancellationToken);
        me.EnsureSuccessStatusCode();
        return (await me.ReadDataAsync<CurrentUserData>()).Roles;
    }
}

/// <summary>Varsayılan mod (Registration:AllowPublic=false): kayıt yalnızca Admin'e açık.</summary>
public class RegistrationClosedTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Api = RegistrationTestData.Api;

    [Fact]
    public async Task Register_VarsayilanKapali_Anonim401Doner()
    {
        using var client = factory.CreateClient();

        var response = await TestHelpers.PostRegisterAsync(client, Api, null, RegistrationTestData.NewUser().Body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_VarsayilanKapali_UserRolu403Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);

        var response = await TestHelpers.PostRegisterAsync(client, Api, tokens.AccessToken, RegistrationTestData.NewUser().Body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_VarsayilanKapali_AdminTokeniyle201DonerVeYalnizcaUserRoluVerir()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var (email, body) = RegistrationTestData.NewUser();

        var response = await TestHelpers.PostRegisterAsync(client, Api, adminToken, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var roles = await RegistrationTestData.LoginAndGetRolesAsync(client, email);
        Assert.Equal("User", Assert.Single(roles));
    }
}

/// <summary>Açık mod (Registration:AllowPublic=true): herkes kayıt olur, rol her zaman User.</summary>
public class RegistrationPublicTests(PublicRegistrationWebApplicationFactory factory) : IClassFixture<PublicRegistrationWebApplicationFactory>
{
    private const string Api = RegistrationTestData.Api;

    [Fact]
    public async Task Register_KayitAcikken_Anonim201DonerVeYalnizcaUserRoluVerir()
    {
        using var client = factory.CreateClient();
        var (email, body) = RegistrationTestData.NewUser();

        var response = await TestHelpers.PostRegisterAsync(client, Api, null, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var roles = await RegistrationTestData.LoginAndGetRolesAsync(client, email);
        Assert.Equal("User", Assert.Single(roles));
    }

    [Fact]
    public async Task Register_KayitAcikken_UserTokeniyle201Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);

        var response = await TestHelpers.PostRegisterAsync(client, Api, tokens.AccessToken, RegistrationTestData.NewUser().Body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
