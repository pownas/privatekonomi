using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Tests.Infrastructure;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class PersonalTransactionsIntegrationTests
{
    private const string Password = "StrongPassword123!";

    [TestMethod]
    public async Task LoginAndPersonalTransactionReadsEnforceIsolationAndPagination()
    {
        await using var factory = new ApiWebApplicationFactory("PersonalTransactions_" + Guid.NewGuid());
        using var anonymous = CreateClient(factory);
        var (aliceIds, bobId, householdId, unownedId) = await SeedAsync(factory);

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/personal/transactions")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/personal/transactions/{aliceIds[0]}")).StatusCode);
        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "forged");
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/personal/transactions")).StatusCode);
        anonymous.DefaultRequestHeaders.Authorization = null;
        anonymous.DefaultRequestHeaders.Add("X-User-Id", "alice");
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/personal/transactions")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await LoginResponseAsync(anonymous, "alice@example.com", "wrong-password")).StatusCode);

        using var alice = CreateClient(factory);
        using var bob = CreateClient(factory);
        await LoginAsync(alice, "alice@example.com");
        await LoginAsync(bob, "bob@example.com");

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await alice.GetAsync("/api/salaryhistory")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/salaryhistory")).StatusCode);
        var first = await alice.GetFromJsonAsync<PersonalTransactionPage>("/api/personal/transactions?limit=1&userId=bob");
        Assert.IsNotNull(first);
        Assert.AreEqual(1, first.Transactions.Count);
        Assert.AreEqual(aliceIds[1], first.Transactions[0].Id);
        Assert.AreEqual(aliceIds[1], first.NextBeforeId);
        var second = await alice.GetFromJsonAsync<PersonalTransactionPage>($"/api/personal/transactions?limit=1&beforeId={first.NextBeforeId}");
        Assert.IsNotNull(second);
        Assert.AreEqual(aliceIds[0], second.Transactions.Single().Id);
        Assert.IsNull(second.NextBeforeId);
        var bobPage = await bob.GetFromJsonAsync<PersonalTransactionPage>("/api/personal/transactions");
        Assert.IsNotNull(bobPage);
        Assert.AreEqual(bobId, bobPage.Transactions.Single().Id);

        Assert.AreEqual(HttpStatusCode.OK, (await alice.GetAsync($"/api/personal/transactions/{aliceIds[0]}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/personal/transactions/{bobId}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/personal/transactions/{aliceIds[0]}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/personal/transactions/{householdId}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/personal/transactions/{unownedId}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await alice.GetAsync("/api/personal/transactions?limit=101")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await alice.GetAsync("/api/personal/transactions?beforeId=0")).StatusCode);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, (await alice.DeleteAsync($"/api/personal/transactions/{bobId}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, (await alice.PutAsJsonAsync($"/api/personal/transactions/{bobId}", new { amount = 999 })).StatusCode);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, (await alice.PostAsJsonAsync("/api/personal/transactions", new { userId = "bob", amount = 999 })).StatusCode);

        var detail = await alice.GetAsync($"/api/personal/transactions/{aliceIds[0]}");
        Assert.IsTrue(detail.Headers.CacheControl?.NoStore);
        var json = await detail.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.IsFalse(document.RootElement.TryGetProperty("userId", out _));
        Assert.IsFalse(document.RootElement.TryGetProperty("accountNumber", out _));
        Assert.IsFalse(document.RootElement.TryGetProperty("receipts", out _));
    }

    [TestMethod]
    public async Task RepeatedBadPasswordsLockAccountAndInvalidateExistingToken()
    {
        await using var factory = new ApiWebApplicationFactory("PersonalLockout_" + Guid.NewGuid());
        await SeedAsync(factory);
        using var client = CreateClient(factory);
        await LoginAsync(client, "alice@example.com");
        for (var i = 0; i < 5; i++)
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await LoginResponseAsync(client, "alice@example.com", "wrong-password")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await LoginResponseAsync(client, "alice@example.com", Password)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/personal/transactions")).StatusCode);
    }

    [TestMethod]
    public async Task CaseInsensitiveDatabaseCollationDoesNotExposeAnotherOwner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        await using var factory = new CaseInsensitiveSqliteFactory(connection);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        int foreignId;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var alice = new ApplicationUser { Id = "alice", UserName = "alice@example.com", Email = "alice@example.com", EmailConfirmed = true };
            Assert.IsTrue((await users.CreateAsync(alice, Password)).Succeeded);
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var foreign = new Transaction { UserId = "ALICE", Description = "Foreign", Amount = 10, Date = DateTime.UtcNow };
            context.Transactions.Add(foreign);
            await context.SaveChangesAsync();
            Assert.AreEqual(1, await context.Transactions.CountAsync(t => t.UserId == "alice"));
            foreignId = foreign.TransactionId;
        }

        await LoginAsync(client, "alice@example.com");
        var page = await client.GetFromJsonAsync<PersonalTransactionPage>("/api/personal/transactions");
        Assert.IsNotNull(page);
        Assert.AreEqual(0, page.Transactions.Count);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync($"/api/personal/transactions/{foreignId}")).StatusCode);
    }

    [TestMethod]
    public async Task ExistingTokensStopWorkingAfterSecurityChanges()
    {
        await using var factory = new ApiWebApplicationFactory("PersonalRevocation_" + Guid.NewGuid());
        await SeedAsync(factory);
        using var client = CreateClient(factory);
        await LoginAsync(client, "alice@example.com");

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("alice@example.com"))!;
            Assert.IsTrue((await users.UpdateSecurityStampAsync(user)).Succeeded);
        }
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/personal/transactions")).StatusCode);

        await LoginAsync(client, "alice@example.com");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("alice@example.com"))!;
            Assert.IsTrue((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
        }
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/personal/transactions")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await LoginResponseAsync(client, "alice@example.com", Password)).StatusCode);
    }

    [TestMethod]
    public async Task PlainHttpCannotIssueOrUseBearerTokens()
    {
        await using var factory = new ApiWebApplicationFactory("PersonalHttps_" + Guid.NewGuid());
        await SeedAsync(factory);
        using var secure = CreateClient(factory);
        await LoginAsync(secure, "alice@example.com");
        using var insecure = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });
        var login = await LoginResponseAsync(insecure, "alice@example.com", Password);
        Assert.AreNotEqual(HttpStatusCode.OK, login.StatusCode);
        insecure.DefaultRequestHeaders.Authorization = secure.DefaultRequestHeaders.Authorization;
        Assert.AreNotEqual(HttpStatusCode.OK, (await insecure.GetAsync("/api/personal/transactions")).StatusCode);
    }

    private static HttpClient CreateClient(ApiWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private static Task<HttpResponseMessage> LoginResponseAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/session/login", new ApiLoginRequest { Email = email, Password = password });

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var response = await LoginResponseAsync(client, email, Password);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        using var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", token.RootElement.GetProperty("accessToken").GetString());
    }

    private static async Task<(int[] AliceIds, int BobId, int HouseholdId, int UnownedId)> SeedAsync(ApiWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var name in new[] { "alice", "bob" })
        {
            var user = new ApplicationUser { Id = name, UserName = name + "@example.com", Email = name + "@example.com", EmailConfirmed = true };
            var result = await users.CreateAsync(user, Password);
            Assert.IsTrue(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var entries = new[]
        {
            new Transaction { UserId = "alice", Description = "Alice 1", Amount = 10, Date = DateTime.UtcNow },
            new Transaction { UserId = "alice", Description = "Alice 2", Amount = 20, Date = DateTime.UtcNow },
            new Transaction { UserId = "bob", Description = "Bob", Amount = 30, Date = DateTime.UtcNow },
            new Transaction { UserId = "alice", HouseholdId = 1, Description = "Household", Amount = 40, Date = DateTime.UtcNow },
            new Transaction { UserId = null, Description = "Unowned", Amount = 50, Date = DateTime.UtcNow }
        };
        context.Transactions.AddRange(entries);
        await context.SaveChangesAsync();
        return ([entries[0].TransactionId, entries[1].TransactionId], entries[2].TransactionId,
            entries[3].TransactionId, entries[4].TransactionId);
    }

    private sealed class CaseInsensitiveSqliteFactory(SqliteConnection connection) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<PrivatekonomyContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PrivatekonomyContext>>();
                services.RemoveAll<IDbContextFactory<PrivatekonomyContext>>();
                services.RemoveAll<PrivatekonomyContext>();
                services.AddDbContextFactory<PrivatekonomyContext>(options => options.UseSqlite(connection)
                    .ReplaceService<IModelCustomizer, CaseInsensitiveModelCustomizer>());
            });
        }
    }

    private sealed class CaseInsensitiveModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<Transaction>().Property(t => t.UserId).UseCollation("NOCASE");
        }
    }
}
