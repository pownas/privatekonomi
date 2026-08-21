using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class SubscriptionServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly SubscriptionService _subscriptionService;
    private const string TestUserId = "test-user-123";

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _subscriptionService = new SubscriptionService(_context, _auditLogServiceMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task GetUnusedSubscriptionsAsync_ReturnsSubscriptionsNotUsedRecently()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-60);
        var recentDate = DateTime.UtcNow.AddDays(-10);

        var subscription1 = new Subscription
        {
            UserId = TestUserId,
            Name = "Netflix",
            Price = 119,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            IsActive = true,
            LastUsedDate = oldDate
        };

        var subscription2 = new Subscription
        {
            UserId = TestUserId,
            Name = "Spotify",
            Price = 99,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-3),
            IsActive = true,
            LastUsedDate = recentDate
        };

        var subscription3 = new Subscription
        {
            UserId = TestUserId,
            Name = "HBO",
            Price = 89,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-4),
            IsActive = true,
            LastUsedDate = null // Never used
        };

        _context.Subscriptions.AddRange(subscription1, subscription2, subscription3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetUnusedSubscriptionsAsync(TestUserId, 45);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count); // subscription1 (60 days old) and subscription3 (never used)
        Assert.IsTrue(result.Any(s => s.Name == "Netflix"));
        Assert.IsTrue(result.Any(s => s.Name == "HBO"));
        Assert.IsFalse(result.Any(s => s.Name == "Spotify"));
    }

    [TestMethod]
    public async Task GetSubscriptionsWithUpcomingCancellationDeadlineAsync_ReturnsOnlyUpcomingDeadlines()
    {
        // Arrange
        var subscription1 = new Subscription
        {
            UserId = TestUserId,
            Name = "Netflix",
            Price = 119,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = true,
            CancellationDeadline = DateTime.UtcNow.AddDays(10)
        };

        var subscription2 = new Subscription
        {
            UserId = TestUserId,
            Name = "Spotify",
            Price = 99,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = true,
            CancellationDeadline = DateTime.UtcNow.AddDays(40) // Beyond 30 days
        };

        var subscription3 = new Subscription
        {
            UserId = TestUserId,
            Name = "HBO",
            Price = 89,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = true,
            CancellationDeadline = null // No deadline
        };

        _context.Subscriptions.AddRange(subscription1, subscription2, subscription3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetSubscriptionsWithUpcomingCancellationDeadlineAsync(TestUserId, 30);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Netflix", result[0].Name);
    }

    [TestMethod]
    public async Task UpdateLastUsedDateAsync_UpdatesDateSuccessfully()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = TestUserId,
            Name = "Netflix",
            Price = 119,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = true,
            LastUsedDate = DateTime.UtcNow.AddDays(-30)
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var newDate = DateTime.UtcNow;

        // Act
        await _subscriptionService.UpdateLastUsedDateAsync(subscription.SubscriptionId, newDate);

        // Assert
        var updated = await _context.Subscriptions.FindAsync(subscription.SubscriptionId);
        Assert.IsNotNull(updated);
        Assert.IsNotNull(updated.LastUsedDate);
        Assert.IsTrue((updated.LastUsedDate.Value - newDate).TotalSeconds < 1);
        Assert.IsNotNull(updated.UpdatedAt);
    }

    [TestMethod]
    public async Task DetectSubscriptionsFromTransactionsAsync_DetectsRecurringPatterns()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.AddMonths(-6);
        
        // Create recurring Netflix transactions
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate,
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate.AddMonths(1),
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate.AddMonths(2),
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate.AddMonths(3),
                IsIncome = false
            }
        };

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.DetectSubscriptionsFromTransactionsAsync(TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count());
        var detected = result[0];
        Assert.IsTrue(detected.Name.Contains("Netflix", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(119, detected.Price);
        Assert.AreEqual("Monthly", detected.BillingFrequency);
        Assert.IsTrue(detected.AutoDetected);
    }

    [TestMethod]
    public async Task DetectSubscriptionsFromTransactionsAsync_IgnoresInconsistentAmounts()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.AddMonths(-6);
        
        // Create transactions with varying amounts
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Random Store",
                Description = "Purchase",
                Amount = 100,
                Currency = "SEK",
                Date = baseDate,
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Random Store",
                Description = "Purchase",
                Amount = 200, // Very different amount
                Currency = "SEK",
                Date = baseDate.AddMonths(1),
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Random Store",
                Description = "Purchase",
                Amount = 50, // Very different amount
                Currency = "SEK",
                Date = baseDate.AddMonths(2),
                IsIncome = false
            }
        };

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.DetectSubscriptionsFromTransactionsAsync(TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count()); // Should not detect inconsistent amounts
    }

    [TestMethod]
    public async Task DetectSubscriptionsFromTransactionsAsync_DoesNotDuplicateExistingSubscriptions()
    {
        // Arrange
        var existingSubscription = new Subscription
        {
            UserId = TestUserId,
            Name = "netflix",
            Price = 119,
            BillingFrequency = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            IsActive = true
        };
        _context.Subscriptions.Add(existingSubscription);
        await _context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow.AddMonths(-6);
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate,
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate.AddMonths(1),
                IsIncome = false
            },
            new Transaction
            {
                UserId = TestUserId,
                Payee = "Netflix",
                Description = "Netflix subscription",
                Amount = 119,
                Currency = "SEK",
                Date = baseDate.AddMonths(2),
                IsIncome = false
            }
        };

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.DetectSubscriptionsFromTransactionsAsync(TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count()); // Should not create duplicate
    }

    [TestMethod]
    public async Task CreateSubscriptionFromTransactionAsync_CreatesSubscriptionSuccessfully()
    {
        // Arrange
        var transaction = new Transaction
        {
            UserId = TestUserId,
            Payee = "Spotify",
            Description = "Spotify Premium",
            Amount = 99,
            Currency = "SEK",
            Date = DateTime.UtcNow,
            IsIncome = false
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.CreateSubscriptionFromTransactionAsync(transaction.TransactionId, TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Name.Contains("Spotify", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(99, result.Price);
        Assert.AreEqual("SEK", result.Currency);
        Assert.IsTrue(result.AutoDetected);
        Assert.AreEqual(transaction.TransactionId, result.DetectedFromTransactionId);
        
        // Verify it was persisted
        var persisted = await _context.Subscriptions.FindAsync(result.SubscriptionId);
        Assert.IsNotNull(persisted);
    }

    [TestMethod]
    public async Task CreateSubscriptionFromTransactionAsync_ReturnsNullForNonexistentTransaction()
    {
        // Act
        var result = await _subscriptionService.CreateSubscriptionFromTransactionAsync(99999, TestUserId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetMonthlySubscriptionCostAsync_CalculatesCorrectly()
    {
        // Arrange
        var subscriptions = new List<Subscription>
        {
            new Subscription
            {
                UserId = TestUserId,
                Name = "Netflix",
                Price = 119,
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow,
                IsActive = true
            },
            new Subscription
            {
                UserId = TestUserId,
                Name = "Spotify",
                Price = 1200,
                BillingFrequency = "Yearly",
                StartDate = DateTime.UtcNow,
                IsActive = true
            },
            new Subscription
            {
                UserId = TestUserId,
                Name = "Gym",
                Price = 300,
                BillingFrequency = "Quarterly",
                StartDate = DateTime.UtcNow,
                IsActive = true
            },
            new Subscription
            {
                UserId = TestUserId,
                Name = "Inactive",
                Price = 500,
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow,
                IsActive = false // Should not be included
            }
        };

        _context.Subscriptions.AddRange(subscriptions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetMonthlySubscriptionCostAsync(TestUserId);

        // Assert
        // Expected: 119 (monthly) + 100 (yearly/12) + 100 (quarterly/3) = 319
        Assert.AreEqual(319, result);
    }
}
