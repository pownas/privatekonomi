using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Privatekonomi.Api.Models;
using Privatekonomi.Core.Configuration;

namespace Privatekonomi.Api.Controllers;

/// <summary>Exposes public API build information.</summary>
[ApiController]
[AllowAnonymous]
[Route("api-info")]
public sealed class ApiInfoController(IWebHostEnvironment environment, IOptions<StorageSettings> storageSettings) : ControllerBase
{
    /// <summary>Gets the API version and available build metadata.</summary>
    [HttpGet]
    public ActionResult<ApiInfoResponse> Get() =>
        ApiInfoResponse.FromAssembly(
            typeof(ApiInfoController).Assembly,
            environment.EnvironmentName,
            storageSettings.Value.Provider,
            HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ApiData:Profile"]);
}
