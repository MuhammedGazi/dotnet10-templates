using FluentValidation;
using ProjectApp.Services.Features.Users.Dtos;

namespace ProjectApp.Services.Features.Users.Validators;

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Hesap silme işlemi için mevcut şifrenizi girmelisiniz.");
    }
}
