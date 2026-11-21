using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Services.DataMigration;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/data-migration")]
public sealed class DataMigrationController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;

    public DataMigrationController(IDataMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DataMigrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DataMigrationResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Migrate(
        [FromBody] DataMigrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _migrationService.MigrateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
