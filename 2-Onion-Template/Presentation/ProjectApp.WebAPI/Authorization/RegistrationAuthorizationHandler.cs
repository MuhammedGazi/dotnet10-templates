using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ProjectApp.Application.Common.Security;

namespace ProjectApp.WebAPI.Authorization;

/// <summary>/auth/register gereksinimi (bkz. RegistrationOptions).</summary>
public sealed class RegistrationRequirement : IAuthorizationRequirement;

/// <summary>
/// Registration:AllowPublic true ise herkes (anonim dahil) geçer; false (varsayılan) ise yalnızca Admin.
/// Anonim istek reddedilirse 401, Admin olmayan oturum 403 alır.
/// </summary>
public sealed class RegistrationAuthorizationHandler(IOptions<RegistrationOptions> options)
    : AuthorizationHandler<RegistrationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RegistrationRequirement requirement)
    {
        if (options.Value.AllowPublic || context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
