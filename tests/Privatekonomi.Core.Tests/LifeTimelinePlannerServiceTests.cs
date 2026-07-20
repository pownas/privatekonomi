using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class LifeTimelinePlannerServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly LifeTimelinePlannerService _service;

    public LifeTimelinePlannerServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _service = new LifeTimelinePlannerService(_context, null);
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

    #region Milestone Tests

    [TestMethod]
    public async Task CreateMilestoneAsync_ValidMilestone_SuccessfullyCreatesMilestone()
    {
        // Arrange
        var milestone = new LifeTimelineMilestone
        {
            Name = "Köpa bostad",
            Description = "Köpa första bostaden",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddYears(5),
            EstimatedCost = 1500000m,
            CurrentSavings = 200000m,
            Priority = 1
        };

        // Act
        var result = await _service.CreateMilestoneAsync(milestone);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.LifeTimelineMilestoneId > 0);
        Assert.AreEqual("Köpa bostad", result.Name);
        Assert.AreEqual(1500000m, result.EstimatedCost);
    }

    [TestMethod]
    public async Task GetAllMilestonesAsync_ReturnsMilestones()
    {
        // Arrange
        var milestone1 = new LifeTimelineMilestone
        {
            Name = "Milestone 1",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddYears(2),
            EstimatedCost = 1000000m,
            CreatedAt = DateTime.UtcNow
        };
        var milestone2 = new LifeTimelineMilestone
        {
            Name = "Milestone 2",
            MilestoneType = "Child",
            PlannedDate = DateTime.UtcNow.AddYears(3),
            EstimatedCost = 500000m,
            CreatedAt = DateTime.UtcNow
        };

        await _service.CreateMilestoneAsync(milestone1);
        await _service.CreateMilestoneAsync(milestone2);

        // Act
        var result = await _service.GetAllMilestonesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task UpdateMilestoneAsync_ValidMilestone_UpdatesSuccessfully()
    {
        // Arrange
        var milestone = new LifeTimelineMilestone
        {
            Name = "Original Name",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddYears(5),
            EstimatedCost = 1000000m,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateMilestoneAsync(milestone);
        created.Name = "Updated Name";
        created.EstimatedCost = 1500000m;

        // Act
        var result = await _service.UpdateMilestoneAsync(created);

        // Assert
        Assert.AreEqual("Updated Name", result.Name);
        Assert.AreEqual(1500000m, result.EstimatedCost);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task DeleteMilestoneAsync_ExistingMilestone_DeletesSuccessfully()
    {
        // Arrange
        var milestone = new LifeTimelineMilestone
        {
            Name = "To Delete",
            MilestoneType = "Other",
            PlannedDate = DateTime.UtcNow.AddYears(1),
            EstimatedCost = 50000m,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateMilestoneAsync(milestone);

        // Act
        await _service.DeleteMilestoneAsync(created.LifeTimelineMilestoneId);
        var result = await _service.GetMilestoneByIdAsync(created.LifeTimelineMilestoneId);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region Scenario Tests

    [TestMethod]
    public async Task CreateScenarioAsync_ValidScenario_SuccessfullyCreatesScenario()
    {
        // Arrange
        var scenario = new LifeTimelineScenario
        {
            Name = "Optimistisk",
            Description = "Optimistiskt scenario med hög avkastning",
            MonthlySavings = 5000m,
            ExpectedReturnRate = 8m,
            RetirementAge = 65,
            InflationRate = 2m,
            SalaryIncreaseRate = 3m
        };

        // Act
        var result = await _service.CreateScenarioAsync(scenario);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.LifeTimelineScenarioId > 0);
        Assert.AreEqual("Optimistisk", result.Name);
        Assert.AreEqual(5000m, result.MonthlySavings);
    }

    [TestMethod]
    public async Task SetActiveScenarioAsync_ValidScenario_SetsActiveCorrectly()
    {
        // Arrange
        var scenario1 = new LifeTimelineScenario
        {
            Name = "Scenario 1",
            MonthlySavings = 4000m,
            ExpectedReturnRate = 7m,
            RetirementAge = 65,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var scenario2 = new LifeTimelineScenario
        {
            Name = "Scenario 2",
            MonthlySavings = 5000m,
            ExpectedReturnRate = 8m,
            RetirementAge = 65,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var created1 = await _service.CreateScenarioAsync(scenario1);
        var created2 = await _service.CreateScenarioAsync(scenario2);

        // Act
        await _service.SetActiveScenarioAsync(created2.LifeTimelineScenarioId);

        // Assert
        var activeScenario = await _service.GetActiveScenarioAsync();
        Assert.IsNotNull(activeScenario);
        Assert.AreEqual(created2.LifeTimelineScenarioId, activeScenario.LifeTimelineScenarioId);
        Assert.IsTrue(activeScenario.IsActive);
        
        var scenario1Updated = await _service.GetScenarioByIdAsync(created1.LifeTimelineScenarioId);
        Assert.IsFalse(scenario1Updated!.IsActive);
    }

    #endregion

    #region Calculation Tests

    [TestMethod]
    public async Task CalculateRequiredMonthlySavingsAsync_ValidMilestone_ReturnsCorrectAmount()
    {
        // Arrange
        var milestone = new LifeTimelineMilestone
        {
            Name = "Test Milestone",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddMonths(12),
            EstimatedCost = 120000m,
            CurrentSavings = 0m,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateMilestoneAsync(milestone);

        // Act
        var result = await _service.CalculateRequiredMonthlySavingsAsync(created.LifeTimelineMilestoneId);

        // Assert
        Assert.IsTrue(result > 0, $"Result should be greater than 0, but was {result}");
        Assert.IsTrue(result <= 11000m, $"Result should be around 10000 kr/month for 12 months, but was {result}"); // Allow some margin for calculation
    }

    [TestMethod]
    public async Task CalculateRequiredMonthlySavingsAsync_CompletedMilestone_ReturnsZero()
    {
        // Arrange
        var milestone = new LifeTimelineMilestone
        {
            Name = "Test Milestone",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddMonths(12),
            EstimatedCost = 100000m,
            CurrentSavings = 150000m, // Already saved more than needed
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateMilestoneAsync(milestone);

        // Act
        var result = await _service.CalculateRequiredMonthlySavingsAsync(created.LifeTimelineMilestoneId);

        // Assert
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public async Task CalculateProjectedRetirementWealthAsync_ValidScenario_ReturnsPositiveValue()
    {
        // Arrange
        var scenario = new LifeTimelineScenario
        {
            Name = "Test Scenario",
            MonthlySavings = 5000m,
            ExpectedReturnRate = 7m,
            RetirementAge = 65,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateScenarioAsync(scenario);

        // Act
        var result = await _service.CalculateProjectedRetirementWealthAsync(created.LifeTimelineScenarioId);

        // Assert
        Assert.IsTrue(result > 0);
        // With compound interest, the result should be significantly higher than just monthly savings * months
    }

    [TestMethod]
    public async Task GetTotalMilestoneCostsAsync_MultipleMilestones_ReturnsSumOfCosts()
    {
        // Arrange
        var milestone1 = new LifeTimelineMilestone
        {
            Name = "Milestone 1",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddYears(2),
            EstimatedCost = 1000000m,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var milestone2 = new LifeTimelineMilestone
        {
            Name = "Milestone 2",
            MilestoneType = "Child",
            PlannedDate = DateTime.UtcNow.AddYears(3),
            EstimatedCost = 500000m,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var milestone3 = new LifeTimelineMilestone
        {
            Name = "Completed Milestone",
            MilestoneType = "Education",
            PlannedDate = DateTime.UtcNow.AddYears(1),
            EstimatedCost = 200000m,
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow
        };

        await _service.CreateMilestoneAsync(milestone1);
        await _service.CreateMilestoneAsync(milestone2);
        await _service.CreateMilestoneAsync(milestone3);

        // Act
        var result = await _service.GetTotalMilestoneCostsAsync();

        // Assert
        Assert.AreEqual(1500000m, result); // Should only include non-completed milestones
    }

    [TestMethod]
    public async Task CalculateExpectedMonthlyPensionAsync_ValidScenario_ReturnsMonthlyPension()
    {
        // Arrange
        var scenario = new LifeTimelineScenario
        {
            Name = "Test Scenario",
            MonthlySavings = 5000m,
            ExpectedReturnRate = 7.0m,
            RetirementAge = 65,
            IsActive = true
        };

        await _service.CreateScenarioAsync(scenario);

        // Act
        var monthlyPension = await _service.CalculateExpectedMonthlyPensionAsync(scenario.LifeTimelineScenarioId);

        // Assert
        Assert.IsTrue(monthlyPension > 0);
        // With 35 years of saving 5000 kr/month at 7% return, monthly pension should be substantial
        Assert.IsTrue(monthlyPension > 10000m); // Should be at least 10,000 kr/month
    }

    [TestMethod]
    public async Task CalculateLifeInsuranceNeedAsync_WithMilestones_ReturnsPositiveAmount()
    {
        // Arrange
        var milestone1 = new LifeTimelineMilestone
        {
            Name = "Köpa bostad",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddYears(5),
            EstimatedCost = 2000000m,
            CurrentSavings = 500000m
        };

        var milestone2 = new LifeTimelineMilestone
        {
            Name = "Barn",
            MilestoneType = "Child",
            PlannedDate = DateTime.UtcNow.AddYears(3),
            EstimatedCost = 500000m,
            CurrentSavings = 50000m
        };

        var scenario = new LifeTimelineScenario
        {
            Name = "Test Scenario",
            MonthlySavings = 10000m,
            IsActive = true
        };

        await _service.CreateMilestoneAsync(milestone1);
        await _service.CreateMilestoneAsync(milestone2);
        await _service.CreateScenarioAsync(scenario);

        // Act
        var insuranceNeed = await _service.CalculateLifeInsuranceNeedAsync();

        // Assert
        Assert.IsTrue(insuranceNeed > 0);
        // Should include outstanding milestone costs plus income replacement
        Assert.IsTrue(insuranceNeed > 1950000m); // Outstanding costs (1,500,000 + 450,000) 
    }

    [TestMethod]
    public async Task CalculateRecommendedLifeInsuranceAsync_ReturnsRoundedAmount()
    {
        // Arrange
        var milestone = new LifeTimelineMilestone
        {
            Name = "Test",
            MilestoneType = "HousePurchase",
            PlannedDate = DateTime.UtcNow.AddYears(5),
            EstimatedCost = 2500000m,
            CurrentSavings = 0m
        };

        var scenario = new LifeTimelineScenario
        {
            Name = "Test Scenario",
            MonthlySavings = 5000m,
            IsActive = true
        };

        await _service.CreateMilestoneAsync(milestone);
        await _service.CreateScenarioAsync(scenario);

        // Act
        var recommended = await _service.CalculateRecommendedLifeInsuranceAsync();

        // Assert
        Assert.IsTrue(recommended > 0);
        // Should be rounded to nearest 100,000
        Assert.AreEqual(0, recommended % 100000);
    }

    #endregion
}
