namespace ProjectApp.Application.Features.Categories.Dto;

public record CategoryDto(Guid Id, string Name, DateTime Created, byte[] RowVersion);
