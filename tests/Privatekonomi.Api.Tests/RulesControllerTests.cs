using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Api.Tests.Infrastructure;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Api.Tests;

/// <summary>
/// Unit tests for the RulesController API endpoints.
/// Tests the /api/rules endpoint which provides user categorization rules.
/// </summary>
[TestClass]
public class RulesControllerTests
{
    private async Task<Category> CreateTestCategoryAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        var category = new Category
        {
            Name = "Test Category",
            Color = "#FF0000",
            IsSystemCategory = false,
            TaxRelated = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return category;
    }

    private async Task<CategoryRule> CreateTestRuleAsync(IServiceProvider services, int categoryId)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        var rule = new CategoryRule
        {
            Pattern = "Test Pattern",
            MatchType = PatternMatchType.Contains,
            CategoryId = categoryId,
            Priority = 50,
            IsActive = true,
            CaseSensitive = false,
            Field = MatchField.Both,
            RuleType = RuleType.User,
            CreatedAt = DateTime.UtcNow
        };

        context.CategoryRules.Add(rule);
        await context.SaveChangesAsync();

        return rule;
    }

    [TestMethod]
    public async Task GetAllRulesReturnsEmptyListWhenNoRulesExist()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/rules");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var rules = await response.Content.ReadFromJsonAsync<List<CategoryRule>>();
        Assert.IsNotNull(rules);
        Assert.AreEqual(0, rules.Count());
    }

    [TestMethod]
    public async Task GetAllRulesReturnsListWhenRulesExist()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);
        await CreateTestRuleAsync(factory.Services, category.CategoryId);

        // Act
        var response = await client.GetAsync("/api/rules");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var rules = await response.Content.ReadFromJsonAsync<List<CategoryRule>>();
        Assert.IsNotNull(rules);
        Assert.AreEqual(1, rules.Count());
    }

    [TestMethod]
    public async Task GetActiveRulesReturnsOnlyActiveRules()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);
        
        // Create an active rule
        await CreateTestRuleAsync(factory.Services, category.CategoryId);
        
        // Create an inactive rule
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var inactiveRule = new CategoryRule
            {
                Pattern = "Inactive Pattern",
                MatchType = PatternMatchType.Contains,
                CategoryId = category.CategoryId,
                Priority = 30,
                IsActive = false,
                CaseSensitive = false,
                Field = MatchField.Both,
                RuleType = RuleType.User,
                CreatedAt = DateTime.UtcNow
            };
            context.CategoryRules.Add(inactiveRule);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/api/rules/active");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var rules = await response.Content.ReadFromJsonAsync<List<CategoryRule>>();
        Assert.IsNotNull(rules);
        Assert.AreEqual(1, rules.Count());
        Assert.IsTrue(rules.All(r => r.IsActive));
    }

    [TestMethod]
    public async Task GetRuleReturnsNotFoundWhenRuleDoesNotExist()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/rules/9999");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetRuleReturnsRuleWhenRuleExists()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);
        var createdRule = await CreateTestRuleAsync(factory.Services, category.CategoryId);

        // Act
        var response = await client.GetAsync($"/api/rules/{createdRule.CategoryRuleId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var rule = await response.Content.ReadFromJsonAsync<CategoryRule>();
        Assert.IsNotNull(rule);
        Assert.AreEqual(createdRule.Pattern, rule.Pattern);
    }

    [TestMethod]
    public async Task CreateRuleCreatesUserRule()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);

        var newRule = new CategoryRule
        {
            Pattern = "ICA",
            MatchType = PatternMatchType.Contains,
            CategoryId = category.CategoryId,
            Priority = 100,
            IsActive = true,
            CaseSensitive = false,
            Field = MatchField.Both,
            Description = "Rule for ICA transactions"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/rules", newRule);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var createdRule = await response.Content.ReadFromJsonAsync<CategoryRule>();
        Assert.IsNotNull(createdRule);
        Assert.AreEqual("ICA", createdRule.Pattern);
        Assert.AreEqual(RuleType.User, createdRule.RuleType);
    }

    [TestMethod]
    public async Task UpdateRuleIdMismatchReturnsBadRequest()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);
        var existingRule = await CreateTestRuleAsync(factory.Services, category.CategoryId);

        // Create a rule with mismatched ID in URL vs body
        var updatePayload = new
        {
            CategoryRuleId = existingRule.CategoryRuleId + 1, // Mismatch
            Pattern = "Updated Pattern",
            CategoryId = existingRule.CategoryId,
            Priority = 75,
            IsActive = true,
            CaseSensitive = false,
            MatchType = 1, // Contains
            Field = 2, // Both
            RuleType = 1, // User
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/rules/{existingRule.CategoryRuleId}", updatePayload);

        // Assert - should fail due to ID mismatch
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteRuleRemovesRule()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);
        var existingRule = await CreateTestRuleAsync(factory.Services, category.CategoryId);

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/rules/{existingRule.CategoryRuleId}");
        var getResponse = await client.GetAsync($"/api/rules/{existingRule.CategoryRuleId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestMethod]
    public async Task TestRuleReturnsMatchingRule()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();
        
        var category = await CreateTestCategoryAsync(factory.Services);
        await CreateTestRuleAsync(factory.Services, category.CategoryId);

        var testRequest = new { Description = "This contains Test Pattern in it", Payee = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/rules/test", testRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var matchingRule = await response.Content.ReadFromJsonAsync<CategoryRule>();
        Assert.IsNotNull(matchingRule);
        Assert.AreEqual("Test Pattern", matchingRule.Pattern);
    }

    [TestMethod]
    public async Task TestRuleReturnsNotFoundWhenNoMatchingRule()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory($"RulesTest_{Guid.NewGuid()}");
        var client = factory.CreateClient();

        var testRequest = new { Description = "Some unmatched description", Payee = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/rules/test", testRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
