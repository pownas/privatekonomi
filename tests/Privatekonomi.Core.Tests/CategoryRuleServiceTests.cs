using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class CategoryRuleServiceTests
{
    [TestMethod]
    public async Task FindMatchingRuleAsyncExactAmountRequiresMatchingTextAndMagnitude()
    {
        await using var context = CreateContext();
        var rule = CreateRule(exactAmount: 49.99m);
        await SeedRuleAsync(context, rule);
        var service = new CategoryRuleService(context);

        Assert.AreSame(rule, await service.FindMatchingRuleAsync("SaaS subscription", amount: -49.99m));
        Assert.IsNull(await service.FindMatchingRuleAsync("SaaS subscription", amount: 50m));
        Assert.IsNull(await service.FindMatchingRuleAsync("Other transaction", amount: 49.99m));
        Assert.IsNull(await service.FindMatchingRuleAsync("SaaS subscription"));
    }

    [TestMethod]
    public async Task FindMatchingRuleAsyncAmountRangeIncludesBothBounds()
    {
        await using var context = CreateContext();
        var rule = CreateRule(minimumAmount: 100m, maximumAmount: 200m);
        await SeedRuleAsync(context, rule);
        var service = new CategoryRuleService(context);

        Assert.AreSame(rule, await service.FindMatchingRuleAsync("SaaS subscription", amount: 100m));
        Assert.AreSame(rule, await service.FindMatchingRuleAsync("SaaS subscription", amount: -200m));
        Assert.IsNull(await service.FindMatchingRuleAsync("SaaS subscription", amount: 99.99m));
        Assert.IsNull(await service.FindMatchingRuleAsync("SaaS subscription", amount: 200.01m));
    }

    [TestMethod]
    public async Task FindMatchingRuleAsyncRuleWithoutAmountCriteriaStillMatchesLegacyCalls()
    {
        await using var context = CreateContext();
        var rule = CreateRule();
        await SeedRuleAsync(context, rule);
        var service = new CategoryRuleService(context);

        Assert.AreSame(rule, await service.FindMatchingRuleAsync("SaaS subscription"));
    }

    [TestMethod]
    public async Task CreateRuleAsyncRejectsInvalidAmountCriteria()
    {
        await using var context = CreateContext();
        var service = new CategoryRuleService(context);
        var invalidRules = new[]
        {
            CreateRule(minimumAmount: 100m),
            CreateRule(minimumAmount: 200m, maximumAmount: 100m),
            CreateRule(exactAmount: 10m, minimumAmount: 1m, maximumAmount: 20m)
        };

        foreach (var rule in invalidRules)
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => service.CreateRuleAsync(rule));
        }

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.CreateRuleAsync(CreateRule(exactAmount: -1m)));
    }

    private static PrivatekonomyContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PrivatekonomyContext(options);
    }

    private static async Task SeedRuleAsync(PrivatekonomyContext context, CategoryRule rule)
    {
        context.Categories.Add(new Category
        {
            CategoryId = rule.CategoryId,
            Name = "Test category",
            Color = "#123456",
            CreatedAt = DateTime.UtcNow
        });
        context.CategoryRules.Add(rule);
        await context.SaveChangesAsync();
    }

    private static CategoryRule CreateRule(
        decimal? exactAmount = null,
        decimal? minimumAmount = null,
        decimal? maximumAmount = null) => new()
    {
        Pattern = "SaaS",
        CategoryId = 1,
        Field = MatchField.Description,
        MatchType = PatternMatchType.Contains,
        RuleType = RuleType.User,
        ExactAmount = exactAmount,
        MinimumAmount = minimumAmount,
        MaximumAmount = maximumAmount
    };
}
