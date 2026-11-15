using System.Net;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Products;
using ProjectApp.Services.Common.Pagination;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;
using ProjectApp.Services.Features.Products.UpdateStock;

namespace ProjectApp.Services.Features.Products;

public class ProductService(
    AppDbContext context,
    IMapper mapper) : IProductService
{
    public async Task<ServiceResult<List<ProductDto>>> GetAllListAsync(CancellationToken cancellationToken = default)
    {
        var productsAsDto = await context.Products.AsNoTracking().ProjectToType<ProductDto>().ToListAsync(cancellationToken);

        return ServiceResult<List<ProductDto>>.Success(productsAsDto);
    }

    public async Task<ServiceResult<PagedResult<ProductDto>>> GetPagedAllListAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Products.AsNoTracking().OrderBy(x => x.Created).ThenBy(x => x.Id).ProjectToType<ProductDto>(); // Guid v7 SQL Server'da kronolojik sıralanmaz
        var pagedResult = await query.ToPagedResultAsync(request, cancellationToken);

        return ServiceResult<PagedResult<ProductDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<ProductDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto?>.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        var productAsDto = mapper.Map<ProductDto>(product);
        return ServiceResult<ProductDto?>.Success(productAsDto);
    }

    public async Task<ServiceResult<CreateProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var anyProduct = await context.Products.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (anyProduct)
        {
            return ServiceResult<CreateProductResponse>.Fail("Ürün ismi veritabanında bulunmaktadır.", HttpStatusCode.BadRequest);
        }

        var categoryExists = await context.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ServiceResult<CreateProductResponse>.Fail("Belirtilen kategori bulunamadı.", HttpStatusCode.BadRequest);
        }

        var product = mapper.Map<Product>(request);
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult<CreateProductResponse>.SuccessAsCreated(
            new CreateProductResponse(product.Id),
            $"/api/products/{product.Id}");
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        var isProductNameExist = await context.Products.AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (isProductNameExist)
        {
            return ServiceResult.Fail("Ürün ismi veritabanında bulunmaktadır.", HttpStatusCode.BadRequest);
        }

        var categoryExists = await context.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ServiceResult.Fail("Belirtilen kategori bulunamadı.", HttpStatusCode.BadRequest);
        }

        product = mapper.Map(request, product);
        context.Entry(product).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> UpdateStockAsync(UpdateProductStockRequest request, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([request.ProductId], cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        product.Stock = request.Quantity;
        // Eşzamanlı iki stok yazımında son yazanın sessizce kazanmasını önler: istemcinin gördüğü sürüm esas alınır (409).
        context.Entry(product).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return ServiceResult.Fail("Ürün bulunamadı.", HttpStatusCode.NotFound);
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
