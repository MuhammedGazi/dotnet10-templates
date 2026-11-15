namespace ProjectApp.Services.Features.Products.Create;

public record CreateProductRequest(string Name, decimal Price, int Stock, Guid CategoryId);

public record CreateProductResponse(Guid Id);
