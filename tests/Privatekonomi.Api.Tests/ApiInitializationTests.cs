using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.Persistence;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class ApiInitializationTests
{
    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, "api-initialization-tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    [DataRow("InMemory", false)]
    [DataRow("InMemory", true)]
    [DataRow("Sqlite", false)]
    [DataRow("Sqlite", true)]
    public async Task StartupSeedsReferenceDataWithoutDemoData(string provider, bool seedTestData)
    {
        await using var factory = CreateFactory(provider, seedTestData);
        using var client = factory.CreateClient();
        var templates = await client.GetFromJsonAsync<List<ChallengeTemplate>>("/api/SavingsChallenges/templates");

        Assert.IsNotNull(templates);
        Assert.IsTrue(templates.Count > 0);
        Assert.IsTrue(templates.Any(template => template.Name == "No Spend Weekend"));
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        Assert.AreEqual(templates.Count, await context.ChallengeTemplates.CountAsync());
        Assert.AreEqual(0, await context.Users.CountAsync());
        Assert.AreEqual(0, await context.Transactions.CountAsync());
        Assert.AreEqual(0, await context.Budgets.CountAsync());
    }

    [TestMethod]
    public async Task SqliteRestartPreservesTemplatesAndExistingUserData()
    {
        int count;
        int templateId;
        await using (var factory = CreateFactory("Sqlite", false))
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            count = await context.ChallengeTemplates.CountAsync();
            var template = await context.ChallengeTemplates.FirstAsync();
            templateId = template.ChallengeTemplateId;
            template.Name = "Anpassad mall";
            template.IsActive = false;
            context.Users.Add(new ApplicationUser { Id = "existing-user", UserName = "existing", PasswordHash = "existing-hash" });
            context.Transactions.Add(new Transaction { UserId = "existing-user", Description = "Befintlig", Amount = 123m, Date = new DateTime(2025, 1, 1) });
            await context.SaveChangesAsync();
        }

        await using (var factory = CreateFactory("Sqlite", true))
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            Assert.AreEqual(count, await context.ChallengeTemplates.CountAsync());
            var template = await context.ChallengeTemplates.SingleAsync(t => t.ChallengeTemplateId == templateId);
            Assert.AreEqual("Anpassad mall", template.Name);
            Assert.IsFalse(template.IsActive);
            var user = await context.Users.SingleAsync();
            Assert.AreEqual("existing-user", user.Id);
            Assert.AreEqual("existing-hash", user.PasswordHash);
            var transaction = await context.Transactions.SingleAsync();
            Assert.AreEqual("existing-user", transaction.UserId);
            Assert.AreEqual("Befintlig", transaction.Description);
            Assert.AreEqual(123m, transaction.Amount);
        }
    }

    [TestMethod]
    public async Task SqliteExistingCustomTemplateIsNotReplacedOrSupplemented()
    {
        Directory.CreateDirectory(_directory);
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>().UseSqlite(ConnectionString).Options;
        await using (var context = new PrivatekonomyContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.ChallengeTemplates.Add(new ChallengeTemplate { Name = "Befintlig mall", IsActive = false });
            await context.SaveChangesAsync();
        }

        await using var factory = CreateFactory("Sqlite", false);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var initialized = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var template = await initialized.ChallengeTemplates.SingleAsync();
        Assert.AreEqual("Befintlig mall", template.Name);
        Assert.IsFalse(template.IsActive);
    }

    [TestMethod]
    public async Task JsonFileStartupLoadsExistingDataBeforeSeedingAndSurvivesRestart()
    {
        Directory.CreateDirectory(_directory);
        var category = new Category { CategoryId = 42, Name = "Befintlig kategori", Color = "#123456" };
        await File.WriteAllTextAsync(Path.Combine(_directory, "categories.json"), JsonSerializer.Serialize(new[] { category }));

        for (var i = 0; i < 2; i++)
        {
            await using var factory = CreateFactory("JsonFile", true);
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            Assert.IsTrue(await context.ChallengeTemplates.AnyAsync());
            var loaded = await context.Categories.SingleAsync();
            Assert.AreEqual(42, loaded.CategoryId);
            Assert.AreEqual("Befintlig kategori", loaded.Name);
            Assert.AreEqual("#123456", loaded.Color);
            Assert.AreEqual(0, await context.Users.CountAsync());
            Assert.AreEqual(0, await context.Transactions.CountAsync());
        }
    }

    private string ConnectionString => $"Data Source={Path.Combine(_directory, "database.db")};Pooling=False";

    private StartupFactory CreateFactory(string provider, bool seedTestData)
    {
        Directory.CreateDirectory(_directory);
        return new StartupFactory(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = provider,
            ["Storage:ConnectionString"] = provider == "Sqlite" ? ConnectionString : _directory,
            ["Storage:SeedTestData"] = seedTestData.ToString(),
            ["DemoUser:Enabled"] = "true",
            ["DemoUser:Email"] = "demo@example.com",
            ["DemoUser:Password"] = "Development-only-password1!"
        });
    }

    private sealed class StartupFactory(Dictionary<string, string?> settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<PrivatekonomyContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PrivatekonomyContext>>();
                services.RemoveAll<IDbContextFactory<PrivatekonomyContext>>();
                services.RemoveAll<PrivatekonomyContext>();
                services.PostConfigure<StorageSettings>(storage =>
                {
                    storage.Provider = settings["Storage:Provider"]!;
                    storage.ConnectionString = settings["Storage:ConnectionString"]!;
                    storage.SeedTestData = bool.Parse(settings["Storage:SeedTestData"]!);
                });

                var provider = settings["Storage:Provider"];
                if (provider == "Sqlite")
                    services.AddDbContextFactory<PrivatekonomyContext>(options => options.UseSqlite(settings["Storage:ConnectionString"]));
                else if (provider == "JsonFile")
                {
                    services.AddSingleton<JsonIdentityPersistenceService>();
                    services.AddSingleton<IDataPersistenceService, JsonFilePersistenceService>();
                    services.AddHostedService<JsonFilePersistenceHostedService>();
                    var databaseName = Guid.NewGuid().ToString();
                    services.AddDbContextFactory<PrivatekonomyContext>((serviceProvider, options) =>
                        options.UseInMemoryDatabase(databaseName)
                            .AddInterceptors(serviceProvider.GetRequiredService<JsonIdentityPersistenceService>()));
                }
                else
                {
                    var databaseName = Guid.NewGuid().ToString();
                    services.AddDbContextFactory<PrivatekonomyContext>(options => options.UseInMemoryDatabase(databaseName));
                }
            });
        }
    }
}
