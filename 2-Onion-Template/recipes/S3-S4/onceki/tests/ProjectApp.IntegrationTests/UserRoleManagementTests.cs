using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ProjectApp.WebAPI.Services;
using ProjectApp.Application.Common.Security;
using ProjectApp.Infrastructure.Identity;
using ProjectApp.Infrastructure.Persistence.Context;

namespace ProjectApp.IntegrationTests;

public record UserRolesData(Guid UserId, List<string> Roles);

/// <summary>
/// Admin'in kullanıcı rollerini yönetmesi (GET/PUT users/{id}/roles) ve ICurrentUserService.IsInRole.
/// </summary>
public class UserRoleManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Api = "/api";

    [Fact]
    public async Task RolDegisikligi_EskiAccessToken401_YenidenGiristeYeniRollerGelir()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var (email, userTokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var userId = await GetCurrentUserIdAsync(client, userTokens.AccessToken);
        var customRole = await CreateRoleAsync("Manager");

        var put = await SendAsync(client, HttpMethod.Put, $"{Api}/users/{userId}/roles", adminToken,
            new { roles = new[] { RoleNames.User, customRole.ToLowerInvariant() } });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var oldTokenMe = await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", userTokens.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenMe.StatusCode);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        var newTokens = await login.ReadDataAsync<TokenData>();

        var me = await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", newTokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var current = await me.ReadDataAsync<CurrentUserData>();
        Assert.Equal(new[] { customRole, RoleNames.User }.Order(), current.Roles.Order());

        var get = await SendAsync(client, HttpMethod.Get, $"{Api}/users/{userId}/roles", adminToken);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var roles = await get.ReadDataAsync<UserRolesData>();
        Assert.Equal(userId, roles.UserId);
        Assert.Equal(new[] { customRole, RoleNames.User }.Order(), roles.Roles.Order());
    }

    [Fact]
    public async Task RolDegisikligi_EskiRefreshToken401Doner()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var (_, userTokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var userId = await GetCurrentUserIdAsync(client, userTokens.AccessToken);

        var put = await SendAsync(client, HttpMethod.Put, $"{Api}/users/{userId}/roles", adminToken,
            new { roles = new[] { RoleNames.User, RoleNames.Admin } });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var refresh = await client.PostAsJsonAsync($"{Api}/auth/refresh-token",
            new { accessToken = userTokens.AccessToken, refreshToken = userTokens.RefreshToken }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task AdminRoluVerilenKullanici_YeniTokenIleYonetimUcunaErisir()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var (email, userTokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var userId = await GetCurrentUserIdAsync(client, userTokens.AccessToken);

        var before = await SendAsync(client, HttpMethod.Get, $"{Api}/users/{userId}/roles", userTokens.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

        var put = await SendAsync(client, HttpMethod.Put, $"{Api}/users/{userId}/roles", adminToken,
            new { roles = new[] { RoleNames.Admin } });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        var newTokens = await login.ReadDataAsync<TokenData>();

        // Yeni token'daki rol claim'i [Authorize(Roles = Admin)] tarafından okunur
        var after = await SendAsync(client, HttpMethod.Get, $"{Api}/users/{userId}/roles", newTokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        Assert.Equal(new[] { RoleNames.Admin }, (await after.ReadDataAsync<UserRolesData>()).Roles);
    }

    [Fact]
    public async Task AdminOlmayan403_Anonim401Doner()
    {
        using var client = factory.CreateClient();
        var (_, userTokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var userId = await GetCurrentUserIdAsync(client, userTokens.AccessToken);
        var url = $"{Api}/users/{userId}/roles";
        var body = new { roles = new[] { RoleNames.Admin } };

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(client, HttpMethod.Get, url, userTokens.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(client, HttpMethod.Put, url, userTokens.AccessToken, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Get, url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Put, url, null, body)).StatusCode);

        // Kullanıcı kendine Admin veremedi; rolü hâlâ User
        var me = await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", userTokens.AccessToken);
        Assert.Equal(new[] { RoleNames.User }, (await me.ReadDataAsync<CurrentUserData>()).Roles);
    }

    [Fact]
    public async Task OlmayanRol_VeBosListe_400Doner()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var (_, userTokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var userId = await GetCurrentUserIdAsync(client, userTokens.AccessToken);
        var url = $"{Api}/users/{userId}/roles";

        var unknown = await SendAsync(client, HttpMethod.Put, url, adminToken, new { roles = new[] { RoleNames.User, "YokRol" } });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var unknownBody = await unknown.Content.ReadFromJsonAsync<ApiResponse<object>>(TestContext.Current.CancellationToken);
        Assert.Contains(unknownBody!.ErrorMessage!, m => m.Contains("YokRol"));

        var empty = await SendAsync(client, HttpMethod.Put, url, adminToken, new { roles = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var missing = await SendAsync(client, HttpMethod.Put, url, adminToken, new { });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        // Başarısız istekler oturumu düşürmez ve rolleri değiştirmez
        var me = await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", userTokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(new[] { RoleNames.User }, (await me.ReadDataAsync<CurrentUserData>()).Roles);
    }

    [Fact]
    public async Task OlmayanVeSilinmisKullanici_404Doner()
    {
        using var client = factory.CreateClient();
        var adminToken = await factory.GetAdminAccessTokenAsync(client, Api);
        var body = new { roles = new[] { RoleNames.User } };

        var unknownId = Guid.CreateVersion7();
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(client, HttpMethod.Get, $"{Api}/users/{unknownId}/roles", adminToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{unknownId}/roles", adminToken, body)).StatusCode);

        var (_, userTokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        var deletedId = await GetCurrentUserIdAsync(client, userTokens.AccessToken);
        var delete = await SendAsync(client, HttpMethod.Delete, $"{Api}/users/account", userTokens.AccessToken,
            new { currentPassword = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(client, HttpMethod.Get, $"{Api}/users/{deletedId}/roles", adminToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{deletedId}/roles", adminToken, body)).StatusCode);
    }

    [Fact]
    public async Task SonAktifAdmin_KendiAdminRolunuKaldiramaz()
    {
        // Paylaşılan fixture'da başka testlerin Admin'leri bulunur; "son Admin" durumu için ayrı veritabanı kullanılır.
        using var isolated = new CustomWebApplicationFactory();
        using var client = isolated.CreateClient();
        var adminToken = await isolated.GetAdminAccessTokenAsync(client, Api);
        var adminId = await GetCurrentUserIdAsync(client, adminToken);

        // Silinmiş (soft delete) Admin sayılmaz: ikinci Admin hesabını siler, ilk Admin yine "son Admin" kalır.
        var secondAdminToken = await isolated.GetAdminAccessTokenAsync(client, Api);
        var delete = await SendAsync(client, HttpMethod.Delete, $"{Api}/users/account", secondAdminToken,
            new { currentPassword = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var put = await SendAsync(client, HttpMethod.Put, $"{Api}/users/{adminId}/roles", adminToken,
            new { roles = new[] { RoleNames.User } });
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);

        var get = await SendAsync(client, HttpMethod.Get, $"{Api}/users/{adminId}/roles", adminToken);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(new[] { RoleNames.Admin }, (await get.ReadDataAsync<UserRolesData>()).Roles);
    }

    [Fact]
    public async Task BaskaAdminVarken_AdminRoluKaldirilabilir_SonAdminKalincaKaldirilamaz()
    {
        using var isolated = new CustomWebApplicationFactory();
        using var client = isolated.CreateClient();
        var firstAdminToken = await isolated.GetAdminAccessTokenAsync(client, Api);
        var firstAdminId = await GetCurrentUserIdAsync(client, firstAdminToken);
        var secondAdminToken = await isolated.GetAdminAccessTokenAsync(client, Api);
        var secondAdminId = await GetCurrentUserIdAsync(client, secondAdminToken);

        // İki aktif Admin varken birinin Admin rolü kaldırılabilir.
        var demote = await SendAsync(client, HttpMethod.Put, $"{Api}/users/{secondAdminId}/roles", firstAdminToken,
            new { roles = new[] { RoleNames.User } });
        Assert.Equal(HttpStatusCode.NoContent, demote.StatusCode);

        var get = await SendAsync(client, HttpMethod.Get, $"{Api}/users/{secondAdminId}/roles", firstAdminToken);
        Assert.Equal(new[] { RoleNames.User }, (await get.ReadDataAsync<UserRolesData>()).Roles);

        // Artık son Admin: kendi Admin rolünü kaldıramaz.
        var selfDemote = await SendAsync(client, HttpMethod.Put, $"{Api}/users/{firstAdminId}/roles", firstAdminToken,
            new { roles = new[] { RoleNames.User } });
        Assert.Equal(HttpStatusCode.BadRequest, selfDemote.StatusCode);
    }

    [Fact]
    public async Task AdminKaldirmaRolSatiriniKilitler_DigerRolGuncellemeleriKilitAlmaz()
    {
        // Testler tek paylaşılan SQLite bağlantısı kullandığından gerçek paralel istek deterministik değildir;
        // bunun yerine eşzamanlılığı sıraya sokan kilit ifadesinin (Admin rol satırına UPDATE) yalnızca
        // Admin kaldırılırken çalıştığı ve ardışık "birbirini düşürme" senaryosunda en az bir Admin kaldığı doğrulanır.
        using var isolated = new CustomWebApplicationFactory();
        using var client = isolated.CreateClient();
        var firstAdminToken = await isolated.GetAdminAccessTokenAsync(client, Api);
        var firstAdminId = await GetCurrentUserIdAsync(client, firstAdminToken);
        var secondAdminToken = await isolated.GetAdminAccessTokenAsync(client, Api);
        var secondAdminId = await GetCurrentUserIdAsync(client, secondAdminToken);

        // Not: TestHelpers.RegisterAndLoginAsync kendi Admin'ini oluşturduğu için burada kullanılmaz (Admin sayısını bozar).
        using var scope = isolated.Services.CreateScope();
        var customRole = $"Manager{Guid.NewGuid():N}"[..20];
        var roleCreated = await scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>()
            .CreateAsync(new AppRole { Id = Guid.CreateVersion7(), Name = customRole });
        Assert.True(roleCreated.Succeeded);
        using var recorder = new SqlCommandRecorder(scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetDbConnection());

        // Admin'e dokunmayan rol ekleme ve kaldırma kilit almaz.
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{secondAdminId}/roles", firstAdminToken,
            new { roles = new[] { RoleNames.Admin, customRole } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{secondAdminId}/roles", firstAdminToken,
            new { roles = new[] { RoleNames.Admin } })).StatusCode);
        Assert.Equal(0, recorder.CountRoleLocks());

        // İlk Admin ikinciyi düşürür: kilit alınır.
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{secondAdminId}/roles", firstAdminToken,
            new { roles = new[] { RoleNames.User } })).StatusCode);
        Assert.Equal(1, recorder.CountRoleLocks());

        // Düşürülen Admin'in eski token'ı geçersizdir; karşı hamle yapamaz.
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{firstAdminId}/roles", secondAdminToken,
            new { roles = new[] { RoleNames.User } })).StatusCode);

        // Son Admin kendi rolünü kaldıramaz; kilit sayımdan önce alınır.
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Put, $"{Api}/users/{firstAdminId}/roles", firstAdminToken,
            new { roles = new[] { RoleNames.User } })).StatusCode);
        Assert.Equal(2, recorder.CountRoleLocks());

        var get = await SendAsync(client, HttpMethod.Get, $"{Api}/users/{firstAdminId}/roles", firstAdminToken);
        Assert.Equal(new[] { RoleNames.Admin }, (await get.ReadDataAsync<UserRolesData>()).Roles);
    }

    [Fact]
    public async Task SonAktifAdmin_KendiHesabiniSilemez()
    {
        // Not: TestHelpers.RegisterAndLoginAsync kendi Admin'ini oluşturduğu için burada kullanılmaz (Admin sayısını bozar).
        using var isolated = new CustomWebApplicationFactory();
        using var client = isolated.CreateClient();
        var (adminEmail, adminToken) = await CreateUserAndLoginAsync(isolated, client, RoleNames.Admin);

        var delete = await SendAsync(client, HttpMethod.Delete, $"{Api}/users/account", adminToken,
            new { currentPassword = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
        var body = await delete.Content.ReadFromJsonAsync<ApiResponse<object>>(TestContext.Current.CancellationToken);
        Assert.Contains(body!.ErrorMessage!, m => m.Contains("Son yöneticinin hesabı silinemez"));

        // Hesap silinmedi: oturum geçerli, Admin rolü duruyor ve yeniden giriş yapılabiliyor.
        var me = await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", adminToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(new[] { RoleNames.Admin }, (await me.ReadDataAsync<CurrentUserData>()).Roles);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email = adminEmail, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task IkiAdminVarken_BiriHesabiniSiler_KalanSonAdminSilemez()
    {
        using var isolated = new CustomWebApplicationFactory();
        using var client = isolated.CreateClient();
        var (_, firstAdminToken) = await CreateUserAndLoginAsync(isolated, client, RoleNames.Admin);
        var (secondAdminEmail, secondAdminToken) = await CreateUserAndLoginAsync(isolated, client, RoleNames.Admin);

        using var scope = isolated.Services.CreateScope();
        using var recorder = new SqlCommandRecorder(scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetDbConnection());

        var firstDelete = await SendAsync(client, HttpMethod.Delete, $"{Api}/users/account", firstAdminToken,
            new { currentPassword = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(1, recorder.CountRoleLocks());

        // Silinen (soft delete) Admin sayılmaz: kalan Admin artık son Admin'dir; kilit sayımdan önce alınır.
        var secondDelete = await SendAsync(client, HttpMethod.Delete, $"{Api}/users/account", secondAdminToken,
            new { currentPassword = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.BadRequest, secondDelete.StatusCode);
        Assert.Equal(2, recorder.CountRoleLocks());

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email = secondAdminEmail, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task AdminOlmayan_HesabiniSiler_KilitAlinmaz()
    {
        using var isolated = new CustomWebApplicationFactory();
        using var client = isolated.CreateClient();
        await CreateUserAndLoginAsync(isolated, client, RoleNames.Admin);
        var (userEmail, userToken) = await CreateUserAndLoginAsync(isolated, client, RoleNames.User);

        using var scope = isolated.Services.CreateScope();
        using var recorder = new SqlCommandRecorder(scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetDbConnection());

        var delete = await SendAsync(client, HttpMethod.Delete, $"{Api}/users/account", userToken,
            new { currentPassword = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, recorder.CountRoleLocks());

        // Silinen hesap commit edildi: eski token ve eski bilgilerle giriş çalışmaz.
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", userToken)).StatusCode);
        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email = userEmail, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public void CurrentUserService_IsInRole_TokenRolunuOkur_AnonimdeFalseDoner()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var currentUser = new CurrentUserService(accessor);

        Assert.False(currentUser.IsInRole(RoleNames.Admin));

        // Kimliği doğrulanmamış principal rol claim'i taşısa bile false
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, RoleNames.Admin)]));
        Assert.False(currentUser.IsInRole(RoleNames.Admin));

        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString()), new Claim(ClaimTypes.Role, RoleNames.Admin)],
            authenticationType: "Test"));
        Assert.True(currentUser.IsInRole(RoleNames.Admin));
        Assert.False(currentUser.IsInRole(RoleNames.User));

        accessor.HttpContext = null;
        Assert.False(currentUser.IsInRole(RoleNames.Admin));
    }

    private async Task<string> CreateRoleAsync(string prefix)
    {
        var name = $"{prefix}{Guid.NewGuid():N}"[..20];
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var created = await roleManager.CreateAsync(new AppRole { Id = Guid.CreateVersion7(), Name = name });
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
        return name;
    }

    /// <summary>Verilen fabrikada tek rollü bir kullanıcı oluşturup giriş yapar (yan etki olarak başka Admin üretmez).</summary>
    private static async Task<(string Email, string AccessToken)> CreateUserAndLoginAsync(CustomWebApplicationFactory target, HttpClient client, string role)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"member-{suffix}@example.com";

        using (var scope = target.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser { Id = Guid.CreateVersion7(), UserName = $"member{suffix}", Email = email, EmailConfirmed = true };
            var created = await userManager.CreateAsync(user, TestHelpers.Password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
            Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        }

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        return (email, (await login.ReadDataAsync<TokenData>()).AccessToken);
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient client, string accessToken)
    {
        var me = await SendAsync(client, HttpMethod.Get, $"{Api}/auth/me", accessToken);
        me.EnsureSuccessStatusCode();
        return (await me.ReadDataAsync<CurrentUserData>()).Id;
    }

    /// <summary>İsteği verilen token ile (null ise anonim) gönderir; client başlıklarını değiştirmez.</summary>
    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string? accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verilen bağlantıda çalışan EF Core komutlarını DiagnosticListener ile toplar; diğer test fabrikalarının
    /// komutları bağlantı karşılaştırmasıyla ayıklanır.
    /// </summary>
    private sealed class SqlCommandRecorder : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly DbConnection _connection;
        private readonly ConcurrentQueue<string> _commands = new();
        private readonly List<IDisposable> _subscriptions = [];
        private readonly IDisposable _allListeners;

        public SqlCommandRecorder(DbConnection connection)
        {
            _connection = connection;
            _allListeners = DiagnosticListener.AllListeners.Subscribe(this);
        }

        /// <summary>Admin rol satırını kilitleyen (AspNetRoles üzerinde UPDATE) komut sayısı.</summary>
        public int CountRoleLocks() =>
            _commands.Count(c => c.Contains("UPDATE \"AspNetRoles\"", StringComparison.Ordinal));

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name != DbLoggerCategory.Name) return;
            lock (_subscriptions) _subscriptions.Add(listener.Subscribe(this));
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == RelationalEventId.CommandExecuting.Name &&
                value.Value is CommandEventData data &&
                ReferenceEquals(data.Command.Connection, _connection))
            {
                _commands.Enqueue(data.Command.CommandText);
            }
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void Dispose()
        {
            _allListeners.Dispose();
            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions) subscription.Dispose();
                _subscriptions.Clear();
            }
        }
    }
}
