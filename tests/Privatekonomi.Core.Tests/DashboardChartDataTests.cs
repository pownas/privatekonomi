using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class DashboardChartDataTests
{
    [TestMethod]
    public void CategoryAmountsRespectSplitsAndIncludeUncategorizedTransactions()
    {
        var date = new DateTime(2026, 5, 1);
        var transactions = new[]
        {
            new Transaction
            {
                Date = date,
                Amount = 100m,
                TransactionCategories = new List<TransactionCategory>
                {
                    new() { Category = new Category { Name = "Mat" }, Amount = 60m },
                    new() { Category = new Category { Name = "Hälsa" }, Amount = 40m }
                }
            },
            new Transaction { Date = date, IsIncome = true, Amount = -25m }
        };

        var amounts = DashboardChartData.GetCategoryAmounts(transactions).ToList();

        Assert.AreEqual(3, amounts.Count);
        Assert.AreEqual((date, false, "Mat", 60m), amounts[0]);
        Assert.AreEqual((date, false, "Hälsa", 40m), amounts[1]);
        Assert.AreEqual((date, true, "Okategoriserad", 25m), amounts[2]);
    }

    [TestMethod]
    public void InternalTransfersAreExcludedFromCategoryChartData()
    {
        var transactions = new[]
        {
            new Transaction
            {
                Date = DateTime.Today,
                Amount = 500m,
                IsInternalTransfer = true,
                TransactionCategories = new List<TransactionCategory>
                {
                    new() { Category = new Category { Name = "Överföringar", IsInternalTransferCategory = true }, Amount = 500m }
                }
            },
            new Transaction
            {
                Date = DateTime.Today,
                Amount = 100m,
                IsIncome = true,
                TransactionCategories = new List<TransactionCategory>
                {
                    new() { Category = new Category { Name = "Lön" }, Amount = 100m }
                }
            }
        };

        var amounts = DashboardChartData.GetCategoryAmounts(transactions).ToList();

        Assert.AreEqual(1, amounts.Count);
        Assert.AreEqual("Lön", amounts[0].Category);
    }

    [TestMethod]
    public async Task CategoryStatisticsUseAllocatedAmountsRatherThanEntireTransactions()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new PrivatekonomyContext(options);
        var food = new Category { Name = "Mat" };
        var health = new Category { Name = "Hälsa" };
        context.Transactions.Add(new Transaction
        {
            Date = DateTime.Today,
            Amount = 100m,
            TransactionCategories = new List<TransactionCategory>
            {
                new() { Category = food, Amount = 60m },
                new() { Category = health, Amount = 40m }
            }
        });
        await context.SaveChangesAsync();

        var stats = await new CategoryService(context).GetCategoryStatisticsAsync(food.CategoryId, 6);

        Assert.AreEqual(60m, stats.TotalExpenses);
        Assert.AreEqual(-60m, stats.NetAmount);
        var currentMonth = stats.MonthlyBreakdown.Single(m => m.Year == DateTime.Today.Year && m.Month == DateTime.Today.Month);
        Assert.AreEqual(60m, currentMonth.Expenses);
        Assert.AreEqual(-60m, currentMonth.NetAmount);
        Assert.AreEqual(1, currentMonth.TransactionCount);
    }
}