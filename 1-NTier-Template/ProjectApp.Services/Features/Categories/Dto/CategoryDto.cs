using ProjectApp.Services.Features.Products;

namespace ProjectApp.Services.Features.Categories.Dto;

public record CategoryDto(Guid Id, string Name);

public record CategoryWithProductsDto(Guid Id, string Name, List<ProductDto> Products);
