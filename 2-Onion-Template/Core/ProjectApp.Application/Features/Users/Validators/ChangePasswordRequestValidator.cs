using FluentValidation;
using ProjectApp.Application.Common.Validation;
using ProjectApp.Application.Features.Users.Dtos;

namespace ProjectApp.Application.Features.Users.Validators;

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
