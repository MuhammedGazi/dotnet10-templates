using ProjectApp.Application.Common.Parameters;
using ProjectApp.Application.Common.Wrappers;
using ProjectApp.Application.Features.Products.Create;
using ProjectApp.Application.Features.Products.Dto;
using ProjectApp.Application.Features.Products.Update;
using ProjectApp.Application.Features.Products.UpdateStock;

namespace ProjectApp.Application.Features.Products;

public interface IProductService
{
    Task<ServiceResult<List<ProductDto>>> GetAllListAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<List<ProductDto>>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<ProductDto>>> GetPagedAllListAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<CreateProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateStockAsync(UpdateProductStockRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
