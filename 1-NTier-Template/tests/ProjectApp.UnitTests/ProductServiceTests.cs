using System.Net;
using Mapster;
using MapsterMapper;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Products;
using ProjectApp.Services;
using ProjectApp.Services.Features.Products;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;

namespace ProjectApp.UnitTests;

public sealed class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _context = TestDb.Create();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ServiceAssembly).Assembly);

        _sut = new ProductService(_context, new Mapper(config));
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_KategoriYoksa_400Doner()
    {
        var result = await _sut.CreateAsync(new CreateProductRequest("Kalem", 10, 5, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Empty(_context.Products);
    }

    [Fact]
    public async Task CreateAsync_IsimZatenVarsa_400Doner()
    {
        var category = new Category { Name = "Kırtasiye" };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.CreateAsync(new CreateProductRequest("Kalem", 10, 5, category.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
    }

    [Fact]
    public async Task CreateAsync_Basarili_201DonerVeKaydeder()
    {
        var category = new Category { Name = "Kırtasiye" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.CreateAsync(new CreateProductRequest("Kalem", 10, 5, category.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.Status);
        Assert.Single(_context.Products);
    }

    [Fact]
    public async Task UpdateAsync_Basarili_204DonerVeGunceller()
    {
        var category = new Category { Name = "Kırtasiye" };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateAsync(product.Id, new UpdateProductRequest("Kursun Kalem", 12, 6, category.Id), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        var updatedProduct = await _context.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedProduct);
        Assert.Equal("Kursun Kalem", updatedProduct.Name);
    }
}
