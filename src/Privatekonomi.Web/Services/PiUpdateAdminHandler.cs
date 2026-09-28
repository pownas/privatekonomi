using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Web.Services;

public sealed class PiUpdateAdminRequirement : IAuthorizationRequirement;

public sealed class PiUpdateAdminHandler(UserManager<ApplicationUser> userManager)
    : AuthorizationHandler<PiUpdateAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PiUpdateAdminRequirement requirement)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated != true || id is null)
            return;

        var user = await userManager.FindByIdAsync(id);
        if (user?.IsSystemAdmin == true)
            context.Succeed(requirement);
    }
}
