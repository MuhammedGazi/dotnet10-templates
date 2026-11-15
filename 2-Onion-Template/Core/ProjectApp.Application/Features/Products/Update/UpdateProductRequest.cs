namespace ProjectApp.Application.Features.Products.Update;

public record UpdateProductRequest(string Name, decimal Price, int Stock, Guid CategoryId, byte[] RowVersion);
