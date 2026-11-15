using FluentValidation;
using ProjectApp.Application.Features.Users.Dtos;

namespace ProjectApp.Application.Features.Users.Validators;

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Hesap silme işlemi için mevcut şifrenizi girmelisiniz.");
    }
}
