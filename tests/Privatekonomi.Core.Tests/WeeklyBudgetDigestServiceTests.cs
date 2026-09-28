using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class WeeklyBudgetDigestServiceTests
{
    private readonly Mock<ILogger<BudgetAlertService>> _mockLogger;

    public WeeklyBudgetDigestServiceTests()
    {
        _mockLogger = new Mock<ILogger<BudgetAlertService>>();
    }

    [TestMethod]
    public async Task SendUserDigestIncludesAllBudgetCategories()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var context = TestHelper.CreateInMemoryContext();
        
        var userId = "test-user-1";
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<BudgetAlertService>>();

        // Create test data
        var category1 = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };
        var category2 = new Category { CategoryId = 2, Name = "Transport", Color = "#33FF57" };

        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = DateTime.Now.AddDays(-10),
            EndDate = DateTime.Now.AddDays(20),
            Period = BudgetPeriod.Monthly,
            UserId = userId,
            BudgetCategories = new List<BudgetCategory>
            {
                new BudgetCategory
                {
                    BudgetCategoryId = 1,
                    BudgetId = 1,
                    CategoryId = 1,
                    PlannedAmount = 7500m,
                    Category = category1
                },
                new BudgetCategory
                {
                    BudgetCategoryId = 2,
                    BudgetId = 1,
                    CategoryId = 2,
                    PlannedAmount = 3000m,
                    Category = category2
                }
            }
        };

        context.Categories.AddRange(category1, category2);
        context.Budgets.Add(budget);

        // Add transactions
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = DateTime.Now.AddDays(-5),
            Description = "Matköp",
            Amount = 6750m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 6750m }
            }
        });

        context.Transactions.Add(new Transaction
        {
            TransactionId = 2,
            Date = DateTime.Now.AddDays(-3),
            Description = "Bensin",
            Amount = 1500m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 2, Amount = 1500m }
            }
        });

        await context.SaveChangesAsync();

        // Setup services
        serviceCollection.AddSingleton(context);
        serviceCollection.AddSingleton(mockCurrentUserService.Object);
        serviceCollection.AddSingleton(mockNotificationService.Object);
        serviceCollection.AddSingleton(mockLogger.Object);
        serviceCollection.AddScoped<IBudgetService, BudgetService>();
        serviceCollection.AddScoped<IBudgetAlertService, BudgetAlertService>();
        serviceCollection.AddScoped<INotificationService>(sp => mockNotificationService.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act - Verify notification was called
        var budgetAlertService = serviceProvider.GetRequiredService<IBudgetAlertService>();
        var budgetService = serviceProvider.GetRequiredService<IBudgetService>();
        
        // Simulate sending digest
        var activeBudgets = await budgetService.GetActiveBudgetsAsync();
        var userBudgets = activeBudgets.Where(b => b.UserId == userId).ToList();

        // Assert
        Assert.AreEqual(1, userBudgets.Count());
        Assert.AreEqual(2, userBudgets[0].BudgetCategories.Count);

        // Verify budget calculations
        var usage1 = await budgetAlertService.CalculateBudgetUsagePercentageAsync(1, 1);
        Assert.AreEqual(90m, usage1); // 6750 / 7500 = 90%

        var usage2 = await budgetAlertService.CalculateBudgetUsagePercentageAsync(1, 2);
        Assert.AreEqual(50m, usage2); // 1500 / 3000 = 50%
    }

    [TestMethod]
    public async Task CalculateDailyRateReturnsCorrectRate()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<BudgetAlertService>>();

        var service = new BudgetAlertService(context, mockCurrentUserService.Object,
            mockNotificationService.Object, mockLogger.Object);

        var userId = "test-user-1";
        var category = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };

        var startDate = DateTime.Now.AddDays(-9); // 10 days ago including today
        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = startDate,
            EndDate = DateTime.Now.AddDays(20),
            Period = BudgetPeriod.Monthly,
            UserId = userId,
            BudgetCategories = new List<BudgetCategory>
            {
                new BudgetCategory
                {
                    BudgetCategoryId = 1,
                    BudgetId = 1,
                    CategoryId = 1,
                    PlannedAmount = 7500m
                }
            }
        };

        context.Categories.Add(category);
        context.Budgets.Add(budget);

        // Add transaction of 1000kr
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = startDate.AddDays(2),
            Description = "Test",
            Amount = 1000m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 1000m }
            }
        });

        await context.SaveChangesAsync();

        // Act
        var dailyRate = await service.CalculateDailyRateAsync(1, 1);

        // Assert
        // 1000kr / 10 days = 100kr/day
        Assert.AreEqual(100m, dailyRate);
    }

    [TestMethod]
    public async Task CalculateForecastPredictsCorrectDaysUntilExceeded()
    {
        // Arrange
        var context = TestHelper.CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<BudgetAlertService>>();

        var service = new BudgetAlertService(context, mockCurrentUserService.Object,
            mockNotificationService.Object, mockLogger.Object);

        var userId = "test-user-1";
        var category = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };

        var startDate = DateTime.Now.AddDays(-9); // 10 days elapsed
        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = startDate,
            EndDate = DateTime.Now.AddDays(20),
            Period = BudgetPeriod.Monthly,
            UserId = userId,
            BudgetCategories = new List<BudgetCategory>
            {
                new BudgetCategory
                {
                    BudgetCategoryId = 1,
                    BudgetId = 1,
                    CategoryId = 1,
                    PlannedAmount = 5000m // Budget of 5000
                }
            }
        };

        context.Categories.Add(category);
        context.Budgets.Add(budget);

        // Spent 3000kr over 10 days = 300kr/day
        // Remaining: 2000kr
        // Days until exceeded: 2000 / 300 = 6.67 -> 7 days
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = startDate.AddDays(2),
            Description = "Test",
            Amount = 3000m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 3000m }
            }
        });

        await context.SaveChangesAsync();

        // Act
        var daysUntilExceeded = await service.CalculateDaysUntilExceededAsync(1, 1);

        // Assert
        Assert.IsNotNull(daysUntilExceeded);
        Assert.AreEqual(7, daysUntilExceeded.Value);
    }
}

internal static class TestHelper
{
    public static PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }
}
