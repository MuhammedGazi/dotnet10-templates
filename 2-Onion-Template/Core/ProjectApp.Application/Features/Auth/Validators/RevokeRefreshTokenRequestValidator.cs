using FluentValidation;
using ProjectApp.Application.Features.Auth.Dtos;

namespace ProjectApp.Application.Features.Auth.Validators;

public class RevokeRefreshTokenRequestValidator : AbstractValidator<RevokeRefreshTokenRequest>
{
    public RevokeRefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken boş olamaz.");
    }
}
