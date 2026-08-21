using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BudgetSuggestionServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly BudgetSuggestionService _service;
    private readonly List<Category> _testCategories;

    public BudgetSuggestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new PrivatekonomyContext(options);

        _mockCategoryService = new Mock<ICategoryService>();
        _mockTransactionService = new Mock<ITransactionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(c => c.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(c => c.UserId).Returns("test-user-id");

        _testCategories = new List<Category>
        {
            new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#4CAF50" },
            new Category { CategoryId = 2, Name = "Transport", Color = "#2196F3" },
            new Category { CategoryId = 3, Name = "Boende", Color = "#9C27B0" },
            new Category { CategoryId = 4, Name = "Nöje", Color = "#FF9800" },
            new Category { CategoryId = 5, Name = "Shopping", Color = "#E91E63" },
            new Category { CategoryId = 6, Name = "Hälsa", Color = "#00BCD4" },
            new Category { CategoryId = 7, Name = "Sparande", Color = "#8BC34A" },
            new Category { CategoryId = 8, Name = "Övrigt", Color = "#9E9E9E" },
            new Category { CategoryId = 9, Name = "Restaurang", Color = "#FF5722" },
            new Category { CategoryId = 10, Name = "Försäkring", Color = "#3F51B5" }
        };

        _context.Categories.AddRange(_testCategories);
        _context.SaveChanges();

        _mockCategoryService.Setup(c => c.GetAllCategoriesAsync())
            .ReturnsAsync(_testCategories);

        _service = new BudgetSuggestionService(
            _context,
            _mockCategoryService.Object,
            _mockTransactionService.Object,
            _mockCurrentUserService.Object);
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
    public async Task GenerateSuggestionAsync_FiftyThirtyTwenty_CreatesCorrectSuggestion()
    {
        // Arrange
        var totalIncome = 30000m;
        var model = BudgetDistributionModel.FiftyThirtyTwenty;

        // Act
        var suggestion = await _service.GenerateSuggestionAsync(totalIncome, model);

        // Assert
        Assert.IsNotNull(suggestion);
        Assert.AreEqual(totalIncome, suggestion.TotalIncome);
        Assert.AreEqual(model, suggestion.DistributionModel);
        Assert.IsFalse(suggestion.IsAccepted);
        Assert.IsTrue(suggestion.Items.Any());
    }

    [TestMethod]
    public async Task GenerateSuggestionAsync_FiftyThirtyTwenty_AllocatesSavingsCorrectly()
    {
        // Arrange
        var totalIncome = 30000m;
        var model = BudgetDistributionModel.FiftyThirtyTwenty;

        // Act
        var suggestion = await _service.GenerateSuggestionAsync(totalIncome, model);

        // Assert
        var savingsItem = suggestion.Items.FirstOrDefault(i => 
            i.Category?.Name.Contains("Sparande") == true);
        
        Assert.IsNotNull(savingsItem);
        Assert.AreEqual(6000m, savingsItem.SuggestedAmount); // 20% of 30000
        Assert.AreEqual(BudgetAllocationCategory.Savings, savingsItem.AllocationCategory);
    }

    [TestMethod]
    public async Task GenerateSuggestionAsync_SwedishFamily_Allocates15PercentToSavings()
    {
        // Arrange
        var totalIncome = 30000m;
        var model = BudgetDistributionModel.SwedishFamily;

        // Act
        var suggestion = await _service.GenerateSuggestionAsync(totalIncome, model);

        // Assert
        var savingsItem = suggestion.Items.FirstOrDefault(i => 
            i.Category?.Name.Contains("Sparande") == true);
        
        Assert.IsNotNull(savingsItem);
        Assert.AreEqual(4500m, savingsItem.SuggestedAmount); // 15% of 30000
    }

    [TestMethod]
    public async Task GenerateSuggestionAsync_SwedishSingle_Allocates20PercentToSavings()
    {
        // Arrange
        var totalIncome = 25000m;
        var model = BudgetDistributionModel.SwedishSingle;

        // Act
        var suggestion = await _service.GenerateSuggestionAsync(totalIncome, model);

        // Assert
        var savingsItem = suggestion.Items.FirstOrDefault(i => 
            i.Category?.Name.Contains("Sparande") == true);
        
        Assert.IsNotNull(savingsItem);
        Assert.AreEqual(5000m, savingsItem.SuggestedAmount); // 20% of 25000
    }

    [TestMethod]
    public async Task GenerateSuggestionAsync_SetsCorrectAllocationCategories()
    {
        // Arrange
        var totalIncome = 30000m;
        var model = BudgetDistributionModel.FiftyThirtyTwenty;

        // Act
        var suggestion = await _service.GenerateSuggestionAsync(totalIncome, model);

        // Assert
        var housingItem = suggestion.Items.FirstOrDefault(i => 
            i.Category?.Name.Contains("Boende") == true);
        var entertainmentItem = suggestion.Items.FirstOrDefault(i => 
            i.Category?.Name.Contains("Nöje") == true);
        var savingsItem = suggestion.Items.FirstOrDefault(i => 
            i.Category?.Name.Contains("Sparande") == true);

        Assert.IsNotNull(housingItem);
        Assert.AreEqual(BudgetAllocationCategory.Needs, housingItem.AllocationCategory);

        Assert.IsNotNull(entertainmentItem);
        Assert.AreEqual(BudgetAllocationCategory.Wants, entertainmentItem.AllocationCategory);

        Assert.IsNotNull(savingsItem);
        Assert.AreEqual(BudgetAllocationCategory.Savings, savingsItem.AllocationCategory);
    }

    [TestMethod]
    public async Task AdjustSuggestionItemAsync_UpdatesAmount()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var itemToAdjust = suggestion.Items.First();
        var newAmount = 5000m;

        // Act
        var adjustedItem = await _service.AdjustSuggestionItemAsync(
            suggestion.BudgetSuggestionId,
            itemToAdjust.CategoryId,
            newAmount,
            "Test adjustment");

        // Assert
        Assert.AreEqual(newAmount, adjustedItem.AdjustedAmount);
        Assert.IsTrue(adjustedItem.IsManuallyAdjusted);
    }

    [TestMethod]
    public async Task AdjustSuggestionItemAsync_RecordsAdjustment()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var itemToAdjust = suggestion.Items.First();
        var newAmount = 5000m;

        // Act
        await _service.AdjustSuggestionItemAsync(
            suggestion.BudgetSuggestionId,
            itemToAdjust.CategoryId,
            newAmount,
            "Test adjustment");

        // Assert
        var updatedSuggestion = await _service.GetSuggestionByIdAsync(suggestion.BudgetSuggestionId);
        Assert.AreEqual(1, updatedSuggestion!.Adjustments.Count());
        Assert.AreEqual(AdjustmentType.Modification, updatedSuggestion.Adjustments.First().Type);
    }

    [TestMethod]
    public async Task TransferBetweenItemsAsync_TransfersCorrectly()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var fromItem = suggestion.Items.First(i => i.AdjustedAmount >= 1000);
        var toItem = suggestion.Items.First(i => i.CategoryId != fromItem.CategoryId);
        var originalFromAmount = fromItem.AdjustedAmount;
        var originalToAmount = toItem.AdjustedAmount;
        var transferAmount = 500m;

        // Act
        await _service.TransferBetweenItemsAsync(
            suggestion.BudgetSuggestionId,
            fromItem.CategoryId,
            toItem.CategoryId,
            transferAmount);

        // Assert
        var updatedSuggestion = await _service.GetSuggestionByIdAsync(suggestion.BudgetSuggestionId);
        var updatedFromItem = updatedSuggestion!.Items.First(i => i.CategoryId == fromItem.CategoryId);
        var updatedToItem = updatedSuggestion.Items.First(i => i.CategoryId == toItem.CategoryId);

        Assert.AreEqual(originalFromAmount - transferAmount, updatedFromItem.AdjustedAmount);
        Assert.AreEqual(originalToAmount + transferAmount, updatedToItem.AdjustedAmount);
    }

    [TestMethod]
    public async Task TransferBetweenItemsAsync_RecordsAdjustment()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var fromItem = suggestion.Items.First(i => i.AdjustedAmount >= 1000);
        var toItem = suggestion.Items.First(i => i.CategoryId != fromItem.CategoryId);

        // Act
        await _service.TransferBetweenItemsAsync(
            suggestion.BudgetSuggestionId,
            fromItem.CategoryId,
            toItem.CategoryId,
            500m);

        // Assert
        var updatedSuggestion = await _service.GetSuggestionByIdAsync(suggestion.BudgetSuggestionId);
        Assert.AreEqual(1, updatedSuggestion!.Adjustments.Count());
        Assert.AreEqual(AdjustmentType.Transfer, updatedSuggestion.Adjustments.First().Type);
    }

    [TestMethod]
    public async Task AcceptSuggestionAsync_CreatesBudget()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        // Act
        var budget = await _service.AcceptSuggestionAsync(
            suggestion.BudgetSuggestionId,
            startDate,
            endDate,
            BudgetPeriod.Monthly);

        // Assert
        Assert.IsNotNull(budget);
        Assert.AreEqual(startDate, budget.StartDate);
        Assert.AreEqual(endDate, budget.EndDate);
        Assert.AreEqual(BudgetPeriod.Monthly, budget.Period);
        Assert.IsTrue(budget.BudgetCategories.Any());
    }

    [TestMethod]
    public async Task AcceptSuggestionAsync_MarksSuggestionAsAccepted()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);

        // Act
        await _service.AcceptSuggestionAsync(
            suggestion.BudgetSuggestionId,
            DateTime.Now,
            DateTime.Now.AddMonths(1),
            BudgetPeriod.Monthly);

        // Assert
        var updatedSuggestion = await _service.GetSuggestionByIdAsync(suggestion.BudgetSuggestionId);
        Assert.IsNotNull(updatedSuggestion);
        Assert.IsTrue(updatedSuggestion.IsAccepted);
        Assert.IsNotNull(updatedSuggestion.AcceptedAt);
    }

    [TestMethod]
    public async Task AcceptSuggestionAsync_ThrowsIfAlreadyAccepted()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        await _service.AcceptSuggestionAsync(
            suggestion.BudgetSuggestionId,
            DateTime.Now,
            DateTime.Now.AddMonths(1),
            BudgetPeriod.Monthly);

        // Act & Assert
        try
        {
            await _service.AcceptSuggestionAsync(
                suggestion.BudgetSuggestionId,
                DateTime.Now,
                DateTime.Now.AddMonths(1),
                BudgetPeriod.Monthly);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task CalculateEffectsAsync_CalculatesCorrectTotals()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);

        // Act
        var effects = await _service.CalculateEffectsAsync(suggestion.BudgetSuggestionId);

        // Assert
        Assert.IsNotNull(effects);
        Assert.AreEqual(effects.TotalOriginalAmount, effects.TotalAdjustedAmount);
        Assert.AreEqual(0, effects.TotalDifference);
    }

    [TestMethod]
    public async Task CalculateEffectsAsync_TracksAdjustmentCount()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var item = suggestion.Items.First();
        
        // Make an adjustment
        await _service.AdjustSuggestionItemAsync(
            suggestion.BudgetSuggestionId,
            item.CategoryId,
            item.AdjustedAmount + 500m);

        // Act
        var effects = await _service.CalculateEffectsAsync(suggestion.BudgetSuggestionId);

        // Assert
        Assert.AreEqual(1, effects.AdjustmentsCount);
    }

    [TestMethod]
    public async Task GetPendingSuggestionsAsync_ReturnsOnlyPending()
    {
        // Arrange
        await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);
        var acceptedSuggestion = await _service.GenerateSuggestionAsync(25000m, BudgetDistributionModel.SwedishSingle);
        await _service.AcceptSuggestionAsync(
            acceptedSuggestion.BudgetSuggestionId,
            DateTime.Now,
            DateTime.Now.AddMonths(1),
            BudgetPeriod.Monthly);

        // Act
        var pendingSuggestions = await _service.GetPendingSuggestionsAsync();

        // Assert
        Assert.AreEqual(1, pendingSuggestions.Count());
        foreach (var s in pendingSuggestions) { Assert.IsFalse(s.IsAccepted); }
    }

    [TestMethod]
    public async Task DeleteSuggestionAsync_RemovesSuggestion()
    {
        // Arrange
        var suggestion = await _service.GenerateSuggestionAsync(30000m, BudgetDistributionModel.FiftyThirtyTwenty);

        // Act
        await _service.DeleteSuggestionAsync(suggestion.BudgetSuggestionId);

        // Assert
        var deletedSuggestion = await _service.GetSuggestionByIdAsync(suggestion.BudgetSuggestionId);
        Assert.IsNull(deletedSuggestion);
    }

    [TestMethod]
    public void GetAvailableModels_ReturnsAllModels()
    {
        // Act
        var models = _service.GetAvailableModels().ToList();

        // Assert
        Assert.IsTrue(models.Count >= 5); // Should have at least 5 models
        Assert.IsTrue(models.Any(m => m.Model == BudgetDistributionModel.FiftyThirtyTwenty));
        Assert.IsTrue(models.Any(m => m.Model == BudgetDistributionModel.SwedishFamily));
        Assert.IsTrue(models.Any(m => m.Model == BudgetDistributionModel.SwedishSingle));
    }

    [DataTestMethod]
    [DataRow(BudgetDistributionModel.FiftyThirtyTwenty)]
    [DataRow(BudgetDistributionModel.SwedishFamily)]
    [DataRow(BudgetDistributionModel.SwedishSingle)]
    [DataRow(BudgetDistributionModel.EightyTwenty)]
    [DataRow(BudgetDistributionModel.SeventyTwentyTen)]
    public void GetDistributionModelDescription_ReturnsNonEmptyDescription(BudgetDistributionModel model)
    {
        // Act
        var description = _service.GetDistributionModelDescription(model);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(description));
    }

    [DataTestMethod]
    [DataRow(20000)]
    [DataRow(30000)]
    [DataRow(40000)]
    [DataRow(50000)]
    public async Task GenerateSuggestionAsync_WorksWithVariousIncomes(decimal income)
    {
        // Act
        var suggestion = await _service.GenerateSuggestionAsync(income, BudgetDistributionModel.FiftyThirtyTwenty);

        // Assert
        Assert.IsNotNull(suggestion);
        Assert.AreEqual(income, suggestion.TotalIncome);
        Assert.IsTrue(suggestion.Items.Any());
        Assert.IsTrue(suggestion.Items.Sum(i => i.SuggestedAmount) > 0);
    }
}
