using ProjectApp.Infrastructure.Identity;

namespace ProjectApp.UnitTests;

public class TokenServiceTests
{
    [Fact]
    public void HashToken_AyniGirdiAyniCiktiVerir()
    {
        Assert.Equal(TokenService.HashToken("token-1"), TokenService.HashToken("token-1"));
    }

    [Fact]
    public void HashToken_FarkliGirdiFarkliCiktiVerir()
    {
        Assert.NotEqual(TokenService.HashToken("token-1"), TokenService.HashToken("token-2"));
    }

    [Fact]
    public void HashToken_64KarakterHexDoner()
    {
        var hash = TokenService.HashToken("token-1");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-fA-F]{64}$", hash);
    }
}
