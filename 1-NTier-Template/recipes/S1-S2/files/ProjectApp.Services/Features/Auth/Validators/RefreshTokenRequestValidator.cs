using ProjectApp.Services.Features.Auth.Dtos;
using FluentValidation;

namespace ProjectApp.Services.Features.Auth.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("AccessToken boş olamaz.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken boş olamaz.");
    }
}
