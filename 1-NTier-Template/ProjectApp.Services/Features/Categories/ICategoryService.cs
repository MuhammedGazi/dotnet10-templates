using ProjectApp.Services.Features.Categories.Create;
using ProjectApp.Services.Features.Categories.Dto;
using ProjectApp.Services.Features.Categories.Update;

namespace ProjectApp.Services.Features.Categories;

public interface ICategoryService
{
    Task<ServiceResult<List<CategoryDto>>> GetAllListAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryWithProductsDto?>> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<CreateCategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
