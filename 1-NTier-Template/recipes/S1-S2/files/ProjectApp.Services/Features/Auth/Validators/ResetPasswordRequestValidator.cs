using ProjectApp.Services.Features.Auth.Dtos;
using FluentValidation;
using ProjectApp.Services.Common.Validation;

namespace ProjectApp.Services.Features.Auth.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Şifre sıfırlama anahtarı (token) boş olamaz.");

        RuleFor(x => x.NewPassword)
            .StrongPassword();
    }
}
