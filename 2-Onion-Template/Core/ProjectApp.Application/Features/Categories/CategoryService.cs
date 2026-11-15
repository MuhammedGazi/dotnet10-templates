using System.Net;
using ProjectApp.Application.Common.Wrappers;
using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Application.Features.Categories.Create;
using ProjectApp.Application.Features.Categories.Dto;
using ProjectApp.Application.Features.Categories.Update;
using ProjectApp.Domain.Entities;
using MapsterMapper;

namespace ProjectApp.Application.Features.Categories;

public class CategoryService(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ICategoryService
{
    public async Task<ServiceResult<List<CategoryDto>>> GetAllListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        var categoryDtos = mapper.Map<List<CategoryDto>>(categories);
        return ServiceResult<List<CategoryDto>>.Success(categoryDtos);
    }

    public async Task<ServiceResult<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var categoryDto = mapper.Map<CategoryDto>(category);
        return ServiceResult<CategoryDto>.Success(categoryDto);
    }

    public async Task<ServiceResult<CategoryWithProductsDto>> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetCategoryWithProductsAsync(id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryWithProductsDto>.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var categoryWithProductsDto = mapper.Map<CategoryWithProductsDto>(category);
        return ServiceResult<CategoryWithProductsDto>.Success(categoryWithProductsDto);
    }

    public async Task<ServiceResult<CreateCategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var anyCategory = await categoryRepository.NameExistsAsync(request.Name, cancellationToken: cancellationToken);
        if (anyCategory)
        {
            return ServiceResult<CreateCategoryResponse>.Fail("Bu isimde bir kategori zaten mevcut.");
        }

        var category = mapper.Map<Category>(request);

        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<CreateCategoryResponse>.SuccessAsCreated(new CreateCategoryResponse(category.Id), $"/api/categories/{category.Id}");
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return ServiceResult.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var isCategoryNameExist = await categoryRepository.NameExistsAsync(request.Name, id, cancellationToken);
        if (isCategoryNameExist)
        {
            return ServiceResult.Fail("Bu isimde başka bir kategori zaten mevcut.");
        }

        category.Name = request.Name;
        categoryRepository.SetOriginalRowVersion(category, request.RowVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return ServiceResult.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var hasProducts = await productRepository.AnyInCategoryAsync(id, cancellationToken);
        if (hasProducts)
        {
            return ServiceResult.Fail("Kategoriye bağlı ürünler varken kategori silinemez.", HttpStatusCode.Conflict);
        }

        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
