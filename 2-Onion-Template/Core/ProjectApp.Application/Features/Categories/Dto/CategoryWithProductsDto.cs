using ProjectApp.Application.Features.Products.Dto;

namespace ProjectApp.Application.Features.Categories.Dto;

public record CategoryWithProductsDto(Guid Id, string Name, List<ProductDto> Products);
