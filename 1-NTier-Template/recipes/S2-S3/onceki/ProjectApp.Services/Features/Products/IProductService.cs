using ProjectApp.Services.Common.Pagination;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;

namespace ProjectApp.Services.Features.Products;

public interface IProductService
{
    Task<ServiceResult<ProductDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<ProductDto>>> GetAllListAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<ProductDto>>> GetPagedAllListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CreateProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
