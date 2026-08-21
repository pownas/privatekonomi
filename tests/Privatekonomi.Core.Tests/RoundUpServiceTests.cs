using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class RoundUpServiceTests : IDisposable
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IGoalService> _mockGoalService;
    private readonly PrivatekonomyContext _context;
    private readonly RoundUpService _service;
    private const string TestUserId = "test-user-123";

    public RoundUpServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PrivatekonomyContext(options);

        // Setup mocks
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(TestUserId);
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);

        _mockGoalService = new Mock<IGoalService>();

        _service = new RoundUpService(_context, _mockGoalService.Object, _mockCurrentUserService.Object);
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
    public async Task GetOrCreateSettingsAsync_CreatesNewSettings_WhenNoneExist()
    {
        // Act
        var settings = await _service.GetOrCreateSettingsAsync();

        // Assert
        Assert.IsNotNull(settings);
        Assert.AreEqual(TestUserId, settings.UserId);
        Assert.IsFalse(settings.IsEnabled);
        Assert.AreEqual(10M, settings.RoundUpAmount);
        Assert.IsTrue(settings.OnlyExpenses);
    }

    [TestMethod]
    public async Task GetOrCreateSettingsAsync_ReturnsExistingSettings_WhenAlreadyExists()
    {
        // Arrange
        var existingSettings = new RoundUpSettings
        {
            UserId = TestUserId,
            IsEnabled = true,
            RoundUpAmount = 5M,
            CreatedAt = DateTime.UtcNow
        };
        _context.RoundUpSettings.Add(existingSettings);
        await _context.SaveChangesAsync();

        // Act
        var settings = await _service.GetOrCreateSettingsAsync();

        // Assert
        Assert.IsNotNull(settings);
        Assert.AreEqual(existingSettings.RoundUpSettingsId, settings.RoundUpSettingsId);
        Assert.IsTrue(settings.IsEnabled);
        Assert.AreEqual(5M, settings.RoundUpAmount);
    }

    [DataTestMethod]
    [DataRow(127, 10, 3)]      // 127 -> 130 (3 kr saved)
    [DataRow(245, 10, 5)]      // 245 -> 250 (5 kr saved)
    [DataRow(587, 10, 3)]      // 587 -> 590 (3 kr saved)
    [DataRow(100, 10, 0)]      // 100 -> 100 (0 kr saved, already rounded)
    [DataRow(99.50, 10, 0.50)] // 99.50 -> 100 (0.50 kr saved)
    [DataRow(1, 10, 9)]        // 1 -> 10 (9 kr saved)
    public void CalculateRoundUp_ReturnsCorrectAmount(double amount, int roundUpTo, double expected)
    {
        // Act
        var result = _service.CalculateRoundUp((decimal)amount, (decimal)roundUpTo);

        // Assert
        Assert.AreEqual((decimal)expected, result);
    }

    [TestMethod]
    public async Task ProcessRoundUpForTransactionAsync_CreatesRoundUpTransaction_WhenEnabled()
    {
        // Arrange
        var goal = new Goal
        {
            Name = "Test Goal",
            TargetAmount = 1000,
            CurrentAmount = 0,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        var settings = new RoundUpSettings
        {
            UserId = TestUserId,
            IsEnabled = true,
            RoundUpAmount = 10M,
            TargetGoalId = goal.GoalId,
            OnlyExpenses = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RoundUpSettings.Add(settings);

        var transaction = new Transaction
        {
            Amount = 127M,
            Description = "ICA Purchase",
            Date = DateTime.UtcNow,
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var roundUp = await _service.ProcessRoundUpForTransactionAsync(transaction.TransactionId);

        // Assert
        Assert.IsNotNull(roundUp);
        Assert.AreEqual(127M, roundUp.OriginalAmount);
        Assert.AreEqual(130M, roundUp.RoundedAmount);
        Assert.AreEqual(3M, roundUp.RoundUpAmount);
        Assert.AreEqual(3M, roundUp.TotalSaved);
        Assert.AreEqual(goal.GoalId, roundUp.GoalId);
        Assert.AreEqual(RoundUpType.StandardRoundUp, roundUp.Type);

        // Verify goal was updated
        var updatedGoal = await _context.Goals.FindAsync(goal.GoalId);
        Assert.IsNotNull(updatedGoal);
        Assert.AreEqual(3M, updatedGoal.CurrentAmount);
    }

    [TestMethod]
    public async Task ProcessRoundUpForTransactionAsync_ReturnsNull_WhenDisabled()
    {
        // Arrange
        var settings = new RoundUpSettings
        {
            UserId = TestUserId,
            IsEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RoundUpSettings.Add(settings);

        var transaction = new Transaction
        {
            Amount = 127M,
            Description = "Test",
            Date = DateTime.UtcNow,
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var roundUp = await _service.ProcessRoundUpForTransactionAsync(transaction.TransactionId);

        // Assert
        Assert.IsNull(roundUp);
    }

    [TestMethod]
    public async Task ProcessRoundUpForTransactionAsync_SkipsIncomeTransactions_WhenOnlyExpensesEnabled()
    {
        // Arrange
        var goal = new Goal
        {
            Name = "Test Goal",
            TargetAmount = 1000,
            CurrentAmount = 0,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        var settings = new RoundUpSettings
        {
            UserId = TestUserId,
            IsEnabled = true,
            RoundUpAmount = 10M,
            TargetGoalId = goal.GoalId,
            OnlyExpenses = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RoundUpSettings.Add(settings);

        var transaction = new Transaction
        {
            Amount = 5000M,
            Description = "Salary",
            Date = DateTime.UtcNow,
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var roundUp = await _service.ProcessRoundUpForTransactionAsync(transaction.TransactionId);

        // Assert
        Assert.IsNull(roundUp);
    }

    [TestMethod]
    public async Task ProcessRoundUpForTransactionAsync_IncludesEmployerMatching_WhenEnabled()
    {
        // Arrange
        var goal = new Goal
        {
            Name = "Test Goal",
            TargetAmount = 1000,
            CurrentAmount = 0,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        var settings = new RoundUpSettings
        {
            UserId = TestUserId,
            IsEnabled = true,
            RoundUpAmount = 10M,
            TargetGoalId = goal.GoalId,
            EnableEmployerMatching = true,
            EmployerMatchingPercentage = 100M, // 100% = doubles the amount
            CreatedAt = DateTime.UtcNow
        };
        _context.RoundUpSettings.Add(settings);

        var transaction = new Transaction
        {
            Amount = 127M,
            Description = "ICA Purchase",
            Date = DateTime.UtcNow,
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var roundUp = await _service.ProcessRoundUpForTransactionAsync(transaction.TransactionId);

        // Assert
        Assert.IsNotNull(roundUp);
        Assert.AreEqual(3M, roundUp.RoundUpAmount);
        Assert.AreEqual(3M, roundUp.EmployerMatchingAmount); // 100% of 3 kr
        Assert.AreEqual(6M, roundUp.TotalSaved); // 3 + 3

        // Verify goal was updated with total
        var updatedGoal = await _context.Goals.FindAsync(goal.GoalId);
        Assert.IsNotNull(updatedGoal);
        Assert.AreEqual(6M, updatedGoal.CurrentAmount);
    }

    [TestMethod]
    public async Task ProcessSalaryAutoSaveAsync_CreatesSalaryAutoSave_WhenEnabled()
    {
        // Arrange
        var goal = new Goal
        {
            Name = "Test Goal",
            TargetAmount = 10000,
            CurrentAmount = 0,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        var settings = new RoundUpSettings
        {
            UserId = TestUserId,
            EnableSalaryAutoSave = true,
            SalaryAutoSavePercentage = 10M,
            TargetGoalId = goal.GoalId,
            CreatedAt = DateTime.UtcNow
        };
        _context.RoundUpSettings.Add(settings);

        var transaction = new Transaction
        {
            Amount = 5000M,
            Description = "Salary",
            Date = DateTime.UtcNow,
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var autoSave = await _service.ProcessSalaryAutoSaveAsync(transaction.TransactionId);

        // Assert
        Assert.IsNotNull(autoSave);
        Assert.AreEqual(5000M, autoSave.OriginalAmount);
        Assert.AreEqual(500M, autoSave.RoundUpAmount); // 10% of 5000
        Assert.AreEqual(500M, autoSave.TotalSaved);
        Assert.AreEqual(RoundUpType.SalaryAutoSave, autoSave.Type);

        // Verify goal was updated
        var updatedGoal = await _context.Goals.FindAsync(goal.GoalId);
        Assert.IsNotNull(updatedGoal);
        Assert.AreEqual(500M, updatedGoal.CurrentAmount);
    }

    [TestMethod]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var fromDate = new DateTime(2024, 1, 1);
        var toDate = new DateTime(2024, 1, 31);

        var roundUps = new List<RoundUpTransaction>
        {
            new RoundUpTransaction
            {
                OriginalAmount = 127M,
                RoundedAmount = 130M,
                RoundUpAmount = 3M,
                EmployerMatchingAmount = 3M,
                TotalSaved = 6M,
                Type = RoundUpType.StandardRoundUp,
                CreatedAt = new DateTime(2024, 1, 15),
                UserId = TestUserId
            },
            new RoundUpTransaction
            {
                OriginalAmount = 245M,
                RoundedAmount = 250M,
                RoundUpAmount = 5M,
                EmployerMatchingAmount = 5M,
                TotalSaved = 10M,
                Type = RoundUpType.StandardRoundUp,
                CreatedAt = new DateTime(2024, 1, 20),
                UserId = TestUserId
            },
            new RoundUpTransaction
            {
                OriginalAmount = 5000M,
                RoundedAmount = 5000M,
                RoundUpAmount = 500M,
                EmployerMatchingAmount = 0M,
                TotalSaved = 500M,
                Type = RoundUpType.SalaryAutoSave,
                CreatedAt = new DateTime(2024, 1, 25),
                UserId = TestUserId
            }
        };

        _context.RoundUpTransactions.AddRange(roundUps);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync(fromDate, toDate);

        // Assert
        Assert.AreEqual(8M, stats.TotalRoundUp); // 3 + 5
        Assert.AreEqual(8M, stats.TotalEmployerMatching); // 3 + 5
        Assert.AreEqual(500M, stats.TotalSalaryAutoSave);
        Assert.AreEqual(516M, stats.TotalSaved); // 6 + 10 + 500
        Assert.AreEqual(3, stats.TransactionCount);
        Assert.AreEqual(4M, stats.AverageRoundUp); // (3 + 5) / 2
        Assert.AreEqual(5M, stats.LargestRoundUp);
    }

    [TestMethod]
    public async Task GetMonthlyTotalAsync_ReturnsCurrentMonthTotal()
    {
        // Arrange
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        
        var roundUps = new List<RoundUpTransaction>
        {
            new RoundUpTransaction
            {
                RoundUpAmount = 3M,
                TotalSaved = 6M,
                Type = RoundUpType.StandardRoundUp,
                CreatedAt = currentMonth.AddDays(5),
                UserId = TestUserId
            },
            new RoundUpTransaction
            {
                RoundUpAmount = 5M,
                TotalSaved = 10M,
                Type = RoundUpType.StandardRoundUp,
                CreatedAt = currentMonth.AddDays(10),
                UserId = TestUserId
            },
            // This one should not be included (previous month)
            new RoundUpTransaction
            {
                RoundUpAmount = 100M,
                TotalSaved = 200M,
                Type = RoundUpType.StandardRoundUp,
                CreatedAt = currentMonth.AddMonths(-1),
                UserId = TestUserId
            }
        };

        _context.RoundUpTransactions.AddRange(roundUps);
        await _context.SaveChangesAsync();

        // Act
        var total = await _service.GetMonthlyTotalAsync();

        // Assert
        Assert.AreEqual(16M, total); // 6 + 10
    }
}
