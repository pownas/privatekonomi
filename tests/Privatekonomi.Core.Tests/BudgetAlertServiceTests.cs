using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BudgetAlertServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<BudgetAlertService>> _mockLogger;

    public BudgetAlertServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<BudgetAlertService>>();
    }

    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    [TestMethod]
    public async Task CalculateBudgetUsagePercentageAsyncReturnsCorrectPercentage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new BudgetAlertService(context, _mockCurrentUserService.Object, 
            _mockNotificationService.Object, _mockLogger.Object);

        var category = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };
        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            Period = BudgetPeriod.Monthly,
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

        // Add transactions totaling 6750 (90% of 7500)
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = new DateTime(2025, 1, 15),
            Description = "Matköp",
            Amount = 6750m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 6750m }
            }
        });

        await context.SaveChangesAsync();

        // Act
        var percentage = await service.CalculateBudgetUsagePercentageAsync(1, 1);

        // Assert
        Assert.AreEqual(90m, percentage);
    }

    [TestMethod]
    public async Task CalculateDailyRateAsyncReturnsCorrectRate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        var category = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };
        
        // Set budget to start in the past so we know the number of elapsed days
        var startDate = DateTime.Now.AddDays(-9); // 10 days ago including today
        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget Test",
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            Period = BudgetPeriod.Monthly
        };

        context.Categories.Add(category);
        context.Budgets.Add(budget);

        // Add transactions totaling 940 over the period
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = startDate.AddDays(5),
            Description = "Matköp",
            Amount = 470m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 470m }
            }
        });

        context.Transactions.Add(new Transaction
        {
            TransactionId = 2,
            Date = startDate.AddDays(9),
            Description = "Matköp",
            Amount = 470m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 470m }
            }
        });

        await context.SaveChangesAsync();

        // Act
        var dailyRate = await service.CalculateDailyRateAsync(1, 1);

        // Assert - 940 / 10 days = 94
        Assert.AreEqual(94m, dailyRate);
    }

    [TestMethod]
    public async Task CalculateDaysUntilExceededAsyncReturnsCorrectForecast()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        var category = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };
        
        // Set budget to start in the past so we know the number of elapsed days
        var startDate = DateTime.Now.AddDays(-9); // 10 days elapsed including today
        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget Test",
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            Period = BudgetPeriod.Monthly,
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

        // Spent 6750 over 10 days = 675/day
        // Remaining: 750
        // Days until exceeded: 750/675 ≈ 2 days (ceiling)
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = startDate.AddDays(5),
            Description = "Matköp",
            Amount = 6750m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 6750m }
            }
        });

        await context.SaveChangesAsync();

        // Act
        var daysUntilExceeded = await service.CalculateDaysUntilExceededAsync(1, 1);

        // Assert
        Assert.IsNotNull(daysUntilExceeded);
        Assert.AreEqual(2, daysUntilExceeded.Value);
    }

    [TestMethod]
    public async Task GetOrCreateSettingsAsyncCreatesDefaultSettings()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-1");
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        // Act
        var settings = await service.GetOrCreateSettingsAsync();

        // Assert
        Assert.IsNotNull(settings);
        Assert.IsTrue(settings.EnableAlert75);
        Assert.IsTrue(settings.EnableAlert90);
        Assert.IsTrue(settings.EnableAlert100);
        Assert.IsTrue(settings.EnableForecastWarnings);
        Assert.AreEqual(7, settings.ForecastWarningDays);
    }

    [TestMethod]
    public async Task FreezeBudgetAsyncCreatesFreezeSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-1");
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-1"
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        // Act
        var freeze = await service.FreezeBudgetAsync(1, null, "Budget överskriden");

        // Assert
        Assert.IsNotNull(freeze);
        Assert.AreEqual(1, freeze.BudgetId);
        Assert.IsTrue(freeze.IsActive);
        Assert.AreEqual("Budget överskriden", freeze.Reason);
    }

    [TestMethod]
    public async Task IsBudgetFrozenAsyncReturnsTrueWhenFrozen()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-1");
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-1"
        };

        context.Budgets.Add(budget);
        context.BudgetFreezes.Add(new BudgetFreeze
        {
            BudgetId = 1,
            FrozenAt = DateTime.UtcNow,
            IsActive = true,
            UserId = "test-user-1"
        });

        await context.SaveChangesAsync();

        // Act
        var isFrozen = await service.IsBudgetFrozenAsync(1);

        // Assert
        Assert.IsTrue(isFrozen);
    }

    [TestMethod]
    public async Task CheckBudgetAsyncCreatesAlertWhenThresholdExceeded()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-1");
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<SystemNotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new Notification());

        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        var category = new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF5733" };
        var budget = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2025-01",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-1",
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

        // Add transaction reaching 90% threshold
        context.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            Date = new DateTime(2025, 1, 15),
            Description = "Matköp",
            Amount = 6750m,
            IsIncome = false,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Amount = 6750m }
            }
        });

        // Create default settings
        context.BudgetAlertSettings.Add(new BudgetAlertSettings
        {
            UserId = "test-user-1",
            EnableAlert75 = true,
            EnableAlert90 = true,
            EnableAlert100 = true
        });

        await context.SaveChangesAsync();

        // Act
        var alerts = await service.CheckBudgetAsync(1);

        // Assert
        Assert.AreNotEqual(0, alerts.Count());
        Assert.IsTrue(alerts.Any(a => a.ThresholdPercentage == 75m));
        Assert.IsTrue(alerts.Any(a => a.ThresholdPercentage == 90m));
        
        // Verify notification was sent
        _mockNotificationService.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<string>(),
                SystemNotificationType.BudgetWarning,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task AcknowledgeAlertAsyncMarksAlertAsInactive()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-1");
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        var service = new BudgetAlertService(context, _mockCurrentUserService.Object,
            _mockNotificationService.Object, _mockLogger.Object);

        var alert = new BudgetAlert
        {
            BudgetAlertId = 1,
            BudgetId = 1,
            BudgetCategoryId = 1,
            ThresholdPercentage = 90m,
            CurrentPercentage = 92m,
            SpentAmount = 6900m,
            PlannedAmount = 7500m,
            TriggeredAt = DateTime.UtcNow,
            IsActive = true,
            UserId = "test-user-1"
        };

        context.BudgetAlerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        await service.AcknowledgeAlertAsync(1);

        // Assert
        var updatedAlert = await context.BudgetAlerts.FindAsync(1);
        Assert.IsNotNull(updatedAlert);
        Assert.IsFalse(updatedAlert.IsActive);
        Assert.IsNotNull(updatedAlert.AcknowledgedAt);
    }
}
