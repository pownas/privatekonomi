using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class HeatmapAnalysisServiceTests
{
    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    [TestMethod]
    public async Task GenerateHeatmapAsyncReturnsCorrectHeatmapData()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        // Create test category
        var category = new Category
        {
            CategoryId = 1,
            Name = "Mat"
        };
        context.Categories.Add(category);

        // Create test transactions across different weekdays and hours
        var startDate = new DateTime(2025, 1, 1); // Wednesday
        var transactions = new List<Transaction>
        {
            // Monday lunch
            new Transaction
            {
                TransactionId = 1,
                Date = new DateTime(2025, 1, 6, 12, 0, 0), // Monday 12:00
                Amount = 100m,
                IsIncome = false,
                Description = "Lunch",
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            // Friday evening - high expense
            new Transaction
            {
                TransactionId = 2,
                Date = new DateTime(2025, 1, 10, 18, 30, 0), // Friday 18:30
                Amount = 500m,
                IsIncome = false,
                Description = "Middag",
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            // Saturday late night - impulse purchase
            new Transaction
            {
                TransactionId = 3,
                Date = new DateTime(2025, 1, 11, 22, 0, 0), // Saturday 22:00
                Amount = 200m,
                IsIncome = false,
                Description = "Nattmat",
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            // Sunday morning
            new Transaction
            {
                TransactionId = 4,
                Date = new DateTime(2025, 1, 12, 10, 0, 0), // Sunday 10:00
                Amount = 150m,
                IsIncome = false,
                Description = "Brunch",
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var endDate = new DateTime(2025, 1, 31);

        // Act
        var result = await service.GenerateHeatmapAsync(startDate, endDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(startDate, result.StartDate);
        Assert.AreEqual(endDate, result.EndDate);
        Assert.AreEqual(4, result.TransactionCount);
        Assert.AreEqual(950m, result.TotalExpenses); // 100 + 500 + 200 + 150
        Assert.IsTrue(result.HeatmapCells.Any());
    }

    [TestMethod]
    public async Task GenerateHeatmapAsyncCalculatesIntensityLevelsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        var category = new Category { CategoryId = 1, Name = "Test" };
        context.Categories.Add(category);

        // Create transactions with varying amounts to test intensity levels
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                TransactionId = 1,
                Date = new DateTime(2025, 1, 6, 12, 0, 0),
                Amount = 1000m, // High amount - should be intensity 3
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            new Transaction
            {
                TransactionId = 2,
                Date = new DateTime(2025, 1, 7, 14, 0, 0),
                Amount = 200m, // Low amount - should be intensity 0
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GenerateHeatmapAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Assert
        Assert.AreEqual(1000m, result.MaxCellAmount);
        Assert.IsTrue(result.HeatmapCells.Any());
    }

    [TestMethod]
    public async Task GenerateHeatmapAsyncDetectsImpulsePurchases()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        var category = new Category { CategoryId = 1, Name = "Shopping" };
        context.Categories.Add(category);

        var transactions = new List<Transaction>
        {
            // Regular daytime purchase
            new Transaction
            {
                TransactionId = 1,
                Date = new DateTime(2025, 1, 6, 14, 0, 0),
                Amount = 100m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            // Late night impulse purchases (>20:00)
            new Transaction
            {
                TransactionId = 2,
                Date = new DateTime(2025, 1, 10, 21, 0, 0),
                Amount = 300m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            new Transaction
            {
                TransactionId = 3,
                Date = new DateTime(2025, 1, 11, 23, 30, 0),
                Amount = 200m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GenerateHeatmapAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Assert
        Assert.IsNotNull(result.Insights.ImpulsePurchases);
        Assert.AreEqual(500m, result.Insights.ImpulsePurchases.TotalAmount); // 300 + 200
        Assert.AreEqual(2, result.Insights.ImpulsePurchases.TransactionCount);
        Assert.IsTrue(result.Insights.ImpulsePurchases.PercentageOfTotal > 0);
        // 500/600 * 100 = 83.33%, which is >5%, so should be detected
        Assert.IsTrue(result.Insights.ImpulsePurchases.IsDetected);
    }

    [TestMethod]
    public async Task GenerateHeatmapAsyncIdentifiesMostAndLeastExpensiveDays()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        var category = new Category { CategoryId = 1, Name = "Test" };
        context.Categories.Add(category);

        var transactions = new List<Transaction>
        {
            // Monday - low
            new Transaction
            {
                TransactionId = 1,
                Date = new DateTime(2025, 1, 6, 12, 0, 0), // Monday
                Amount = 50m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            // Friday - high
            new Transaction
            {
                TransactionId = 2,
                Date = new DateTime(2025, 1, 10, 18, 0, 0), // Friday
                Amount = 500m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            },
            new Transaction
            {
                TransactionId = 3,
                Date = new DateTime(2025, 1, 10, 20, 0, 0), // Friday
                Amount = 300m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GenerateHeatmapAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Assert
        Assert.IsNotNull(result.Insights.MostExpensiveDay);
        Assert.AreEqual("Fredag", result.Insights.MostExpensiveDay.DayName);
        Assert.AreEqual(800m, result.Insights.MostExpensiveDay.Amount); // 500 + 300

        Assert.IsNotNull(result.Insights.LeastExpensiveDay);
        Assert.AreEqual("Måndag", result.Insights.LeastExpensiveDay.DayName);
        Assert.AreEqual(50m, result.Insights.LeastExpensiveDay.Amount);
    }

    [TestMethod]
    public async Task GenerateHeatmapAsyncFiltersOnCategory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        var categoryMat = new Category { CategoryId = 1, Name = "Mat" };
        var categoryTransport = new Category { CategoryId = 2, Name = "Transport" };
        context.Categories.AddRange(categoryMat, categoryTransport);

        var transactions = new List<Transaction>
        {
            new Transaction
            {
                TransactionId = 1,
                Date = new DateTime(2025, 1, 6, 12, 0, 0),
                Amount = 100m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = categoryMat }
                }
            },
            new Transaction
            {
                TransactionId = 2,
                Date = new DateTime(2025, 1, 7, 8, 0, 0),
                Amount = 50m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 2, Category = categoryTransport }
                }
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GenerateHeatmapAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            categoryId: 1 // Filter on "Mat"
        );

        // Assert
        Assert.AreEqual(1, result.TransactionCount);
        Assert.AreEqual(100m, result.TotalExpenses);
        Assert.AreEqual("Mat", result.CategoryName);
    }

    [TestMethod]
    public async Task GetPatternInsightsAsyncReturnsInsights()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        var category = new Category { CategoryId = 1, Name = "Test" };
        context.Categories.Add(category);

        var transactions = new List<Transaction>
        {
            new Transaction
            {
                TransactionId = 1,
                Date = new DateTime(2025, 1, 6, 12, 0, 0),
                Amount = 100m,
                IsIncome = false,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetPatternInsightsAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.MostExpensiveDay);
        Assert.IsNotNull(result.LeastExpensiveDay);
    }

    [TestMethod]
    public async Task GenerateHeatmapAsyncHandlesEmptyData()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new HeatmapAnalysisService(context);

        // Act
        var result = await service.GenerateHeatmapAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TransactionCount);
        Assert.AreEqual(0m, result.TotalExpenses);
        Assert.AreEqual(0, result.HeatmapCells.Count());
    }
}
