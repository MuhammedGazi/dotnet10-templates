using System.ComponentModel.DataAnnotations;
using ProjectApp.Services.Identity;

namespace ProjectApp.UnitTests;

public class JwtOptionsValidationTests
{
    private static JwtOptions Valid() => new()
    {
        Issuer = "issuer",
        Audience = "audience",
        SigningKey = new string('k', 32),
        AccessTokenMinutes = 120
    };

    private static List<ValidationResult> Validate(JwtOptions options) =>
        options.Validate(new ValidationContext(options)).ToList();

    [Fact]
    public void GecerliAyarlar_HataUretmez()
    {
        Assert.Empty(Validate(Valid()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SifirVeyaNegatifSure_HataUretir(int accessTokenMinutes)
    {
        var options = Valid();
        options.AccessTokenMinutes = accessTokenMinutes;

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
