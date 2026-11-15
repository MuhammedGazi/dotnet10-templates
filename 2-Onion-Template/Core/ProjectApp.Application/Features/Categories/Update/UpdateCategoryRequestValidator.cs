using FluentValidation;

namespace ProjectApp.Application.Features.Categories.Update;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı boş bırakılamaz.")
            .Length(3, 100).WithMessage("Kategori adı 3 ile 100 karakter arasında olmalıdır.");

        // İyimser eşzamanlılık: istemci GET ile aldığı RowVersion'ı geri gönderir; kayıt bu arada değiştiyse 409 döner.
        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion gönderilmelidir (kaydı önce okuyup dönen değeri kullanın).");
    }
}
