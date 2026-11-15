using ProjectApp.Services.Features.Auth.Dtos;
using FluentValidation;

namespace ProjectApp.Services.Features.Auth.Validators;

public class RevokeRefreshTokenRequestValidator : AbstractValidator<RevokeRefreshTokenRequest>
{
    public RevokeRefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken boş olamaz.");
    }
}
