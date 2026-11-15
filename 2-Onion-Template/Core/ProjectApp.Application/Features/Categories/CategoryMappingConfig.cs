using ProjectApp.Application.Features.Categories.Create;
using ProjectApp.Application.Features.Categories.Dto;
using ProjectApp.Application.Features.Categories.Update;
using ProjectApp.Domain.Entities;
using Mapster;

namespace ProjectApp.Application.Features.Categories;

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
