using System.Net;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Products;
using ProjectApp.Services.Common.Pagination;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;
using ProjectApp.Services.Features.Products.UpdateStock;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Services.Features.Products;

public class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IProductService
{
    public async Task<ServiceResult<List<ProductDto>>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default)
    {
        var products = await productRepository.GetTopPricedProductsWithDapperAsync(count, cancellationToken);
        var productsAsDto = mapper.Map<List<ProductDto>>(products);

        return ServiceResult<List<ProductDto>>.Success(productsAsDto);
    }

    public async Task<ServiceResult<List<ProductDto>>> GetAllListAsync(CancellationToken cancellationToken = default)
    {
        var productsAsDto = await productRepository.GetAll().ProjectToType<ProductDto>().ToListAsync(cancellationToken);

        return ServiceResult<List<ProductDto>>.Success(productsAsDto);
    }

    public async Task<ServiceResult<PagedResult<ProductDto>>> GetPagedAllListAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = productRepository.GetAll().OrderBy(x => x.Created).ThenBy(x => x.Id).ProjectToType<ProductDto>(); // Guid v7 SQL Server'da kronolojik sıralanmaz
        var pagedResult = await query.ToPagedResultAsync(request, cancellationToken);

        return ServiceResult<PagedResult<ProductDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<ProductDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto?>.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        var productAsDto = mapper.Map<ProductDto>(product);
        return ServiceResult<ProductDto?>.Success(productAsDto);
    }

    public async Task<ServiceResult<CreateProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var anyProduct = await productRepository.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (anyProduct)
        {
            return ServiceResult<CreateProductResponse>.Fail("Ürün ismi veritabanında bulunmaktadır.", HttpStatusCode.BadRequest);
        }

        var categoryExists = await categoryRepository.AnyAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ServiceResult<CreateProductResponse>.Fail("Belirtilen kategori bulunamadı.", HttpStatusCode.BadRequest);
        }

        var product = mapper.Map<Product>(request);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<CreateProductResponse>.SuccessAsCreated(
            new CreateProductResponse(product.Id),
            $"/api/products/{product.Id}");
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        var isProductNameExist = await productRepository.AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (isProductNameExist)
        {
            return ServiceResult.Fail("Ürün ismi veritabanında bulunmaktadır.", HttpStatusCode.BadRequest);
        }

        var categoryExists = await categoryRepository.AnyAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ServiceResult.Fail("Belirtilen kategori bulunamadı.", HttpStatusCode.BadRequest);
        }

        product = mapper.Map(request, product);
        productRepository.SetOriginalRowVersion(product, request.RowVersion);
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
