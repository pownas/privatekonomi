using Microsoft.AspNetCore.Identity;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Web.Services;

/// <summary>
/// Development-only implementation of ICurrentUserService that bypasses authentication.
/// This service returns the test user ID when the DevDisableAuth feature flag is enabled.
/// WARNING: This should ONLY be used in Development environment for testing purposes.
/// </summary>
public class DevCurrentUserService : ICurrentUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private string? _cachedUserId;

    public DevCurrentUserService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the test user ID from the seeded test data.
    /// This matches the account configured in DemoUser:Email.
    /// </summary>
    public string? UserId
    {
        get
        {
            if (_cachedUserId == null)
            {
                // Get the test user synchronously (not ideal but necessary for property)
                var email = _configuration["DemoUser:Email"];
                var testUser = string.IsNullOrWhiteSpace(email) ? null
                    : _userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
                _cachedUserId = testUser?.Id;
            }
            return _cachedUserId;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
