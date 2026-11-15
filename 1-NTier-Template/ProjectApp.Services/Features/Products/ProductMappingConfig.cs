using ProjectApp.Repositories.Products;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;
using Mapster;

namespace ProjectApp.Services.Features.Products;

public class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductRequest, Product>();
        config.NewConfig<UpdateProductRequest, Product>();
        config.NewConfig<Product, ProductDto>();
    }
}
