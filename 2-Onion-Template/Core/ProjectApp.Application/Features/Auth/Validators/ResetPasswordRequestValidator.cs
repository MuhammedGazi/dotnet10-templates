using FluentValidation;
using ProjectApp.Application.Common.Validation;
using ProjectApp.Application.Features.Auth.Dtos;

namespace ProjectApp.Application.Features.Auth.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Şifre sıfırlama anahtarı (Token) zorunludur.");

        RuleFor(x => x.NewPassword)
            .StrongPassword();
    }
}
