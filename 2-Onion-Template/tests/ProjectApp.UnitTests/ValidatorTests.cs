using ProjectApp.Application.Features.Auth.Register;
using ProjectApp.Application.Features.Products.Create;

namespace ProjectApp.UnitTests;

public class ValidatorTests
{
    private readonly CreateProductRequestValidator _productValidator = new();
    private readonly RegisterRequestValidator _registerValidator = new();

    [Fact]
    public void CreateProduct_BosIsim_HataVerir()
    {
        var result = _productValidator.Validate(new CreateProductRequest("", 10, 5, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Name));
    }

    [Theory]
    [InlineData("100000000000000000")] // decimal(18,2) kolonunu taşırır (500 yerine doğrulama hatası olmalı)
    [InlineData("10.999")]             // 2'den fazla ondalık basamak sessizce yuvarlanmamalı
    public void CreateProduct_FiyatHassasiyetiAsilirsa_HataVerir(string price)
    {
        var result = _productValidator.Validate(new CreateProductRequest("Kalem", decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), 5, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Price));
    }

    [Theory]
    [InlineData("10.50")]
    [InlineData("9999999999999999.99")]
    public void CreateProduct_GecerliFiyatHassasiyeti_Gecer(string price)
    {
        var result = _productValidator.Validate(new CreateProductRequest("Kalem", decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), 5, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateStock_RowVersionYoksa_HataVerir()
    {
        var validator = new ProjectApp.Application.Features.Products.UpdateStock.UpdateProductStockRequestValidator();

        var result = validator.Validate(new ProjectApp.Application.Features.Products.UpdateStock.UpdateProductStockRequest(Guid.NewGuid(), 5, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "RowVersion");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateProduct_FiyatSifirVeyaAltindaysa_HataVerir(decimal price)
    {
        var result = _productValidator.Validate(new CreateProductRequest("Kalem", price, 5, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Price));
    }

    [Fact]
    public void CreateProduct_GecerliIstek_Gecer()
    {
        var result = _productValidator.Validate(new CreateProductRequest("Kalem", 10, 5, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("abc12345")]      // büyük harf ve özel karakter yok
    [InlineData("Abcdef1")]       // çok kısa ve özel karakter yok
    [InlineData("ABCDEFG1*")]     // küçük harf yok
    public void Register_ZayifSifre_HataVerir(string password)
    {
        var result = _registerValidator.Validate(new RegisterRequest("Test Kullanıcı", "testuser", "test@example.com", password));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Register_GucluSifre_Gecer()
    {
        var result = _registerValidator.Validate(new RegisterRequest("Test Kullanıcı", "testuser", "test@example.com", "Password12*"));

        Assert.True(result.IsValid);
    }
}
