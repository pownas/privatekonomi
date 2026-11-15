using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.Persistence;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class AccountInitializationTests
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"privatekonomi-identity-{Guid.NewGuid()}");
    private const string DemoEmail = "demo@example.com";
    private const string DemoPassword = "Development-only-password1!";

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    [DataRow("Production", true)]
    [DataRow("Staging", true)]
    [DataRow("Local", true)]
    [DataRow("Development", false)]
    public async Task DemoAccountIsNotCreatedOutsideEnabledDevelopment(string environment, bool enabled)
    {
        var configuration = Configuration(enabled: enabled);
        await using var provider = Services(configuration);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        await DevelopmentDataInitializer.InitializeAsync(context,
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), Environment(environment), configuration);

        Assert.AreEqual(0, await context.Users.CountAsync());
        Assert.AreEqual(0, await context.Transactions.CountAsync());
        Assert.IsFalse(File.Exists(Path.Combine(_directory, "identity.json")));
    }

    [TestMethod]
    public async Task DevelopmentDemoCanSignInAndInvalidCredentialsAreRejected()
    {
        var configuration = Configuration();
        await using var provider = Services(configuration);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DevelopmentDataInitializer.InitializeAsync(context, manager, Environment("Development"), configuration);
        var user = (await manager.FindByEmailAsync(DemoEmail))!;
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        Assert.IsFalse(user.IsSystemAdmin);
        Assert.IsTrue(user.EmailConfirmed);
        Assert.IsTrue((await signIn.CheckPasswordSignInAsync(user, DemoPassword, true)).Succeeded);
        Assert.IsFalse((await signIn.CheckPasswordSignInAsync(user, "invalid", true)).Succeeded);
        Assert.AreEqual(1, await context.Users.CountAsync());
        Assert.IsTrue(await context.Transactions.AnyAsync(transaction => transaction.UserId == user.Id));
    }

    [TestMethod]
    public async Task ReinitializationPreservesExistingAccountAndDoesNotAttachDemoData()
    {
        var configuration = Configuration();
        await using var provider = Services(configuration);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = DemoEmail, Email = DemoEmail, EmailConfirmed = false };
        Assert.IsTrue((await manager.CreateAsync(user, "Existing-account-password1!")).Succeeded);
        var hash = user.PasswordHash;
        var stamp = user.SecurityStamp;
        var id = user.Id;

        await DevelopmentDataInitializer.InitializeAsync(context, manager, Environment("Development"), configuration);
        await DevelopmentDataInitializer.InitializeAsync(context, manager, Environment("Development"), configuration);
        TestDataSeeder.SeedProductionReferenceData(context);
        TestDataSeeder.SeedProductionReferenceData(context);

        Assert.AreEqual(id, user.Id);
        Assert.AreEqual(hash, user.PasswordHash);
        Assert.AreEqual(stamp, user.SecurityStamp);
        Assert.IsFalse(user.IsSystemAdmin);
        Assert.IsFalse(user.EmailConfirmed);
        Assert.IsTrue(await manager.CheckPasswordAsync(user, "Existing-account-password1!"));
        Assert.IsFalse(await manager.CheckPasswordAsync(user, DemoPassword));
        Assert.AreEqual(1, await context.Users.CountAsync());
        Assert.AreEqual(0, await context.Transactions.CountAsync());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task JsonAccountsAndPermissionsSurviveRestartWithoutPeriodicSave(bool includeAttestation)
    {
        var configuration = Configuration(seed: false);
        string id;
        string? hash;
        string? stamp;
        await using (var provider = Services(configuration))
        {
            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var user = new ApplicationUser { UserName = "registered@example.com", Email = "registered@example.com", IsSystemAdmin = true };
            Assert.IsTrue((await manager.CreateAsync(user, DemoPassword)).Succeeded);
            Assert.IsTrue((await manager.AddClaimAsync(user, new Claim("permission", "manage"))).Succeeded);
            Assert.IsTrue((await manager.AddLoginAsync(user, new UserLoginInfo("test-provider", "external-id", "Test"))).Succeeded);
            Assert.IsTrue((await manager.SetAuthenticationTokenAsync(user, "test-provider", "token-name", "test-token-value")).Succeeded);
            var role = new IdentityRole("Reader") { NormalizedName = "READER" };
            context.Roles.Add(role);
            context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
            context.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "role-permission", ClaimValue = "read" });
            context.Set<IdentityUserPasskey<string>>().Add(new IdentityUserPasskey<string>
            {
                UserId = user.Id,
                CredentialId = [1, 2, 3],
                Data = new IdentityPasskeyData
                {
                    PublicKey = [4, 5, 6],
                    Name = "Existing passkey",
                    SignCount = 42,
                    Transports = ["internal"],
                    AttestationObject = includeAttestation ? [7, 8] : null!,
                    ClientDataJson = includeAttestation ? [9, 10] : null!
                }
            });
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);
            user.AccessFailedCount = 2;
            await manager.UpdateAsync(user);
            Assert.IsNotNull((await context.Set<IdentityUserPasskey<string>>().AsNoTracking().SingleAsync()).Data,
                "Passkey data must be queryable before restarting.");
            id = user.Id;
            hash = user.PasswordHash;
            stamp = user.SecurityStamp;
        }

        Assert.IsTrue(File.Exists(Path.Combine(_directory, "identity.json")));
        StringAssert.Contains(await File.ReadAllTextAsync(Path.Combine(_directory, "identity.json")), "Existing passkey");
        await using var restarted = Services(configuration);
        await using var restartedScope = restarted.CreateAsyncScope();
        var services = restartedScope.ServiceProvider;
        var restored = services.GetRequiredService<PrivatekonomyContext>();
        var persistence = services.GetRequiredService<JsonIdentityPersistenceService>();
        await persistence.LoadAsync(restored);
        await persistence.LoadAsync(restored);
        var restoredManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var restoredUser = (await restoredManager.FindByEmailAsync("registered@example.com"))!;

        Assert.AreEqual(id, restoredUser.Id);
        Assert.AreEqual(hash, restoredUser.PasswordHash);
        Assert.AreEqual(stamp, restoredUser.SecurityStamp);
        Assert.IsTrue(restoredUser.IsSystemAdmin);
        Assert.AreEqual(2, restoredUser.AccessFailedCount);
        Assert.IsTrue(await restoredManager.IsLockedOutAsync(restoredUser));
        Assert.IsTrue(await restoredManager.CheckPasswordAsync(restoredUser, DemoPassword));
        Assert.IsFalse(await restoredManager.CheckPasswordAsync(restoredUser, "invalid"));
        Assert.AreEqual("manage", (await restoredManager.GetClaimsAsync(restoredUser)).Single().Value);
        Assert.AreEqual("external-id", (await restoredManager.GetLoginsAsync(restoredUser)).Single().ProviderKey);
        Assert.AreEqual("test-token-value", await restoredManager.GetAuthenticationTokenAsync(restoredUser, "test-provider", "token-name"));
        Assert.AreEqual("Reader", (await restoredManager.GetRolesAsync(restoredUser)).Single());
        Assert.AreEqual("read", (await restored.RoleClaims.SingleAsync()).ClaimValue);
        var passkey = await restored.Set<IdentityUserPasskey<string>>().SingleAsync();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, passkey.CredentialId);
        CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, passkey.Data.PublicKey);
        Assert.AreEqual(42u, passkey.Data.SignCount);
        if (!includeAttestation)
        {
            Assert.IsNull(passkey.Data.AttestationObject);
            Assert.IsNull(passkey.Data.ClientDataJson);
        }
        Assert.AreEqual(1, await restored.Users.CountAsync());
    }

    [TestMethod]
    public async Task InvalidDemoPasswordDoesNotCreateAccountOrSampleData()
    {
        var configuration = Configuration();
        configuration["DemoUser:Password"] = "short";
        await using var provider = Services(configuration);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            DevelopmentDataInitializer.InitializeAsync(context,
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), Environment("Development"), configuration));

        Assert.AreEqual(0, await context.Users.CountAsync());
        Assert.AreEqual(0, await context.Transactions.CountAsync());
    }

    [TestMethod]
    [DataRow("{invalid")]
    [DataRow("{}")]
    public async Task CorruptIdentitySnapshotIsRejectedWithoutOverwritingIt(string contents)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "identity.json");
        await File.WriteAllTextAsync(path, contents);
        await using var provider = Services(Configuration());
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        await Assert.ThrowsExactlyAsync<JsonException>(() =>
            provider.GetRequiredService<JsonIdentityPersistenceService>().LoadAsync(context));

        Assert.AreEqual(contents, await File.ReadAllTextAsync(path));
        Assert.AreEqual(0, await context.Users.CountAsync());
    }

    [TestMethod]
    public async Task IdentityWriteFailureStopsHostAndPreservesLastDurableSnapshot()
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        await using var provider = Services(Configuration(seed: false), lifetime.Object);
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = DemoEmail, Email = DemoEmail };
        Assert.IsTrue((await manager.CreateAsync(user, DemoPassword)).Succeeded);
        var path = Path.Combine(_directory, "identity.json");
        var durableSnapshot = await File.ReadAllTextAsync(path);
        Directory.CreateDirectory(path + ".tmp");
        user.IsSystemAdmin = true;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => manager.UpdateAsync(user));

        lifetime.Verify(value => value.StopApplication(), Times.Once);
        Assert.AreEqual(durableSnapshot, await File.ReadAllTextAsync(path));
        Directory.Delete(path + ".tmp");
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => manager.UpdateAsync(user));
        Assert.AreEqual(durableSnapshot, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task SqliteSchemaUpgradeAndRestartPreserveAccountCredentialsAndPermissions()
    {
        Directory.CreateDirectory(_directory);
        var configuration = Configuration(seed: false);
        configuration["Storage:Provider"] = "Sqlite";
        configuration["Storage:ConnectionString"] = $"Data Source={Path.Combine(_directory, "accounts.db")}";
        string id;
        string? hash;
        string? stamp;
        await using (var provider = Services(configuration))
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            await context.Database.EnsureCreatedAsync();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = DemoEmail, Email = DemoEmail, IsSystemAdmin = true, EmailConfirmed = true };
            Assert.IsTrue((await manager.CreateAsync(user, DemoPassword)).Succeeded);
            Assert.IsTrue((await manager.AddClaimAsync(user, new Claim("permission", "manage"))).Succeeded);
            id = user.Id;
            hash = user.PasswordHash;
            stamp = user.SecurityStamp;
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE AspNetUsers DROP COLUMN OnboardingCompleted;
                ALTER TABLE AspNetUsers DROP COLUMN OnboardingCompletedAt;
                ALTER TABLE AspNetUsers DROP COLUMN LastLoginAt;
                DROP TABLE AspNetUserPasskeys;
                """);
        }

        await using var restarted = Services(configuration);
        await using var restartedScope = restarted.CreateAsyncScope();
        var restored = restartedScope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        await restored.Database.EnsureCreatedAsync();
        await IdentitySchemaInitializer.EnsureColumnsAsync(restored);
        await PasskeySchemaInitializer.EnsurePasskeyTableAsync(restored);
        var restoredManager = restartedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DevelopmentDataInitializer.InitializeAsync(restored, restoredManager, Environment("Development"), configuration);
        var restoredUser = (await restoredManager.FindByEmailAsync(DemoEmail))!;
        restoredUser.OnboardingCompleted = true;
        await restoredManager.UpdateAsync(restoredUser);
        await IdentitySchemaInitializer.EnsureColumnsAsync(restored);
        await PasskeySchemaInitializer.EnsurePasskeyTableAsync(restored);
        restored.ChangeTracker.Clear();
        restoredUser = (await restoredManager.FindByEmailAsync(DemoEmail))!;

        Assert.AreEqual(id, restoredUser.Id);
        Assert.AreEqual(hash, restoredUser.PasswordHash);
        Assert.AreEqual(stamp, restoredUser.SecurityStamp);
        Assert.IsTrue(restoredUser.IsSystemAdmin);
        Assert.IsTrue(restoredUser.EmailConfirmed);
        Assert.IsTrue(restoredUser.OnboardingCompleted);
        Assert.IsTrue((await restartedScope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>()
            .CheckPasswordSignInAsync(restoredUser, DemoPassword, true)).Succeeded);
        Assert.AreEqual("manage", (await restoredManager.GetClaimsAsync(restoredUser)).Single().Value);
        Assert.AreEqual(1, await restored.Users.CountAsync());
    }

    private IConfigurationRoot Configuration(bool enabled = true, bool seed = true) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "JsonFile",
            ["Storage:ConnectionString"] = _directory,
            ["Storage:SeedTestData"] = seed.ToString(),
            ["DemoUser:Enabled"] = enabled.ToString(),
            ["DemoUser:Email"] = DemoEmail,
            ["DemoUser:Password"] = DemoPassword
        }).Build();

    private static IHostEnvironment Environment(string name)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(name);
        return environment.Object;
    }

    private static ServiceProvider Services(IConfiguration configuration, IHostApplicationLifetime? lifetime = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (lifetime is not null)
            services.AddSingleton(lifetime);
        services.AddPrivatekonomyStorage(configuration);
        if (configuration["Storage:Provider"] == "JsonFile")
        {
            var databaseName = Guid.NewGuid().ToString();
            services.AddDbContextFactory<PrivatekonomyContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        services.AddAuthentication().AddIdentityCookies();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PrivatekonomyContext>()
            .AddSignInManager();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        return services.BuildServiceProvider();
    }
}
