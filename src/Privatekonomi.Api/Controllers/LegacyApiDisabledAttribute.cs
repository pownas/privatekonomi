using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Privatekonomi.Api.Controllers;

/// <summary>Blocks legacy endpoints until their authorization and data isolation are migrated.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LegacyApiDisabledAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
{
    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
