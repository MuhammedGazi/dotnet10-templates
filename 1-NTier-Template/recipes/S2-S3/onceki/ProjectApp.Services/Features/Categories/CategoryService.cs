using System.Net;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Repositories;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Products;
using ProjectApp.Services.Features.Categories.Create;
using ProjectApp.Services.Features.Categories.Dto;
using ProjectApp.Services.Features.Categories.Update;

namespace ProjectApp.Services.Features.Categories;

public class CategoryService(
    AppDbContext context,
    IMapper mapper) : ICategoryService
{
    public async Task<ServiceResult<List<CategoryDto>>> GetAllListAsync(CancellationToken cancellationToken = default)
    {
        var categoriesAsDto = await context.Categories.AsNoTracking().ProjectToType<CategoryDto>().ToListAsync(cancellationToken);

        return ServiceResult<List<CategoryDto>>.Success(categoriesAsDto);
    }

    public async Task<ServiceResult<CategoryDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryDto?>.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var categoryAsDto = mapper.Map<CategoryDto>(category);
        return ServiceResult<CategoryDto?>.Success(categoryAsDto);
    }

    public async Task<ServiceResult<CategoryWithProductsDto?>> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.AsNoTracking().Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryWithProductsDto?>.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var categoryAsDto = mapper.Map<CategoryWithProductsDto>(category);
        return ServiceResult<CategoryWithProductsDto?>.Success(categoryAsDto);
    }

    public async Task<ServiceResult<CreateCategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var anyCategory = await context.Categories.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (anyCategory)
        {
            return ServiceResult<CreateCategoryResponse>.Fail("Kategori ismi veritabanında bulunmaktadır.", HttpStatusCode.BadRequest);
        }

        var category = mapper.Map<Category>(request);
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult<CreateCategoryResponse>.SuccessAsCreated(
            new CreateCategoryResponse(category.Id),
            $"/api/categories/{category.Id}");
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return ServiceResult.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var isCategoryNameExist = await context.Categories.AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);
        if (isCategoryNameExist)
        {
            return ServiceResult.Fail("Kategori ismi veritabanında bulunmaktadır.", HttpStatusCode.BadRequest);
        }

        category = mapper.Map(request, category);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return ServiceResult.Fail("Kategori bulunamadı.", HttpStatusCode.NotFound);
        }

        var hasProducts = await context.Products.AnyAsync(x => x.CategoryId == id, cancellationToken);
        if (hasProducts)
        {
            return ServiceResult.Fail("Kategoriye bağlı ürünler varken kategori silinemez.", HttpStatusCode.Conflict);
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
