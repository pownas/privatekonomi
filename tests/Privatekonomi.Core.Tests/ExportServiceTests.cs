using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class ExportServiceTests
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly ExportService _exportService;
    private readonly string _testUserId = "test-user-123";

    public ExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        _exportService = new ExportService(_context, _mockCurrentUserService.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task SeedTestData()
    {
        // Add categories
        var category1 = new Category 
        { 
            CategoryId = 1, 
            Name = "Mat", 
            Color = "#FF0000"
        };
        var category2 = new Category 
        { 
            CategoryId = 2, 
            Name = "Lön", 
            Color = "#00FF00"
        };
        
        _context.Categories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        // Add transactions for different years
        var transactions = new List<Transaction>
        {
            // Year 2023
            new Transaction
            {
                TransactionId = 1,
                Amount = 5000,
                Description = "Matinköp ICA",
                Date = new DateTime(2023, 1, 15),
                IsIncome = false,
                UserId = _testUserId,
                Currency = "SEK"
            },
            new Transaction
            {
                TransactionId = 2,
                Amount = 30000,
                Description = "Lön januari",
                Date = new DateTime(2023, 1, 25),
                IsIncome = true,
                UserId = _testUserId,
                Currency = "SEK"
            },
            // Year 2024
            new Transaction
            {
                TransactionId = 3,
                Amount = 6000,
                Description = "Matinköp Willys",
                Date = new DateTime(2024, 2, 10),
                IsIncome = false,
                UserId = _testUserId,
                Currency = "SEK"
            },
            new Transaction
            {
                TransactionId = 4,
                Amount = 32000,
                Description = "Lön februari",
                Date = new DateTime(2024, 2, 25),
                IsIncome = true,
                UserId = _testUserId,
                Currency = "SEK"
            },
            new Transaction
            {
                TransactionId = 5,
                Amount = 3500,
                Description = "Hyra",
                Date = new DateTime(2024, 3, 1),
                IsIncome = false,
                UserId = _testUserId,
                Currency = "SEK"
            }
        };

        _context.Transactions.AddRange(transactions);

        // Add budgets
        var budget2023 = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2023",
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2023, 12, 31),
            Period = BudgetPeriod.Yearly,
            UserId = _testUserId
        };

        var budget2024 = new Budget
        {
            BudgetId = 2,
            Name = "Budget 2024",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = BudgetPeriod.Yearly,
            UserId = _testUserId
        };

        _context.Budgets.AddRange(budget2023, budget2024);

        // Add salary history
        var salaryHistory2023 = new SalaryHistory
        {
            SalaryHistoryId = 1,
            MonthlySalary = 30000,
            Period = new DateTime(2023, 1, 1),
            JobTitle = "Developer",
            UserId = _testUserId
        };

        var salaryHistory2024 = new SalaryHistory
        {
            SalaryHistoryId = 2,
            MonthlySalary = 32000,
            Period = new DateTime(2024, 1, 1),
            JobTitle = "Senior Developer",
            UserId = _testUserId
        };

        _context.SalaryHistories.AddRange(salaryHistory2023, salaryHistory2024);

        await _context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task GetAvailableYearsAsync_ReturnsYearsWithTransactions()
    {
        // Arrange
        await SeedTestData();

        // Act
        var years = await _exportService.GetAvailableYearsAsync();

        // Assert
        Assert.IsNotNull(years);
        Assert.AreEqual(2, years.Count);
        CollectionAssert.Contains(years, 2023);
        CollectionAssert.Contains(years, 2024);
        Assert.AreEqual(2024, years[0]); // Should be sorted descending
        Assert.AreEqual(2023, years[1]);
    }

    [TestMethod]
    public async Task GetAvailableYearsAsync_NoTransactions_ReturnsEmptyList()
    {
        // Arrange - no data seeded

        // Act
        var years = await _exportService.GetAvailableYearsAsync();

        // Assert
        Assert.IsNotNull(years);
        Assert.AreEqual(0, years.Count());
    }

    [TestMethod]
    public async Task ExportYearDataToJsonAsync_ValidYear_ExportsCorrectData()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2024);

        // Assert
        Assert.IsNotNull(data);
        Assert.IsTrue(data.Length > 0);

        // Verify it's valid JSON and contains expected year and structure
        var json = Encoding.UTF8.GetString(data);
        StringAssert.Contains(json.ToLowerInvariant(), "\"year\": 2024");
        StringAssert.Contains(json.ToLowerInvariant(), "\"data\"");
        StringAssert.Contains(json.ToLowerInvariant(), "\"transactions\"");
        
        // Parse to verify it's valid JSON - skip BOM if present
        var jsonBytes = data;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            jsonBytes = data.Skip(3).ToArray();
        }
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonBytes);
        var root = jsonDoc.RootElement;
        
        Assert.AreEqual(2024, root.GetProperty("year").GetInt32());
        var dataElement = root.GetProperty("data");
        var transactions = dataElement.GetProperty("transactions");
        
        // Should have 3 transactions for 2024
        Assert.AreEqual(3, transactions.GetArrayLength());
    }

    [TestMethod]
    public async Task ExportYearDataToCsvAsync_ValidYear_ExportsCorrectData()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2024);

        // Assert
        Assert.IsNotNull(data);
        Assert.IsTrue(data.Length > 0);

        // Verify it's valid CSV with header
        var csv = Encoding.UTF8.GetString(data);
        StringAssert.Contains(csv, "# Privatekonomi Export - År 2024");
        StringAssert.Contains(csv, "Datum,Beskrivning,Belopp,Typ");
        StringAssert.Contains(csv, "Matinköp Willys");
        StringAssert.Contains(csv, "Lön februari");
        StringAssert.Contains(csv, "Hyra");
        
        // Should not contain 2023 data
        Assert.IsFalse(csv.Contains("Matinköp ICA"));
        Assert.IsFalse(csv.Contains("Lön januari"));

        // Verify summary section
        StringAssert.Contains(csv, "# Summering 2024");
        StringAssert.Contains(csv, "# Totala inkomster: 32000");
        StringAssert.Contains(csv, "# Totala utgifter: 9500"); // 6000 + 3500
        StringAssert.Contains(csv, "# Nettoresultat: 22500"); // 32000 - 9500
    }

    [TestMethod]
    public async Task ExportYearDataToJsonAsync_IncludesAllRelevantData()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2024);

        // Assert
        var json = Encoding.UTF8.GetString(data);
        
        // Check for all data types (camelCase because of JsonNamingPolicy.CamelCase)
        StringAssert.Contains(json.ToLowerInvariant(), "transactions");
        StringAssert.Contains(json.ToLowerInvariant(), "budgets");
        StringAssert.Contains(json.ToLowerInvariant(), "goals");
        StringAssert.Contains(json.ToLowerInvariant(), "investments");
        StringAssert.Contains(json.ToLowerInvariant(), "loans");
        // SalaryHistory is serialized as "salaryHistory" in camelCase
        StringAssert.Contains(json.ToLowerInvariant(), "\"salaryhistory\"");
        
        // Check for metadata (also in camelCase)
        StringAssert.Contains(json.ToLowerInvariant(), "\"year\": 2024");
        StringAssert.Contains(json.ToLowerInvariant(), "\"exportdate\""); // Property name in camelCase
        StringAssert.Contains(json.ToLowerInvariant(), "\"version\"");
    }

    [TestMethod]
    public async Task ExportYearDataToCsvAsync_CalculatesSummaryCorrectly()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2023);

        // Assert
        var csv = Encoding.UTF8.GetString(data);
        
        // Verify counts
        StringAssert.Contains(csv, "# Antal transaktioner: 2");
        
        // Verify financial summary
        StringAssert.Contains(csv, "# Totala inkomster: 30000,00 SEK");
        StringAssert.Contains(csv, "# Totala utgifter: 5000,00 SEK");
        StringAssert.Contains(csv, "# Nettoresultat: 25000,00 SEK");
    }

    [TestMethod]
    public async Task ExportYearDataToJsonAsync_FiltersByUserId()
    {
        // Arrange
        await SeedTestData();
        
        // Add transaction for different user
        var otherUserTransaction = new Transaction
        {
            TransactionId = 10,
            Amount = 99999,
            Description = "Other user transaction",
            Date = new DateTime(2024, 5, 1),
            IsIncome = true,
            UserId = "other-user-456",
            Currency = "SEK"
        };
        _context.Transactions.Add(otherUserTransaction);
        await _context.SaveChangesAsync();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2024);

        // Assert - skip BOM if present
        var jsonBytes = data;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            jsonBytes = data.Skip(3).ToArray();
        }
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonBytes);
        var root = jsonDoc.RootElement;
        var dataElement = root.GetProperty("data");
        var transactions = dataElement.GetProperty("transactions");
        
        // Should only have 3 transactions for current user (not the other user's transaction)
        Assert.AreEqual(3, transactions.GetArrayLength());
        
        // Verify none of the transactions have the other user's ID
        foreach (var transaction in transactions.EnumerateArray())
        {
            var userId = transaction.GetProperty("userId").GetString();
            Assert.AreEqual(_testUserId, userId);
        }
    }

    [TestMethod]
    public async Task ExportYearDataToCsvAsync_FiltersByUserId()
    {
        // Arrange
        await SeedTestData();
        
        // Add transaction for different user
        var otherUserTransaction = new Transaction
        {
            TransactionId = 10,
            Amount = 99999,
            Description = "Other user transaction",
            Date = new DateTime(2024, 5, 1),
            IsIncome = true,
            UserId = "other-user-456",
            Currency = "SEK"
        };
        _context.Transactions.Add(otherUserTransaction);
        await _context.SaveChangesAsync();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2024);

        // Assert
        var csv = Encoding.UTF8.GetString(data);
        
        // Should contain current user's data
        StringAssert.Contains(csv, "Matinköp Willys");
        
        // Should NOT contain other user's data
        Assert.IsFalse(csv.Contains("Other user transaction"));
    }

    [TestMethod]
    public async Task ExportYearDataToJsonAsync_YearWithNoData_ReturnsEmptyDataStructure()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2025); // No data for 2025

        // Assert
        Assert.IsNotNull(data);
        var json = Encoding.UTF8.GetString(data);
        
        // Should still have structure but empty arrays
        StringAssert.Contains(json.ToLowerInvariant(), "2025");
        StringAssert.Contains(json.ToLowerInvariant(), "transactions");
    }

    [TestMethod]
    public async Task ExportYearDataToCsvAsync_YearWithNoData_ReturnsHeaderOnly()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2025); // No data for 2025

        // Assert
        Assert.IsNotNull(data);
        var csv = Encoding.UTF8.GetString(data);
        
        // Should have header with 0 transactions
        StringAssert.Contains(csv, "# Privatekonomi Export - År 2025");
        StringAssert.Contains(csv, "# Antal transaktioner: 0");
        StringAssert.Contains(csv, "# Totala inkomster: 0,00 SEK");
        StringAssert.Contains(csv, "# Totala utgifter: 0,00 SEK");
        StringAssert.Contains(csv, "# Nettoresultat: 0,00 SEK");
    }
}
