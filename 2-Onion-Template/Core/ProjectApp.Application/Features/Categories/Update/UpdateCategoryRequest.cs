namespace ProjectApp.Application.Features.Categories.Update;

public record UpdateCategoryRequest(string Name, byte[] RowVersion);
