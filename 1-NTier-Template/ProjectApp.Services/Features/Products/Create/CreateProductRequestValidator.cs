using FluentValidation;

namespace ProjectApp.Services.Features.Products.Create;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün ismi boş olamaz.")
            .Length(3, 150).WithMessage("Ürün ismi 3 ile 150 karakter arasında olmalıdır.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.")
            // Kolon decimal(18,2): taşan veya 2'den fazla ondalıklı değer 500 / sessiz yuvarlama yerine 400 döner.
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Ürün fiyatı en fazla 16 tam ve 2 ondalık basamak içerebilir.");

        RuleFor(x => x.Stock)
            .InclusiveBetween(0, 10_000).WithMessage("Stok miktarı 0 ile 10.000 arasında olmalıdır.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmelidir.");
    }
}
