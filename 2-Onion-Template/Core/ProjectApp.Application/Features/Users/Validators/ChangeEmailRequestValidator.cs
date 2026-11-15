using FluentValidation;
using ProjectApp.Application.Features.Users.Dtos;

namespace ProjectApp.Application.Features.Users.Validators;

public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre zorunludur.");

        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Yeni e-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}
