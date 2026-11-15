using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Identity;

namespace ProjectApp.UnitTests;

public class TokenServiceTests
{
    [Fact]
    public void CreateAccessToken_GecerliKullaniciVeRoller_ClaimVeBitisZamaniUretir()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "ProjectApp",
            Audience = "ProjectAppUsers",
            SigningKey = new string('k', 32),
            AccessTokenMinutes = 120
        });
        var service = new TokenService(options, TimeProvider.System);
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = "admin",
            Email = "admin@admin.com",
            FullName = "Admin User"
        };

        var (accessToken, expiry) = service.CreateAccessToken(user, [RoleNames.Admin]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal("ProjectApp", jwt.Issuer);
        Assert.Equal(expiry, jwt.ValidTo, TimeSpan.FromSeconds(1));
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.Admin);
    }
}
