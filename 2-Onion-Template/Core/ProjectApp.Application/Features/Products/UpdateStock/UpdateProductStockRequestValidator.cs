using FluentValidation;

namespace ProjectApp.Application.Features.Products.UpdateStock;

public class UpdateProductStockRequestValidator : AbstractValidator<UpdateProductStockRequest>
{
    public UpdateProductStockRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ürün kimliği boş bırakılamaz.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(0, 10_000).WithMessage("Stok miktarı 0 ile 10.000 arasında olmalıdır.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion gönderilmelidir (kaydı önce okuyup dönen değeri kullanın).");
    }
}
