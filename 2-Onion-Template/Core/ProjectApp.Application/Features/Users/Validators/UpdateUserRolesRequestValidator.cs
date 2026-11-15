using FluentValidation;
using ProjectApp.Application.Features.Users.Dtos;

namespace ProjectApp.Application.Features.Users.Validators;

public class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("En az bir rol belirtilmelidir.")
            .Must(roles => roles is null || roles.Count <= 20).WithMessage("En fazla 20 rol belirtilebilir.");

        RuleForEach(x => x.Roles)
            .NotEmpty().WithMessage("Rol adı boş olamaz.")
            .MaximumLength(256).WithMessage("Rol adı en fazla 256 karakter olabilir.");
    }
}
