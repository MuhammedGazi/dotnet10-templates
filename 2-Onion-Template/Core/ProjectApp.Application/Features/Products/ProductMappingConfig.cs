using ProjectApp.Application.Features.Products.Create;
using ProjectApp.Application.Features.Products.Dto;
using ProjectApp.Application.Features.Products.Update;
using ProjectApp.Domain.Entities;
using Mapster;

namespace ProjectApp.Application.Features.Products;

public class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductRequest, Product>();
        config.NewConfig<UpdateProductRequest, Product>();
        config.NewConfig<Product, ProductDto>();
    }
}
