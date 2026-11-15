using FluentValidation;

namespace ProjectApp.Services.Features.Categories.Update;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori ismi boş olamaz.")
            .Length(3, 100).WithMessage("Kategori adı 3 ile 100 karakter arasında olmalıdır.");
    }
}
