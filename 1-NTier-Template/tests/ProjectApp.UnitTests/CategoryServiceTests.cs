using System.Net;
using Mapster;
using MapsterMapper;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Products;
using ProjectApp.Services;
using ProjectApp.Services.Features.Categories;

namespace ProjectApp.UnitTests;

public sealed class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _context = TestDb.Create();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ServiceAssembly).Assembly);

        _sut = new CategoryService(_context, new Mapper(config));
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_UrunuOlanKategori_409Doner()
    {
        var category = new Category { Name = "Kırtasiye" };
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.DeleteAsync(category.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.NotNull(await _context.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_KategoriYoksa_404Doner()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_UrunuOlmayanKategori_204DonerVeSiler()
    {
        var category = new Category { Name = "Boş" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.DeleteAsync(category.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        Assert.Null(await _context.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken));
    }
}
