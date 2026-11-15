using System.Net;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Products;
using ProjectApp.Services;
using ProjectApp.Services.Features.Products;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;
using ProjectApp.Services.Features.Products.UpdateStock;

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
    public async Task UpdateAsync_IstemcininRowVersionuOrijinalDegerOlarakAyarlanir()
    {
        var category = new Category { Name = "Kırtasiye" };
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category, RowVersion = rowVersion };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateAsync(product.Id, new UpdateProductRequest("Kursun Kalem", 12, 6, category.Id, rowVersion), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        var updatedProduct = await _context.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedProduct);
        Assert.Equal("Kursun Kalem", updatedProduct.Name);
    }

    [Fact]
    public async Task UpdateAsync_FarkliRowVersion_DbUpdateConcurrencyExceptionFirlatir()
    {
        var category = new Category { Name = "Kırtasiye" };
        var dbRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category, RowVersion = dbRowVersion };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        var clientRowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
        var request = new UpdateProductRequest("Kursun Kalem", 12, 6, category.Id, clientRowVersion);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.UpdateAsync(product.Id, request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateStockAsync_IstemcininRowVersionuOrijinalDegerOlarakAyarlanir()
    {
        var category = new Category { Name = "Kırtasiye" };
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category, RowVersion = rowVersion };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateStockAsync(
            new UpdateProductStockRequest(product.Id, 7, rowVersion),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        var updatedProduct = await _context.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedProduct);
        Assert.Equal(7, updatedProduct.Stock);
    }

    [Fact]
    public async Task UpdateStockAsync_FarkliRowVersion_DbUpdateConcurrencyExceptionFirlatir()
    {
        var category = new Category { Name = "Kırtasiye" };
        var dbRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category, RowVersion = dbRowVersion };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        var clientRowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
        var request = new UpdateProductStockRequest(product.Id, 7, clientRowVersion);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.UpdateStockAsync(request, TestContext.Current.CancellationToken));
    }
}
