using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BudgetTemplateServiceTests
{
    private readonly List<Category> _categories;

    public BudgetTemplateServiceTests()
    {
        _categories = new List<Category>
        {
            new Category { CategoryId = 1, Name = "Mat & Dryck", Color = "#4CAF50" },
            new Category { CategoryId = 2, Name = "Transport", Color = "#2196F3" },
            new Category { CategoryId = 3, Name = "Boende", Color = "#9C27B0" },
            new Category { CategoryId = 4, Name = "Nöje", Color = "#FF9800" },
            new Category { CategoryId = 5, Name = "Shopping", Color = "#E91E63" },
            new Category { CategoryId = 6, Name = "Hälsa", Color = "#00BCD4" },
            new Category { CategoryId = 7, Name = "Lön", Color = "#4CAF50" },
            new Category { CategoryId = 8, Name = "Sparande", Color = "#8BC34A" },
            new Category { CategoryId = 9, Name = "Övrigt", Color = "#9E9E9E" },
            new Category { CategoryId = 10, Name = "Restaurang", Color = "#FF5722" },
            new Category { CategoryId = 11, Name = "Försäkring", Color = "#3F51B5" },
            new Category { CategoryId = 12, Name = "El", Color = "#FFEB3B" },
            new Category { CategoryId = 13, Name = "Barn", Color = "#E91E63" },
            new Category { CategoryId = 14, Name = "Buffert", Color = "#607D8B" }
        };
    }

    [TestMethod]
    public void ApplyTemplateSwedishFamilyAllocatesCorrectPercentages()
    {
        // Arrange
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);

        // Assert
        // Verify key categories get correct allocations
        var boende = result[3]; // Boende category
        var sparande = result[8]; // Sparande category
        
        Assert.AreEqual(9000m, boende); // 30% of 30000
        Assert.AreEqual(4500m, sparande); // 15% of 30000
    }

    [TestMethod]
    public void ApplyTemplateSwedishFamilyTreatsSavingsAsMonthlyCost()
    {
        // Arrange - Verify that savings is treated as a significant monthly cost
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);

        // Assert
        var sparande = result[8]; // Sparande category
        
        // Should allocate 15% to savings as a monthly cost
        Assert.AreEqual(4500m, sparande);
        
        // Verify it's a significant portion
        Assert.IsTrue(sparande > totalIncome * 0.10m, "Savings should be at least 10% of income");
    }

    [TestMethod]
    public void ApplyTemplateSwedishFamilySeparatesFoodCategories()
    {
        // Arrange
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);

        // Assert
        var mat = result[1]; // Mat & Dryck (groceries)
        var restaurang = result[10]; // Restaurang
        
        // Groceries should get 15% (4500 kr)
        Assert.AreEqual(4500m, mat);
        
        // Restaurant should get 5% (1500 kr)
        Assert.AreEqual(1500m, restaurang);
        
        // Total food budget should be 20%
        Assert.AreEqual(6000m, mat + restaurang);
    }

    [TestMethod]
    public void ApplyTemplateSwedishSingleAllocatesHigherSavingsRate()
    {
        // Arrange
        decimal totalIncome = 25000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishSingle, totalIncome, _categories);

        // Assert
        var sparande = result[8]; // Sparande category
        
        // Singles should have 20% savings rate
        Assert.AreEqual(5000m, sparande);
        
        // Verify it's higher than family template would give
        var familyResult = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);
        Assert.IsTrue(sparande > familyResult[8], "Single household should save more than family");
    }

    [TestMethod]
    public void ApplyTemplateSwedishSingleLowerHousingCosts()
    {
        // Arrange
        decimal totalIncome = 25000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishSingle, totalIncome, _categories);

        // Assert
        var boende = result[3]; // Boende category
        
        // Singles should have 28% housing costs (lower than family 30%)
        Assert.AreEqual(7000m, boende);
    }

    [TestMethod]
    public void ApplyTemplateCustomReturnsAllZeros()
    {
        // Arrange
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.Custom, totalIncome, _categories);

        // Assert
        foreach (var amount in result.Values) { Assert.AreEqual(0m, amount); }
    }

    [TestMethod]
    public void ApplyTemplateFiftyThirtyTwentyAllocatesCorrectly()
    {
        // Arrange
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.FiftyThirtyTwenty, totalIncome, _categories);

        // Assert
        var sparande = result[8]; // Sparande should get 20% = 6000
        
        Assert.AreEqual(6000m, sparande);
    }

    [DataTestMethod]
    [DataRow(20000)]
    [DataRow(30000)]
    [DataRow(40000)]
    [DataRow(50000)]
    public void ApplyTemplateSwedishFamilyWorksWithVariousIncomes(int income)
    {
        // Act
        decimal totalIncome = income;
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);

        // Assert
        var total = result.Values.Sum();
        
        // Templates provide suggestions and may allocate more or less than 100%
        // Users are expected to adjust. Just verify it's a reasonable starting point.
        Assert.IsTrue(total > 0, "Template should allocate some amount");
        Assert.IsTrue(total > income * 0.5m, "Template should allocate at least 50% of income");
    }

    [TestMethod]
    public void GetTemplateDescriptionSwedishFamilyReturnsCorrectDescription()
    {
        // Act
        var description = BudgetTemplateService.GetTemplateDescription(BudgetTemplateType.SwedishFamily);

        // Assert
        StringAssert.Contains(description, "Svenska Familjehushåll");
        StringAssert.Contains(description, "Länsförsäkringar");
        StringAssert.Contains(description, "15% sparande");
    }

    [TestMethod]
    public void GetTemplateDescriptionSwedishSingleReturnsCorrectDescription()
    {
        // Act
        var description = BudgetTemplateService.GetTemplateDescription(BudgetTemplateType.SwedishSingle);

        // Assert
        StringAssert.Contains(description, "Svenska Singelhushåll");
        StringAssert.Contains(description, "20%");
    }

    [TestMethod]
    public void ApplyTemplateSwedishFamilyIncludesChildrenCategory()
    {
        // Arrange
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);

        // Assert
        var barn = result[13]; // Barn category
        
        // Should allocate 5% for children activities (1500 kr)
        Assert.AreEqual(1500m, barn);
    }

    [TestMethod]
    public void ApplyTemplateSwedishFamilyIncludesBuffer()
    {
        // Arrange
        decimal totalIncome = 30000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishFamily, totalIncome, _categories);

        // Assert
        var buffert = result[14]; // Buffert category
        
        // Should allocate 6% for buffer (1800 kr)
        Assert.AreEqual(1800m, buffert);
    }

    [TestMethod]
    public void ApplyTemplateSwedishSingleHigherBuffer()
    {
        // Arrange
        decimal totalIncome = 25000m;

        // Act
        var result = BudgetTemplateService.ApplyTemplate(BudgetTemplateType.SwedishSingle, totalIncome, _categories);

        // Assert
        var buffert = result[14]; // Buffert category
        
        // Should allocate 9.5% for buffer (2375 kr)
        Assert.AreEqual(2375m, buffert);
    }
}
