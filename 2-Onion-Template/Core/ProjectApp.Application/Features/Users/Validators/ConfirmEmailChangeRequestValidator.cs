using FluentValidation;
using ProjectApp.Application.Features.Users.Dtos;

namespace ProjectApp.Application.Features.Users.Validators;

public class ConfirmEmailChangeRequestValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Yeni e-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Doğrulama anahtarı (token) zorunludur.");
    }
}
