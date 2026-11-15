using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Creates the configured demo account and optional sample data only in Development.
/// </summary>
public static class DevelopmentDataInitializer
{
    /// <summary>
    /// Initializes a new demo account without changing an existing account's credentials or permissions.
    /// </summary>
    public static async Task InitializeAsync(
        PrivatekonomyContext context,
        UserManager<ApplicationUser> userManager,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DemoUser:Enabled"))
            return;

        var email = configuration["DemoUser:Email"]?.Trim();
        var password = configuration["DemoUser:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("DemoUser:Email and DemoUser:Password are required in Development.");

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
            return;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Demo",
            LastName = "Användare",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Demo account creation failed: {string.Join(", ", result.Errors.Select(error => error.Code))}");

        if (configuration.GetValue<bool>("Storage:SeedTestData"))
            await TestDataSeeder.SeedTestDataAsync(context, user.Id);
    }
}
