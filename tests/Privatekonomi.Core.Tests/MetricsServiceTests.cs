using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class MetricsServiceTests
{
    private readonly Mock<ILogger<MetricsService>> _mockLogger;

    public MetricsServiceTests()
    {
        _mockLogger = new Mock<ILogger<MetricsService>>();
    }

    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncReturnsMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        // Add test users
        var now = DateTime.UtcNow;
        context.Users.AddRange(
            new ApplicationUser { Id = "user1", Email = "user1@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = now },
            new ApplicationUser { Id = "user2", Email = "user2@test.com", CreatedAt = now.AddMonths(-1), LastLoginAt = now.AddDays(-15) },
            new ApplicationUser { Id = "user3", Email = "user3@test.com", CreatedAt = now.AddDays(-5), LastLoginAt = now }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.UserMetrics);
        Assert.IsNotNull(result.EngagementMetrics);
        Assert.IsNotNull(result.PerformanceMetrics);
        Assert.IsNotNull(result.SecurityMetrics);
        Assert.AreEqual(3, result.UserMetrics.TotalUsers);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncCalculatesMAUCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        
        // Add users - 2 active this month, 1 inactive
        // Use startOfMonth.AddDays() to ensure dates are within the current month
        context.Users.AddRange(
            new ApplicationUser { Id = "user1", Email = "user1@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = now },
            new ApplicationUser { Id = "user2", Email = "user2@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = startOfMonth.AddDays(1) },
            new ApplicationUser { Id = "user3", Email = "user3@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = now.AddMonths(-2) }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.AreEqual(2, result.UserMetrics.MAU);
        Assert.AreEqual(3, result.UserMetrics.TotalUsers);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncCalculatesDAUCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        var now = DateTime.UtcNow;
        var today = now.Date;
        
        // Add users - 1 active today, 2 not active today
        context.Users.AddRange(
            new ApplicationUser { Id = "user1", Email = "user1@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = today.AddHours(10) },
            new ApplicationUser { Id = "user2", Email = "user2@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = today.AddDays(-1) },
            new ApplicationUser { Id = "user3", Email = "user3@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = today.AddDays(-5) }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.AreEqual(1, result.UserMetrics.DAU);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncCalculatesTransactionsPerUserCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        
        // Add users with login dates in the current month
        var user1 = new ApplicationUser { Id = "user1", Email = "user1@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = startOfMonth };
        var user2 = new ApplicationUser { Id = "user2", Email = "user2@test.com", CreatedAt = now.AddMonths(-2), LastLoginAt = startOfMonth };
        context.Users.AddRange(user1, user2);
        
        // Add transactions this month - use dates that are at or before 'now' to ensure they're counted
        // The service filters: t.Date >= startOfMonth && t.Date <= now
        // Using startOfMonth as the base date ensures all transactions are within valid range
        context.Transactions.AddRange(
            new Transaction { TransactionId = 1, UserId = "user1", Date = startOfMonth, Amount = 100, Description = "Test1" },
            new Transaction { TransactionId = 2, UserId = "user1", Date = startOfMonth, Amount = 200, Description = "Test2" },
            new Transaction { TransactionId = 3, UserId = "user1", Date = startOfMonth, Amount = 300, Description = "Test3" },
            new Transaction { TransactionId = 4, UserId = "user2", Date = startOfMonth, Amount = 400, Description = "Test4" },
            new Transaction { TransactionId = 5, UserId = "user2", Date = startOfMonth, Amount = 500, Description = "Test5" },
            new Transaction { TransactionId = 6, UserId = "user2", Date = startOfMonth, Amount = 600, Description = "Test6" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert - 6 transactions for 2 active users = 3 per user
        Assert.AreEqual(3m, result.EngagementMetrics.TransactionsPerUser);
        Assert.AreEqual(6, result.EngagementMetrics.TotalTransactionsThisMonth);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncCalculatesNewUsersThisMonthCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        
        // Add users - 2 new this month, 1 old
        context.Users.AddRange(
            new ApplicationUser { Id = "user1", Email = "user1@test.com", CreatedAt = startOfMonth.AddDays(5), LastLoginAt = now },
            new ApplicationUser { Id = "user2", Email = "user2@test.com", CreatedAt = startOfMonth.AddDays(10), LastLoginAt = now },
            new ApplicationUser { Id = "user3", Email = "user3@test.com", CreatedAt = startOfMonth.AddMonths(-2), LastLoginAt = now }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.AreEqual(2, result.UserMetrics.NewUsersThisMonth);
        Assert.AreEqual(3, result.UserMetrics.TotalUsers);
    }

    [TestMethod]
    public async Task GetMetricsForPeriodAsyncReturnsMetricsForSpecificPeriod()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        
        // Add users with activity in January
        context.Users.AddRange(
            new ApplicationUser { Id = "user1", Email = "user1@test.com", CreatedAt = startDate.AddDays(-30), LastLoginAt = startDate.AddDays(10) },
            new ApplicationUser { Id = "user2", Email = "user2@test.com", CreatedAt = startDate.AddDays(-30), LastLoginAt = startDate.AddDays(20) }
        );
        
        // Add transactions in January
        context.Transactions.AddRange(
            new Transaction { TransactionId = 1, UserId = "user1", Date = startDate.AddDays(5), Amount = 100, Description = "Test1" },
            new Transaction { TransactionId = 2, UserId = "user2", Date = startDate.AddDays(15), Amount = 200, Description = "Test2" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMetricsForPeriodAsync(startDate, endDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.UserMetrics.MAU);
        Assert.AreEqual(2, result.EngagementMetrics.TotalTransactionsThisMonth);
    }

    [TestMethod]
    public async Task GetHistoricalMetricsAsyncReturnsCorrectNumberOfSnapshots()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        // Add some test data
        var now = DateTime.UtcNow;
        context.Users.Add(new ApplicationUser 
        { 
            Id = "user1", 
            Email = "user1@test.com", 
            CreatedAt = now.AddMonths(-6), 
            LastLoginAt = now 
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetHistoricalMetricsAsync(MetricsPeriodType.Monthly, 6);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(6, result.Count);
        foreach (var snapshot in result) { Assert.AreEqual(MetricsPeriodType.Monthly, snapshot.PeriodType); }
    }

    [TestMethod]
    public async Task GetHistoricalMetricsAsyncQuarterlyPeriodReturnsQuarterlySnapshots()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        // Add test data
        var now = DateTime.UtcNow;
        context.Users.Add(new ApplicationUser 
        { 
            Id = "user1", 
            Email = "user1@test.com", 
            CreatedAt = now.AddMonths(-12), 
            LastLoginAt = now 
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetHistoricalMetricsAsync(MetricsPeriodType.Quarterly, 4);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.Count);
        foreach (var snapshot in result) { Assert.AreEqual(MetricsPeriodType.Quarterly, snapshot.PeriodType); }
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncWithNoDataReturnsZeroMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        // Act - no data added
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.UserMetrics.TotalUsers);
        Assert.AreEqual(0, result.UserMetrics.MAU);
        Assert.AreEqual(0, result.UserMetrics.DAU);
        Assert.AreEqual(0m, result.EngagementMetrics.TransactionsPerUser);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncGDPRComplianceAlwaysReturns100Percent()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.AreEqual(100m, result.SecurityMetrics.GDPRCompliancePercent);
    }

    [TestMethod]
    public async Task GetCurrentMetricsAsyncPerformanceMetricsReturnsExpectedValues()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.GetCurrentMetricsAsync();

        // Assert
        Assert.IsTrue(result.PerformanceMetrics.UptimePercent >= 99.0m);
        Assert.IsTrue(result.PerformanceMetrics.AveragePageLoadTime > 0);
        Assert.IsTrue(result.PerformanceMetrics.LighthouseScore >= 0);
        Assert.IsTrue(result.PerformanceMetrics.CrashRate >= 0);
    }
}
