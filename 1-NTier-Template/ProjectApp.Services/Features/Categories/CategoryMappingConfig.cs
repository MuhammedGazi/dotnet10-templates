using ProjectApp.Repositories.Categories;
using ProjectApp.Services.Features.Categories.Create;
using ProjectApp.Services.Features.Categories.Dto;
using ProjectApp.Services.Features.Categories.Update;
using Mapster;

namespace ProjectApp.Services.Features.Categories;

public class CategoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCategoryRequest, Category>();
        config.NewConfig<UpdateCategoryRequest, Category>();
        config.NewConfig<Category, CategoryDto>();
        config.NewConfig<Category, CategoryWithProductsDto>();
    }
}
