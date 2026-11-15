namespace ProjectApp.Services.Features.Products;

public record ProductDto(Guid Id, string Name, decimal Price, int Stock, Guid CategoryId);
