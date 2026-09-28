using Moq;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class CashFlowScenarioServiceTests
{
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly Mock<IGoalService> _goalServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CashFlowScenarioService _service;

    public CashFlowScenarioServiceTests()
    {
        _transactionServiceMock = new Mock<ITransactionService>();
        _goalServiceMock = new Mock<IGoalService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        
        _service = new CashFlowScenarioService(
            _transactionServiceMock.Object,
            _goalServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    [TestMethod]
    public async Task CalculateScenarioAsyncSimpleScenarioCalculatesCorrectly()
    {
        // Arrange
        var scenario = new CashFlowScenario
        {
            Name = "Test",
            InitialAmount = 10000m,
            MonthlySavings = 1000m,
            AnnualInterestRate = 12m, // 1% per month for easy calculation
            Years = 1
        };

        // Act
        var result = await _service.CalculateScenarioAsync(scenario);

        // Assert
        Assert.AreEqual("Test", result.ScenarioName);
        Assert.AreEqual(13, result.MonthlyData.Count); // 0 + 12 months
        Assert.IsTrue(result.FinalAmount > 22000m); // Initial + contributions + interest
        Assert.AreEqual(22000m, result.TotalContributions); // Initial + 12 * 1000
        Assert.IsTrue(result.TotalInterest > 0);
    }

    [TestMethod]
    public async Task CalculateScenarioAsyncWithZeroInterestReturnsOnlyContributions()
    {
        // Arrange
        var scenario = new CashFlowScenario
        {
            Name = "No Interest",
            InitialAmount = 10000m,
            MonthlySavings = 1000m,
            AnnualInterestRate = 0m,
            Years = 1
        };

        // Act
        var result = await _service.CalculateScenarioAsync(scenario);

        // Assert
        Assert.AreEqual(22000m, result.FinalAmount); // 10000 + 12*1000
        Assert.AreEqual(22000m, result.TotalContributions);
        Assert.AreEqual(0m, result.TotalInterest);
    }

    [TestMethod]
    public async Task CalculateScenarioAsyncWithExtraContributionAddsExtraCorrectly()
    {
        // Arrange
        var scenario = new CashFlowScenario
        {
            Name = "Extra",
            InitialAmount = 10000m,
            MonthlySavings = 1000m,
            AnnualInterestRate = 0m,
            Years = 1,
            ExtraContribution = 5000m,
            ExtraContributionMonth = 6
        };

        // Act
        var result = await _service.CalculateScenarioAsync(scenario);

        // Assert
        Assert.AreEqual(27000m, result.FinalAmount); // 10000 + 12*1000 + 5000
        Assert.AreEqual(27000m, result.TotalContributions);
        
        // Check that month 6 has the extra contribution
        var month6 = result.MonthlyData.First(m => m.Month == 6);
        Assert.AreEqual(6000m, month6.MonthlyContribution); // 1000 + 5000
    }

    [TestMethod]
    public async Task CalculateScenarioAsyncWithInflationCalculatesRealValue()
    {
        // Arrange
        var scenario = new CashFlowScenario
        {
            Name = "Inflation",
            InitialAmount = 10000m,
            MonthlySavings = 1000m,
            AnnualInterestRate = 0m,
            Years = 1,
            InflationRate = 12m // 1% per month
        };

        // Act
        var result = await _service.CalculateScenarioAsync(scenario);

        // Assert
        Assert.IsNotNull(result.RealValue);
        Assert.IsTrue(result.RealValue < result.FinalAmount); // Real value should be less due to inflation
    }

    [TestMethod]
    public async Task CalculateScenarioAsyncWithAnnualSavingsIncreaseIncreasesContributions()
    {
        // Arrange
        var scenario = new CashFlowScenario
        {
            Name = "Increase",
            InitialAmount = 0m,
            MonthlySavings = 1000m,
            AnnualInterestRate = 0m,
            Years = 2,
            AnnualSavingsIncrease = 10m // 10% increase per year
        };

        // Act
        var result = await _service.CalculateScenarioAsync(scenario);

        // Assert
        // First year: 12 * 1000 = 12000
        // Second year: 12 * 1100 = 13200
        // Total: 25200
        Assert.AreEqual(25200m, result.FinalAmount);
        
        // Check that month 13 has increased contribution
        var month13 = result.MonthlyData.First(m => m.Month == 13);
        Assert.AreEqual(1100m, month13.MonthlyContribution);
    }

    [TestMethod]
    public async Task GetUserBasedDefaultsAsyncWithNoUserReturnsGuestDefaults()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId)
            .Returns((string?)null);
        _currentUserServiceMock.Setup(x => x.IsAuthenticated)
            .Returns(false);

        // Act
        var result = await _service.GetUserBasedDefaultsAsync();

        // Assert
        Assert.AreEqual("Exempel scenario", result.Name);
        Assert.AreEqual(50000m, result.InitialAmount);
        Assert.AreEqual(3000m, result.MonthlySavings);
    }

    [TestMethod]
    public async Task GetUserBasedDefaultsAsyncWithUserCalculatesFromTransactions()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId)
            .Returns("user123");
        _currentUserServiceMock.Setup(x => x.IsAuthenticated)
            .Returns(true);

        var threeMonthsAgo = DateTime.Today.AddMonths(-3);
        var transactions = new List<Transaction>
        {
            // Month 1
            new Transaction { Amount = 30000m, IsIncome = true, Date = threeMonthsAgo.AddDays(5) },
            new Transaction { Amount = 20000m, IsIncome = false, Date = threeMonthsAgo.AddDays(10) },
            // Month 2
            new Transaction { Amount = 32000m, IsIncome = true, Date = threeMonthsAgo.AddMonths(1).AddDays(5) },
            new Transaction { Amount = 18000m, IsIncome = false, Date = threeMonthsAgo.AddMonths(1).AddDays(10) },
            // Month 3
            new Transaction { Amount = 31000m, IsIncome = true, Date = threeMonthsAgo.AddMonths(2).AddDays(5) },
            new Transaction { Amount = 19000m, IsIncome = false, Date = threeMonthsAgo.AddMonths(2).AddDays(10) },
        };

        _transactionServiceMock.Setup(x => x.GetTransactionsByDateRangeAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(transactions);

        var goals = new List<Goal>
        {
            new Goal { CurrentAmount = 10000m },
            new Goal { CurrentAmount = 15000m }
        };

        _goalServiceMock.Setup(x => x.GetActiveGoalsAsync())
            .ReturnsAsync(goals);

        // Act
        var result = await _service.GetUserBasedDefaultsAsync();

        // Assert
        Assert.AreEqual("Mitt scenario", result.Name);
        Assert.AreEqual(25000m, result.InitialAmount); // 10000 + 15000
        // Average income: (30000+32000+31000)/3 = 31000
        // Average expenses: (20000+18000+19000)/3 = 19000
        // Average savings: 31000 - 19000 = 12000
        Assert.AreEqual(12000m, result.MonthlySavings);
    }

    [TestMethod]
    public void GetGuestDefaultsReturnsExpectedDefaults()
    {
        // Act
        var result = _service.GetGuestDefaults();

        // Assert
        Assert.AreEqual("Exempel scenario", result.Name);
        Assert.AreEqual(50000m, result.InitialAmount);
        Assert.AreEqual(3000m, result.MonthlySavings);
        Assert.AreEqual(3.0m, result.AnnualInterestRate);
        Assert.AreEqual(10, result.Years);
        Assert.AreEqual(2.0m, result.InflationRate);
    }

    [TestMethod]
    public async Task CompareMultipleScenariosAsyncReturnsMultipleProjections()
    {
        // Arrange
        var scenarios = new List<CashFlowScenario>
        {
            new CashFlowScenario
            {
                Name = "Scenario 1",
                InitialAmount = 10000m,
                MonthlySavings = 1000m,
                AnnualInterestRate = 3m,
                Years = 5
            },
            new CashFlowScenario
            {
                Name = "Scenario 2",
                InitialAmount = 10000m,
                MonthlySavings = 2000m,
                AnnualInterestRate = 3m,
                Years = 5
            }
        };

        // Act
        var result = await _service.CompareMultipleScenariosAsync(scenarios);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Scenario 1", result[0].ScenarioName);
        Assert.AreEqual("Scenario 2", result[1].ScenarioName);
        Assert.IsTrue(result[1].FinalAmount > result[0].FinalAmount); // Scenario 2 should have more
    }
}
