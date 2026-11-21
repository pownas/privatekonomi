using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Api.Services;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.DataMigration;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = ApiAuthentication.Scheme)]
[Route("api/data-migration")]
public sealed class DataMigrationController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DataMigrationController(
        IDataMigrationService migrationService,
        UserManager<ApplicationUser> userManager)
    {
        _migrationService = migrationService;
        _userManager = userManager;
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(DataMigrationPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Preview(
        [FromBody] DataMigrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsSystemAdminAsync())
            return Forbid();

        var result = await _migrationService.PreviewAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DataMigrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DataMigrationResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Migrate(
        [FromBody] DataMigrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsSystemAdminAsync())
            return Forbid();

        var result = await _migrationService.MigrateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private async Task<bool> IsSystemAdminAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsSystemAdmin == true;
    }
}
