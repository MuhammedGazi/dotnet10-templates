using System.Net;
using Mapster;
using MapsterMapper;
using NSubstitute;
using ProjectApp.Application;
using ProjectApp.Application.Contracts.Infrastructure;
using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Application.Features.Products;
using ProjectApp.Application.Features.Products.Create;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Events;
using ProjectApp.Application.Features.Products.Update;

namespace ProjectApp.UnitTests;

public class ProductServiceTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IServiceBus _serviceBus = Substitute.For<IServiceBus>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ServiceRegistration).Assembly);

        _sut = new ProductService(_productRepository, _categoryRepository, _unitOfWork, new Mapper(config), _serviceBus);
    }

    [Fact]
    public async Task CreateAsync_KategoriYoksa_400Doner()
    {
        _categoryRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(new CreateProductRequest("Kalem", 10, 5, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_IsimZatenVarsa_400Doner()
    {
        _categoryRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _productRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateProductRequest("Kalem", 10, 5, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        _productRepository.DidNotReceive().Add(Arg.Any<Product>());
    }

    [Fact]
    public async Task CreateAsync_Basarili_201DonerKaydeder()
    {
        _categoryRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _productRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(new CreateProductRequest("Kalem", 10, 5, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.Status);
        _productRepository.Received(1).Add(Arg.Any<Product>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _serviceBus.Received(1).PublishAsync(Arg.Any<ProductAddedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_IstemcininRowVersionuOrijinalDegerOlarakAyarlanir()
    {
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, CategoryId = Guid.NewGuid() };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _productRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _categoryRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var result = await _sut.UpdateAsync(product.Id, new UpdateProductRequest("Kursun Kalem", 12, 6, product.CategoryId, rowVersion), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        _productRepository.Received(1).SetOriginalRowVersion(product, rowVersion);
    }

    [Fact]
    public async Task UpdateStockAsync_IstemcininRowVersionuOrijinalDegerOlarakAyarlanir()
    {
        // Eşzamanlı stok yazımında kayıp güncellemeyi önlemek için istemcinin gördüğü sürüm esas alınmalı (aksi halde 409 yerine son yazan kazanır).
        var product = new Product { Name = "Kalem", Price = 10, Stock = 5, CategoryId = Guid.NewGuid() };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var rowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        var result = await _sut.UpdateStockAsync(
            new ProjectApp.Application.Features.Products.UpdateStock.UpdateProductStockRequest(product.Id, 7, rowVersion),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        Assert.Equal(7, product.Stock);
        _productRepository.Received(1).SetOriginalRowVersion(product, rowVersion);
    }
}
