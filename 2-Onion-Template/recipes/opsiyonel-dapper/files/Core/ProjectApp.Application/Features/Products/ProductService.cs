using System.Net;
using ProjectApp.Application.Common;
using ProjectApp.Application.Common.Parameters;
using ProjectApp.Application.Common.Wrappers;
using ProjectApp.Application.Contracts.Caching;
using ProjectApp.Application.Contracts.Infrastructure;
using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Application.Features.Products.Create;
using ProjectApp.Application.Features.Products.Dto;
using ProjectApp.Application.Features.Products.Update;
using ProjectApp.Application.Features.Products.UpdateStock;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Events;
using MapsterMapper;

namespace ProjectApp.Application.Features.Products;

public class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICacheService cacheService,
    IServiceBus serviceBus) : IProductService
{
    private const string ProductsCacheKey = CacheKeys.Products;

    public async Task<ServiceResult<List<ProductDto>>> GetAllListAsync(CancellationToken cancellationToken = default)
    {
        // GetOrCreateAsync: önbellek boşken gelen eşzamanlı isteklerde tablo yalnızca bir kez okunur.
        var productDtos = await cacheService.GetOrCreateAsync(
            ProductsCacheKey,
            async ct => mapper.Map<List<ProductDto>>(await productRepository.GetAllAsync(ct)),
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return ServiceResult<List<ProductDto>>.Success(productDtos);
    }

    public async Task<ServiceResult<List<ProductDto>>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default)
    {
        var products = await productRepository.GetTopPricedProductsWithDapperAsync(count, cancellationToken);
        return ServiceResult<List<ProductDto>>.Success(products);
    }

    public async Task<ServiceResult<PagedResult<ProductDto>>> GetPagedAllListAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var (products, totalCount) = await productRepository.GetAllPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
        var productDtos = mapper.Map<List<ProductDto>>(products);
        var pagedResult = new PagedResult<ProductDto>(productDtos, totalCount, request.PageNumber, request.PageSize);

        return ServiceResult<PagedResult<ProductDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        var productDto = mapper.Map<ProductDto>(product);
        return ServiceResult<ProductDto>.Success(productDto);
    }

    public async Task<ServiceResult<CreateProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var categoryExists = await categoryRepository.AnyAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ServiceResult<CreateProductResponse>.Fail("Belirtilen kategori bulunamadı.", HttpStatusCode.BadRequest);
        }

        var productNameExists = await productRepository.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (productNameExists)
        {
            return ServiceResult<CreateProductResponse>.Fail("Bu isimde bir ürün zaten mevcut.");
        }

        var product = mapper.Map<Product>(request);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(ProductsCacheKey, CancellationToken.None); // commit sonrası: iptal edilmemeli

        // In-Memory kuyruk üzerinden ProductAddedEvent fırlatılır.
        // Kayıt commit edildi; istek iptal edilse bile event yayımlanmalı (CancellationToken.None).
        await serviceBus.PublishAsync(new ProductAddedEvent(product.Id, product.Name, product.Price, product.CategoryId), CancellationToken.None);

        return ServiceResult<CreateProductResponse>.SuccessAsCreated(new CreateProductResponse(product.Id), $"/api/v1/products/{product.Id}");
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        var categoryExists = await categoryRepository.AnyAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ServiceResult.Fail("Belirtilen kategori bulunamadı.", HttpStatusCode.BadRequest);
        }

        var isProductNameExist = await productRepository.AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (isProductNameExist)
        {
            return ServiceResult.Fail("Bu isimde başka bir ürün zaten mevcut.");
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;

        productRepository.SetOriginalRowVersion(product, request.RowVersion);
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(ProductsCacheKey, CancellationToken.None); // commit sonrası: iptal edilmemeli

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> UpdateStockAsync(UpdateProductStockRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        product.Stock = request.Quantity;
        // Eşzamanlı iki stok yazımında son yazanın sessizce kazanmasını önler: istemcinin gördüğü sürüm esas alınır (409).
        productRepository.SetOriginalRowVersion(product, request.RowVersion);
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(ProductsCacheKey, CancellationToken.None); // commit sonrası: iptal edilmemeli

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        productRepository.Delete(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(ProductsCacheKey, CancellationToken.None); // commit sonrası: iptal edilmemeli

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
