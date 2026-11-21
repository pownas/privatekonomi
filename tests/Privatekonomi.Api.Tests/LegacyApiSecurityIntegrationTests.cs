using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Api.Controllers;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Tests.Infrastructure;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class LegacyApiSecurityIntegrationTests
{
    [TestMethod]
    public void EveryControllerIsExplicitlyClassified()
    {
        var migrated = new HashSet<Type>
        {
            typeof(ApiInfoController), typeof(ApiSessionController), typeof(PersonalTransactionsController)
        };
        var controllers = typeof(TransactionsController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

        foreach (var controller in controllers)
        {
            Assert.IsTrue(migrated.Contains(controller) ||
                controller.GetCustomAttribute<LegacyApiDisabledAttribute>(inherit: true) != null,
                $"{controller.Name} must be explicitly classified before it is exposed.");
        }
    }

    [TestMethod]
    public async Task LegacyReadsWritesAndOAuthCallbackAreBlockedForAnonymousAndBearerClients()
    {
        await using var factory = new ApiWebApplicationFactory("LegacySecurity_" + Guid.NewGuid());
        int transactionId;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var alice = new ApplicationUser { Id = "alice", UserName = "alice@example.com", Email = "alice@example.com", EmailConfirmed = true };
            Assert.IsTrue((await users.CreateAsync(alice, "StrongPassword123!")).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var transaction = new Transaction { UserId = "bob", Description = "Private Bob", Amount = 123, Date = DateTime.UtcNow };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
            transactionId = transaction.TransactionId;
        }

        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        using var bearer = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        var login = await bearer.PostAsJsonAsync("/api/session/login",
            new ApiLoginRequest { Email = "alice@example.com", Password = "StrongPassword123!" });
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        using var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", token.RootElement.GetProperty("accessToken").GetString());
        Assert.AreEqual(HttpStatusCode.OK, (await bearer.GetAsync("/api/personal/transactions")).StatusCode);

        foreach (var client in new[] { anonymous, bearer })
        {
            foreach (var path in new[]
            {
                "/api/transactions", $"/api/transactions/{transactionId}",
                "/api/export/transactions/csv", "/api/export/backup",
                "/api/bankconnections", "/api/bankconnections/callback?code=secret&state=test",
                "/api/me/dashboard", "/api/categories"
            })
            {
                using var response = await client.GetAsync(path);
                Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, path);
                Assert.IsNull(response.Headers.Location, path);
                Assert.IsFalse((await response.Content.ReadAsStringAsync()).Contains("Private Bob"), path);
            }

            using var create = await client.PostAsJsonAsync("/api/transactions",
                new { userId = "bob", description = "Injected", amount = 999, date = DateTime.UtcNow });
            Assert.AreEqual(HttpStatusCode.Forbidden, create.StatusCode);
            using var update = await client.PutAsJsonAsync($"/api/transactions/{transactionId}", new { amount = 999 });
            Assert.AreEqual(HttpStatusCode.Forbidden, update.StatusCode);
            using var delete = await client.DeleteAsync($"/api/transactions/{transactionId}");
            Assert.AreEqual(HttpStatusCode.Forbidden, delete.StatusCode);
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("{}"), "file", "backup.json");
            using var restore = await client.PostAsync("/api/export/backup/import", content);
            Assert.AreEqual(HttpStatusCode.Forbidden, restore.StatusCode);
        }

        using var checkScope = factory.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var saved = await checkDb.Transactions.SingleAsync();
        Assert.AreEqual(transactionId, saved.TransactionId);
        Assert.AreEqual(123m, saved.Amount);
        Assert.AreEqual("Private Bob", saved.Description);
    }
}
