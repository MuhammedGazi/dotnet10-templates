using System.Net;
using Mapster;
using MapsterMapper;
using NSubstitute;
using ProjectApp.Application;
using ProjectApp.Application.Contracts.Caching;
using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Application.Features.Categories;
using ProjectApp.Domain.Entities;

namespace ProjectApp.UnitTests;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ServiceRegistration).Assembly);

        _sut = new CategoryService(_categoryRepository, _productRepository, _cacheService, _unitOfWork, new Mapper(config));
    }

    [Fact]
    public async Task DeleteAsync_UrunuOlanKategori_409Doner()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new Category { Name = "Kırtasiye" });
        _productRepository.AnyInCategoryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        _categoryRepository.DidNotReceive().Remove(Arg.Any<Category>());
    }

    [Fact]
    public async Task DeleteAsync_KategoriYoksa_404Doner()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_UrunuOlmayanKategori_204DonerVeSiler()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new Category { Name = "Boş" });
        _productRepository.AnyInCategoryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        _categoryRepository.Received(1).Remove(Arg.Any<Category>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
