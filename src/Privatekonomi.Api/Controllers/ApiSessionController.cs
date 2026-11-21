using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Privatekonomi.Api.Models;
using Privatekonomi.Core.Models;
using Privatekonomi.Api.Services;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/session")]
public sealed class ApiSessionController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("api-login")]
    public async Task<IActionResult> Login(ApiLoginRequest request)
    {
        if (!Request.IsHttps)
            return BadRequest();

        var user = await users.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        // Password-only API login must not bypass accounts configured for two-factor authentication.
        if (!result.Succeeded || await users.GetTwoFactorEnabledAsync(user))
            return Unauthorized();

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        await HttpContext.SignInAsync(ApiAuthentication.Scheme, principal);
        return new EmptyResult();
    }
}
