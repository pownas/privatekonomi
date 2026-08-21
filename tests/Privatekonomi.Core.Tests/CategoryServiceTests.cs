using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class CategoryServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _categoryService = new CategoryService(_context);
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
    public async Task CreateCategoryAsync_GeneratesRandomColorWhenNotProvided()
    {
        // Arrange
        var category = new Category
        {
            Name = "Test Category",
            Color = "#000000" // Default black should be replaced
        };

        // Act
        var result = await _categoryService.CreateCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual("#000000", result.Color);
        StringAssert.StartsWith(result.Color, "#");
        Assert.AreEqual(7, result.Color.Length); // #RRGGBB format
    }

    [TestMethod]
    public async Task CreateCategoryAsync_PreservesProvidedColor()
    {
        // Arrange
        var expectedColor = "#FF6B6B";
        var category = new Category
        {
            Name = "Test Category",
            Color = expectedColor
        };

        // Act
        var result = await _categoryService.CreateCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedColor, result.Color);
    }

    [TestMethod]
    public async Task CreateCategoryAsync_SetsCreatedAt()
    {
        // Arrange
        var category = new Category
        {
            Name = "Test Category",
            Color = "#4ECDC4"
        };

        // Act
        var result = await _categoryService.CreateCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual(DateTime.MinValue, result.CreatedAt);
    }

    [TestMethod]
    public async Task CreateCategoryAsync_CreatesSubcategory()
    {
        // Arrange
        var parentCategory = new Category
        {
            Name = "Parent Category",
            Color = "#FF6B6B"
        };
        await _categoryService.CreateCategoryAsync(parentCategory);

        var subCategory = new Category
        {
            Name = "Sub Category",
            Color = "#4ECDC4",
            ParentId = parentCategory.CategoryId
        };

        // Act
        var result = await _categoryService.CreateCategoryAsync(subCategory);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(parentCategory.CategoryId, result.ParentId);
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_UpdatesNameAndColor()
    {
        // Arrange
        var category = new Category
        {
            Name = "Original Name",
            Color = "#FF6B6B"
        };
        await _categoryService.CreateCategoryAsync(category);

        // Act
        category.Name = "Updated Name";
        category.Color = "#4ECDC4";
        var result = await _categoryService.UpdateCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Updated Name", result.Name);
        Assert.AreEqual("#4ECDC4", result.Color);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task ResetSystemCategoryAsync_RestoresOriginalValues()
    {
        // Arrange
        var category = new Category
        {
            Name = "Modified Name",
            Color = "#000000",
            IsSystemCategory = true,
            OriginalName = "Original Name",
            OriginalColor = "#FF6B6B",
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.ResetSystemCategoryAsync(category.CategoryId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Original Name", result.Name);
        Assert.AreEqual("#FF6B6B", result.Color);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task ResetSystemCategoryAsync_ReturnsNullForNonSystemCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "User Category",
            Color = "#4ECDC4",
            IsSystemCategory = false
        };
        await _categoryService.CreateCategoryAsync(category);

        // Act
        var result = await _categoryService.ResetSystemCategoryAsync(category.CategoryId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ResetSystemCategoryAsync_ReturnsNullForNonExistentCategory()
    {
        // Act
        var result = await _categoryService.ResetSystemCategoryAsync(999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAllCategoriesAsync_ReturnsMainCategoriesWithSubcategories()
    {
        // Arrange
        var parentCategory = new Category
        {
            Name = "Parent",
            Color = "#FF6B6B"
        };
        await _categoryService.CreateCategoryAsync(parentCategory);

        var subCategory = new Category
        {
            Name = "Sub",
            Color = "#4ECDC4",
            ParentId = parentCategory.CategoryId
        };
        await _categoryService.CreateCategoryAsync(subCategory);

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();

        // Assert
        var categories = result.ToList();
        Assert.AreEqual(2, categories.Count);
        
        var parent = categories.FirstOrDefault(c => c.Name == "Parent");
        Assert.IsNotNull(parent);
        Assert.AreEqual(1, parent.SubCategories.Count());
        Assert.AreEqual("Sub", parent.SubCategories.First().Name);
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_RemovesCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "Test Category",
            Color = "#4ECDC4"
        };
        await _categoryService.CreateCategoryAsync(category);

        // Act
        await _categoryService.DeleteCategoryAsync(category.CategoryId);

        // Assert
        var result = await _categoryService.GetCategoryByIdAsync(category.CategoryId);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateCategoryAsync_PreservesAccountNumber()
    {
        // Arrange
        var category = new Category
        {
            Name = "Mat & Dryck",
            Color = "#FF6B6B",
            AccountNumber = "5000"
        };

        // Act
        var result = await _categoryService.CreateCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("5000", result.AccountNumber);
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_UpdatesAccountNumber()
    {
        // Arrange
        var category = new Category
        {
            Name = "Test Category",
            Color = "#FF6B6B",
            AccountNumber = "3000"
        };
        await _categoryService.CreateCategoryAsync(category);

        // Act
        category.AccountNumber = "3100";
        var result = await _categoryService.UpdateCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("3100", result.AccountNumber);
    }

    [TestMethod]
    public async Task ResetSystemCategoryAsync_RestoresOriginalAccountNumber()
    {
        // Arrange
        var category = new Category
        {
            Name = "Modified Name",
            Color = "#000000",
            AccountNumber = "9999",
            IsSystemCategory = true,
            OriginalName = "Original Name",
            OriginalColor = "#FF6B6B",
            OriginalAccountNumber = "5000",
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.ResetSystemCategoryAsync(category.CategoryId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Original Name", result.Name);
        Assert.AreEqual("#FF6B6B", result.Color);
        Assert.AreEqual("5000", result.AccountNumber);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task SeededCategories_HaveCorrectAccountNumbers()
    {
        // This test verifies that the database seeding includes account numbers
        // We need to create a new context with the actual seed data
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new PrivatekonomyContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var categoryService = new CategoryService(context);

        // Act
        var categories = await categoryService.GetAllCategoriesAsync();
        var categoryList = categories.ToList();

        // Assert - Verify main categories have account numbers
        var matOchDryck = categoryList.FirstOrDefault(c => c.Name == "Mat & Dryck");
        Assert.IsNotNull(matOchDryck);
        Assert.AreEqual("5000", matOchDryck.AccountNumber);

        var boende = categoryList.FirstOrDefault(c => c.Name == "Boende");
        Assert.IsNotNull(boende);
        Assert.AreEqual("4000", boende.AccountNumber);

        var lon = categoryList.FirstOrDefault(c => c.Name == "Lön");
        Assert.IsNotNull(lon);
        Assert.AreEqual("3000", lon.AccountNumber);

        var sparande = categoryList.FirstOrDefault(c => c.Name == "Sparande");
        Assert.IsNotNull(sparande);
        Assert.AreEqual("8000", sparande.AccountNumber);

        // Verify subcategories exist and have account numbers
        var livsmedel = categoryList.FirstOrDefault(c => c.Name == "Livsmedel");
        Assert.IsNotNull(livsmedel);
        Assert.AreEqual("5100", livsmedel.AccountNumber);
        Assert.AreEqual(matOchDryck.CategoryId, livsmedel.ParentId);

        var hyra = categoryList.FirstOrDefault(c => c.Name == "Hyra/Avgift");
        Assert.IsNotNull(hyra);
        Assert.AreEqual("4100", hyra.AccountNumber);
        Assert.AreEqual(boende.CategoryId, hyra.ParentId);
    }
}
