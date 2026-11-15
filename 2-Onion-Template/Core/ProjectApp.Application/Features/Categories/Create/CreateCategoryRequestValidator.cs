using FluentValidation;

namespace ProjectApp.Application.Features.Categories.Create;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı boş bırakılamaz.")
            .Length(3, 100).WithMessage("Kategori adı 3 ile 100 karakter arasında olmalıdır.");
    }
}
