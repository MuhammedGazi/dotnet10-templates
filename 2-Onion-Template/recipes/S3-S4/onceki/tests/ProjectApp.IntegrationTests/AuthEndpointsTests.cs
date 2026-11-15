using System.Net;
using System.Net.Http.Json;

namespace ProjectApp.IntegrationTests;

public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Api = "/api";

    private static object NewUserBody()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        return new { fullName = "Test Kullanıcı", userName = $"user{suffix}", email = $"u-{suffix}@example.com", password = TestHelpers.Password };
    }

    [Fact]
    public async Task Register_AdminTokeniyle_201DonerVeKullaniciGirisYapabilir()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"u-{suffix}@example.com";

        var response = await TestHelpers.PostRegisterAsync(client, Api, adminToken,
            new { fullName = "Test Kullanıcı", userName = $"user{suffix}", email, password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Register_AyniEmailIkinciKez_400Doner()
    {
        using var client = factory.CreateClient();
        var (email, _) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);

        var response = await TestHelpers.PostRegisterAsync(client, Api, adminToken,
            new { fullName = "Başka Kullanıcı", userName = $"other{Guid.NewGuid():N}"[..15], email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_DogruBilgiler_TokenDoner()
    {
        using var client = factory.CreateClient();

        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_YanlisSifre_401Doner()
    {
        using var client = factory.CreateClient();
        var (email, _) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);

        var response = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = "Yanlis12*Sifre" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_TokenYoksa_401Doner()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"{Api}/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_TokenVarsa_200Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        client.Authorize(tokens.AccessToken);

        var response = await client.GetAsync($"{Api}/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_KullaniciVarsayilanUserRolundeOlusur()
    {
        // Roller migration/HasData ile gelir; seeder çalışmasa bile kayıt User rolünü atayabilmeli.
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        client.Authorize(tokens.AccessToken);

        var response = await client.GetAsync($"{Api}/auth/me", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var me = await response.ReadDataAsync<CurrentUserData>();

        Assert.Contains("User", me.Roles);
    }

    [Fact]
    public async Task RefreshToken_AyniTokenIkinciKezKullanilirsa_401Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var request = new { accessToken = tokens.AccessToken, refreshToken = tokens.RefreshToken };

        var first = await client.PostAsJsonAsync($"{Api}/auth/refresh-token", request, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync($"{Api}/auth/refresh-token", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_BozukAccessToken_401Doner()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{Api}/auth/refresh-token",
            new { accessToken = "bozuk-token", refreshToken = "bozuk-refresh" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
