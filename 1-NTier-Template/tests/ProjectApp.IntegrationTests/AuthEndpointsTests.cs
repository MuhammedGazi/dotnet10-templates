using System.Net;
using System.Net.Http.Json;

namespace ProjectApp.IntegrationTests;

public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Api = "/api";

    [Fact]
    public async Task Login_DogruBilgiler_TokenDoner()
    {
        using var client = factory.CreateClient();

        var (_, tokens) = await factory.CreateUserAndLoginAsync(client, Api);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.True(tokens.AccessTokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_YanlisSifre_401Doner()
    {
        using var client = factory.CreateClient();
        var (email, _) = await factory.CreateUserAndLoginAsync(client, Api);

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
        var (_, tokens) = await factory.CreateUserAndLoginAsync(client, Api);
        client.Authorize(tokens.AccessToken);

        var response = await client.GetAsync($"{Api}/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.ReadDataAsync<CurrentUserData>();
        Assert.Contains("User", me.Roles);
    }
}
