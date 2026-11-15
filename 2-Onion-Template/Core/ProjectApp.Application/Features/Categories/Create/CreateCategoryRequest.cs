namespace ProjectApp.Application.Features.Categories.Create;

public record CreateCategoryRequest(string Name);

public record CreateCategoryResponse(Guid Id);
