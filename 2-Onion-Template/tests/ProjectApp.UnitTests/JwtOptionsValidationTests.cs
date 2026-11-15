using System.ComponentModel.DataAnnotations;
using ProjectApp.Infrastructure.Identity;

namespace ProjectApp.UnitTests;

public class JwtOptionsValidationTests
{
    private static JwtOption Valid() => new()
    {
        Issuer = "issuer",
        Audience = "audience",
        SigningKey = new string('k', 32),
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private static List<ValidationResult> Validate(JwtOption options) =>
        options.Validate(new ValidationContext(options)).ToList();

    [Fact]
    public void GecerliAyarlar_HataUretmez()
    {
        Assert.Empty(Validate(Valid()));
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(-5, 7)]
    [InlineData(15, 0)]
    public void SifirVeyaNegatifSure_HataUretir(int accessTokenMinutes, int refreshTokenDays)
    {
        var options = Valid();
        options.AccessTokenMinutes = accessTokenMinutes;
        options.RefreshTokenDays = refreshTokenDays;

        Assert.NotEmpty(Validate(options));
    }

    [Fact]
    public void KisaSigningKey_HataUretir()
    {
        var options = Valid();
        options.SigningKey = "kisa";

        Assert.NotEmpty(Validate(options));
    }
}
