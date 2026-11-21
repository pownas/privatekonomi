using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Services;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class StartupDataIntegrationTests
{
    private const string Email = "demo@example.com";
    private const string Password = "Development-only-password1!";

    [TestMethod]
    [DataRow("Production", true, true, false, false)]
    [DataRow("Staging", true, true, false, false)]
    [DataRow("Development", false, true, false, false)]
    [DataRow("Development", true, false, true, false)]
    [DataRow("Development", true, true, true, true)]
    public async Task StartupSeedsOnlyExplicitlyEnabledDevelopmentData(
        string environment, bool enabled, bool seed, bool hasUser, bool hasTransactions)
    {
        await using var factory = new StartupFactory(null, environment, enabled, seed);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.IsTrue(await context.ChallengeTemplates.AnyAsync());
        var user = await users.FindByEmailAsync(Email);
        Assert.AreEqual(hasUser, user is not null);
        Assert.AreEqual(hasTransactions, await context.Transactions.AnyAsync());
        if (user is not null)
        {
            Assert.IsFalse(user.IsSystemAdmin);
            Assert.IsTrue(await users.CheckPasswordAsync(user, Password));
            Assert.IsFalse(await context.Transactions.AnyAsync(t => t.UserId != user.Id));
        }
    }

    [TestMethod]
    public async Task DemoTransactionsRemainPrivate()
    {
        await using (var factory = new StartupFactory(null, "Development", true, true))
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var demo = (await users.FindByEmailAsync(Email))!;
            Assert.IsTrue(await context.Transactions.AnyAsync(t => t.UserId == demo.Id));
            var bankIds = await context.BankSources.Where(b => b.UserId == demo.Id).Select(b => b.BankSourceId).ToListAsync();
            var accountIds = await context.InvestmentAccounts.Where(a => a.UserId == demo.Id).Select(a => a.InvestmentAccountId).ToListAsync();
            var investments = await context.Investments.Where(i => i.UserId == demo.Id).ToListAsync();
            Assert.IsTrue(investments.Count > 0);
            Assert.IsTrue(investments.All(i => i.BankSourceId.HasValue && bankIds.Contains(i.BankSourceId.Value)
                && i.InvestmentAccountId.HasValue && accountIds.Contains(i.InvestmentAccountId.Value)));

            var other = new ApplicationUser { UserName = "other@example.com", Email = "other@example.com", EmailConfirmed = true };
            Assert.IsTrue((await users.CreateAsync(other, Password)).Succeeded);
            var login = await client.PostAsJsonAsync("/api/session/login", new ApiLoginRequest { Email = other.Email, Password = Password });
            Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
            var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
            var page = await client.GetFromJsonAsync<PersonalTransactionPage>("/api/personal/transactions");
            Assert.IsNotNull(page);
            Assert.AreEqual(0, page.Transactions.Count);
        }
    }

    [TestMethod]
    public async Task SqliteRestartPreservesReferenceDataAndAccount()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        string? demoHash;
        int templates;
        await using (var factory = new StartupFactory(connection, "Development", true, false))
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            demoHash = (await users.FindByEmailAsync(Email))!.PasswordHash;
            templates = await context.ChallengeTemplates.CountAsync();
            Assert.IsTrue(templates > 0);
        }
        await using (var factory = new StartupFactory(connection, "Development", true, false))
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.AreEqual(demoHash, (await users.FindByEmailAsync(Email))!.PasswordHash);
            Assert.AreEqual(templates, await context.ChallengeTemplates.CountAsync());
            Assert.AreEqual(0, await context.Transactions.CountAsync());
        }
    }

    [TestMethod]
    public async Task SqliteDemoTestDataIsRejectedBeforeCreatingDemoUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var factory = new StartupFactory(connection, "Development", true, true);
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateClient());
        StringAssert.Contains(exception.Message, "isolated InMemory");
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM AspNetUsers";
        Assert.AreEqual(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    [TestMethod]
    public async Task ExistingSqliteDataIsNotContaminatedByDemoTestData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var context = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseSqlite(connection).Options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Users.Add(new ApplicationUser { Id = "existing", UserName = "existing@example.com", Email = "existing@example.com" });
            context.Transactions.Add(new Transaction { UserId = "existing", Description = "Existing", Amount = 123, Date = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        await using var factory = new StartupFactory(connection, "Development", true, true);
        Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateClient());
        await using var verify = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseSqlite(connection).Options);
        Assert.AreEqual(1, await verify.Users.CountAsync());
        var transaction = await verify.Transactions.SingleAsync();
        Assert.AreEqual("existing", transaction.UserId);
        Assert.AreEqual(123m, transaction.Amount);
    }

    [TestMethod]
    public void ServerDatabasesNeverSeedReferenceOrEnabledDevelopmentDemo()
    {
        using var context = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseSqlServer("Server=localhost;Database=not-connected;User Id=unused;******;TrustServerCertificate=True").Options);
        Assert.IsFalse(ApiSeedPolicy.CanSeedReferenceData(context));
        ApiSeedPolicy.ValidateDemoInitialization(context, false, true);
        ApiSeedPolicy.ValidateDemoInitialization(context, true, false);
        Assert.ThrowsExactly<InvalidOperationException>(() => ApiSeedPolicy.ValidateDemoInitialization(context, true, true));
    }

    [TestMethod]
    public void TestDataRequiresNewNonPersistentInMemoryDatabase()
    {
        using var context = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        ApiSeedPolicy.ValidateTestDataInitialization(context, "InMemory", true, true, true, true);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ApiSeedPolicy.ValidateTestDataInitialization(context, "InMemory", true, true, true, false));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ApiSeedPolicy.ValidateTestDataInitialization(context, "JsonFile", true, true, true, true));
    }

    private sealed class TokenResponse
    {
        public string AccessToken { get; set; } = "";
    }

    private sealed class StartupFactory(SqliteConnection? connection, string environment, bool enabled, bool seed)
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ApiData:Profile"] = connection is null ? "Mock" : "Sqlite",
                    ["ApiData:SqliteConnectionString"] = "Data Source=:memory:",
                    ["Storage:SeedTestData"] = seed.ToString(),
                    ["DemoUser:Enabled"] = enabled.ToString(),
                    ["DemoUser:Email"] = Email,
                    ["DemoUser:Password"] = Password
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<PrivatekonomyContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PrivatekonomyContext>>();
                services.RemoveAll<IDbContextFactory<PrivatekonomyContext>>();
                services.RemoveAll<PrivatekonomyContext>();
                services.AddDbContextFactory<PrivatekonomyContext>(options =>
                {
                    if (connection is null)
                        options.UseInMemoryDatabase(_databaseName);
                    else
                        options.UseSqlite(connection);
                });
            });
        }
    }
}
