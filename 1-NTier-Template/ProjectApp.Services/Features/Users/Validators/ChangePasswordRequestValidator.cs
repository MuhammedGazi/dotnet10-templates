using FluentValidation;
using ProjectApp.Services.Common.Validation;
using ProjectApp.Services.Features.Users.Dtos;

namespace ProjectApp.Services.Features.Users.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre zorunludur.");

        RuleFor(x => x.NewPassword)
            .StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("Yeni şifre mevcut şifre ile aynı olamaz.");
    }
}
