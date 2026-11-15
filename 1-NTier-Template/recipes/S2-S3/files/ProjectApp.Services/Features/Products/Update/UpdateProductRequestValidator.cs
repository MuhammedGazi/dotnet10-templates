using FluentValidation;

namespace ProjectApp.Services.Features.Products.Update;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün ismi boş olamaz.")
            .Length(3, 150).WithMessage("Ürün ismi 3 ile 150 karakter arasında olmalıdır.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Ürün fiyatı en fazla 16 tam ve 2 ondalık basamak içerebilir.");

        RuleFor(x => x.Stock)
            .InclusiveBetween(0, 10_000).WithMessage("Stok miktarı 0 ile 10.000 arasında olmalıdır.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmelidir.");

        // İyimser eşzamanlılık: istemci GET ile aldığı RowVersion'ı geri gönderir; kayıt bu arada değiştiyse 409 döner.
        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion gönderilmelidir (kaydı önce okuyup dönen değeri kullanın).");
    }
}
