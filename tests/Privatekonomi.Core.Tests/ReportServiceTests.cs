using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class ReportServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly ReportService _reportService;
    private const string TestUserId = "test-user-123";

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _reportService = new ReportService(_context);
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
    public async Task GetNetWorthReportAsync_WithNoData_ReturnsZeroNetWorth()
    {
        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(0, report.TotalAssets);
        Assert.AreEqual(0, report.TotalInvestments);
        Assert.AreEqual(0, report.TotalLiabilities);
        Assert.AreEqual(0, report.NetWorth);
        Assert.AreEqual(0, report.Assets.Count());
        Assert.AreEqual(0, report.Liabilities.Count());
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_WithAssets_CalculatesCorrectNetWorth()
    {
        // Arrange
        var asset = new Asset
        {
            UserId = TestUserId,
            Name = "Test Asset",
            Type = "Property",
            CurrentValue = 500000m,
            PurchaseValue = 400000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.AreEqual(500000m, report.TotalAssets);
        Assert.AreEqual(0, report.TotalLiabilities);
        Assert.AreEqual(500000m, report.NetWorth);
        Assert.AreEqual(1, report.Assets.Count());
        Assert.AreEqual("Test Asset", report.Assets[0].Name);
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_WithInvestments_IncludesInTotalAssets()
    {
        // Arrange
        var investment = new Investment
        {
            UserId = TestUserId,
            Name = "Test Stock",
            Type = "Aktie",
            Quantity = 100,
            PurchasePrice = 50m,
            CurrentPrice = 75m,
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.AreEqual(7500m, report.TotalAssets); // 100 * 75
        Assert.AreEqual(7500m, report.TotalInvestments);
        Assert.AreEqual(7500m, report.NetWorth);
        Assert.AreEqual(1, report.Assets.Count());
        Assert.AreEqual("Investment", report.Assets[0].Type);
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_WithLoans_SubtractsFromNetWorth()
    {
        // Arrange
        var asset = new Asset
        {
            UserId = TestUserId,
            Name = "Property",
            Type = "Fastighet",
            CurrentValue = 3000000m,
            PurchaseValue = 2500000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        var loan = new Loan
        {
            UserId = TestUserId,
            Name = "Mortgage",
            Type = "Bolån",
            Amount = 2000000m,
            InterestRate = 3.5m,
            Amortization = 5000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Assets.Add(asset);
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.AreEqual(3000000m, report.TotalAssets);
        Assert.AreEqual(2000000m, report.TotalLiabilities);
        Assert.AreEqual(1000000m, report.NetWorth);
        Assert.AreEqual(1, report.Liabilities.Count());
        Assert.AreEqual("Mortgage", report.Liabilities[0].Name);
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_WithMultipleUsers_FiltersCorrectly()
    {
        // Arrange
        var userAsset = new Asset
        {
            UserId = TestUserId,
            Name = "User Asset",
            Type = "Property",
            CurrentValue = 100000m,
            PurchaseValue = 80000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        var otherAsset = new Asset
        {
            UserId = "other-user",
            Name = "Other Asset",
            Type = "Property",
            CurrentValue = 500000m,
            PurchaseValue = 400000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Assets.AddRange(userAsset, otherAsset);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.AreEqual(100000m, report.TotalAssets);
        Assert.AreEqual(1, report.Assets.Count());
        Assert.AreEqual("User Asset", report.Assets[0].Name);
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_ReturnsHistoricalData()
    {
        // Arrange
        var asset = new Asset
        {
            UserId = TestUserId,
            Name = "Test Asset",
            Type = "Property",
            CurrentValue = 100000m,
            PurchaseValue = 80000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.IsNotNull(report.History);
        Assert.AreNotEqual(0, report.History.Count());
        // History should contain 12 months of data
        Assert.AreEqual(12, report.History.Count);
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_WithSnapshots_UsesSnapshotData()
    {
        // Arrange
        var snapshot = new NetWorthSnapshot
        {
            UserId = TestUserId,
            Date = DateTime.Today.AddMonths(-1),
            TotalAssets = 50000m,
            TotalLiabilities = 10000m,
            NetWorth = 40000m,
            CreatedAt = DateTime.UtcNow
        };
        _context.NetWorthSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.IsNotNull(report.History);
        Assert.AreNotEqual(0, report.History.Count());
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_CalculatesPercentageChange()
    {
        // Arrange - Create two snapshots for different months
        var oldSnapshot = new NetWorthSnapshot
        {
            UserId = TestUserId,
            Date = DateTime.Today.AddMonths(-2).AddDays(-DateTime.Today.Day + 1),
            TotalAssets = 100000m,
            TotalLiabilities = 20000m,
            NetWorth = 80000m,
            CreatedAt = DateTime.UtcNow
        };
        var recentSnapshot = new NetWorthSnapshot
        {
            UserId = TestUserId,
            Date = DateTime.Today.AddMonths(-1).AddDays(-DateTime.Today.Day + 1),
            TotalAssets = 120000m,
            TotalLiabilities = 20000m,
            NetWorth = 100000m,
            CreatedAt = DateTime.UtcNow
        };
        _context.NetWorthSnapshots.AddRange(oldSnapshot, recentSnapshot);
        
        // Add current assets
        var asset = new Asset
        {
            UserId = TestUserId,
            Name = "Current Asset",
            Type = "Property",
            CurrentValue = 120000m,
            PurchaseValue = 100000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        var loan = new Loan
        {
            UserId = TestUserId,
            Name = "Loan",
            Type = "Lån",
            Amount = 20000m,
            InterestRate = 5m,
            Amortization = 500m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Assets.Add(asset);
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(TestUserId);

        // Assert
        Assert.AreNotEqual(0, report.PercentageChange);
    }

    [TestMethod]
    public async Task GetNetWorthReportAsync_WithNullUserId_ReturnsAllUsersData()
    {
        // Arrange
        var asset1 = new Asset
        {
            UserId = "user1",
            Name = "Asset 1",
            Type = "Property",
            CurrentValue = 100000m,
            PurchaseValue = 80000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        var asset2 = new Asset
        {
            UserId = "user2",
            Name = "Asset 2",
            Type = "Property",
            CurrentValue = 200000m,
            PurchaseValue = 180000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetNetWorthReportAsync(null);

        // Assert
        Assert.AreEqual(300000m, report.TotalAssets); // Sum of both users
        Assert.AreEqual(2, report.Assets.Count);
    }
    
    [TestMethod]
    public async Task GetPeriodComparisonAsync_WithTransactions_ReturnsCorrectComparison()
    {
        // Arrange
        var referenceDate = new DateTime(2025, 1, 15);
        
        // Current month (January 2025) - 1,000 income, 600 expenses
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 1000m,
            Date = new DateTime(2025, 1, 10),
            Description = "Salary January",
            IsIncome = true
        });
        
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 600m,
            Date = new DateTime(2025, 1, 12),
            Description = "Groceries January",
            IsIncome = false
        });

        // Previous month (December 2024) - 1,000 income, 800 expenses
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 1000m,
            Date = new DateTime(2024, 12, 10),
            Description = "Salary December",
            IsIncome = true
        });
        
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 800m,
            Date = new DateTime(2024, 12, 15),
            Description = "Groceries December",
            IsIncome = false
        });

        // Year ago (January 2024) - 900 income, 700 expenses
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 900m,
            Date = new DateTime(2024, 1, 10),
            Description = "Salary January 2024",
            IsIncome = true
        });
        
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 700m,
            Date = new DateTime(2024, 1, 12),
            Description = "Groceries January 2024",
            IsIncome = false
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _reportService.GetPeriodComparisonAsync(referenceDate);

        // Assert
        Assert.IsNotNull(result);
        
        // Check income comparison
        Assert.AreEqual(1000m, result.Income.CurrentPeriod);
        Assert.AreEqual(1000m, result.Income.PreviousPeriod);
        Assert.AreEqual(900m, result.Income.YearAgoPeriod);
        Assert.AreEqual(0m, result.Income.ChangeFromPrevious);
        Assert.AreEqual(100m, result.Income.ChangeFromYearAgo);
        Assert.AreEqual(0m, result.Income.PercentageChangeFromPrevious);
        Assert.AreEqual(11.1m, Math.Round(result.Income.PercentageChangeFromYearAgo, 1));
        
        // Check expenses comparison
        Assert.AreEqual(600m, result.Expenses.CurrentPeriod);
        Assert.AreEqual(800m, result.Expenses.PreviousPeriod);
        Assert.AreEqual(700m, result.Expenses.YearAgoPeriod);
        Assert.AreEqual(-200m, result.Expenses.ChangeFromPrevious);
        Assert.AreEqual(-100m, result.Expenses.ChangeFromYearAgo);
        Assert.AreEqual(-25m, result.Expenses.PercentageChangeFromPrevious);
        Assert.AreEqual(-14.3m, Math.Round(result.Expenses.PercentageChangeFromYearAgo, 1));
        Assert.AreEqual("Improving", result.Expenses.TrendDirection);
        
        // Check net flow comparison
        Assert.AreEqual(400m, result.NetFlow.CurrentPeriod); // 1000 - 600
        Assert.AreEqual(200m, result.NetFlow.PreviousPeriod); // 1000 - 800
        Assert.AreEqual(200m, result.NetFlow.YearAgoPeriod); // 900 - 700
        Assert.AreEqual(200m, result.NetFlow.ChangeFromPrevious);
        Assert.AreEqual(100m, result.NetFlow.PercentageChangeFromPrevious);
        Assert.AreEqual("Improving", result.NetFlow.TrendDirection);
    }

    [TestMethod]
    public async Task GetPeriodComparisonAsync_NoTransactions_ReturnsZeroValues()
    {
        // Arrange
        var referenceDate = new DateTime(2025, 1, 15);

        // Act
        var result = await _reportService.GetPeriodComparisonAsync(referenceDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result.Income.CurrentPeriod);
        Assert.AreEqual(0m, result.Income.PreviousPeriod);
        Assert.AreEqual(0m, result.Income.YearAgoPeriod);
        Assert.AreEqual(0m, result.Expenses.CurrentPeriod);
        Assert.AreEqual(0m, result.Expenses.PreviousPeriod);
        Assert.AreEqual(0m, result.Expenses.YearAgoPeriod);
    }

    [TestMethod]
    public async Task GetPeriodComparisonAsync_IncreasingExpenses_ShowsWorsening()
    {
        // Arrange
        var referenceDate = new DateTime(2025, 1, 15);
        
        // Current month - higher expenses
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 1000m,
            Date = new DateTime(2025, 1, 10),
            Description = "Expensive purchase",
            IsIncome = false
        });

        // Previous month - lower expenses
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 500m,
            Date = new DateTime(2024, 12, 10),
            Description = "Normal purchase",
            IsIncome = false
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _reportService.GetPeriodComparisonAsync(referenceDate);

        // Assert
        Assert.AreEqual("Worsening", result.Expenses.TrendDirection);
        Assert.IsTrue(result.Expenses.ChangeFromPrevious > 0);
        Assert.IsTrue(result.Expenses.PercentageChangeFromPrevious > 0);
    }

    [TestMethod]
    public async Task GetPeriodComparisonAsync_StableExpenses_ShowsStable()
    {
        // Arrange
        var referenceDate = new DateTime(2025, 1, 15);
        
        // Current month
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 1000m,
            Date = new DateTime(2025, 1, 10),
            Description = "Purchase",
            IsIncome = false
        });

        // Previous month - very similar
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 1020m,
            Date = new DateTime(2024, 12, 10),
            Description = "Purchase",
            IsIncome = false
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _reportService.GetPeriodComparisonAsync(referenceDate);

        // Assert
        // Change is less than 5%, so should be "Stable"
        Assert.AreEqual("Stable", result.Expenses.TrendDirection);
    }

    [TestMethod]
    public async Task GetPeriodComparisonAsync_GeneratesSparklineData()
    {
        // Arrange
        var referenceDate = new DateTime(2025, 1, 15);
        
        // Add transactions for the last 6 months with increasing amounts
        for (int i = 5; i >= 0; i--)
        {
            var date = new DateTime(2025, 1, 1).AddMonths(-i).AddDays(5);
            await _context.Transactions.AddAsync(new Transaction
            {
                Amount = 100m * (6 - i), // Increasing: 100, 200, 300, 400, 500, 600
                Date = date,
                Description = $"Expense {i}",
                IsIncome = false
            });
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _reportService.GetPeriodComparisonAsync(referenceDate);

        // Assert
        Assert.IsNotNull(result.SparklineData);
        Assert.AreEqual(6, result.SparklineData.Count);
        // Verify we have increasing trend (first value should be less than last)
        Assert.IsTrue(result.SparklineData[0] < result.SparklineData[5]);
        Assert.AreEqual(100, result.SparklineData[0]);
        Assert.AreEqual(600, result.SparklineData[5]);
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_WithNoData_ReturnsEmptyReport()
    {
        // Arrange
        var fromDate = DateTime.Today.AddMonths(-1);
        var toDate = DateTime.Today;

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(fromDate, toDate);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(0, report.TotalSpending);
        Assert.AreEqual(0, report.AverageMonthlySpending);
        Assert.AreEqual(0, report.CategoryDistribution.Count());
        Assert.AreEqual(0, report.TopCategories.Count());
        Assert.AreEqual(0, report.MonthlyData.Count());
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_CalculatesTotalSpending()
    {
        // Arrange
        var category = new Category { CategoryId = 1, Name = "Mat", Color = "#FF0000" };
        _context.Categories.Add(category);
        
        var transaction1 = new Transaction
        {
            TransactionId = 1,
            Amount = 500m,
            Date = DateTime.Today.AddDays(-10),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };
        
        var transaction2 = new Transaction
        {
            TransactionId = 2,
            Amount = 300m,
            Date = DateTime.Today.AddDays(-5),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };

        _context.Transactions.AddRange(transaction1, transaction2);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-1), 
            DateTime.Today);

        // Assert
        Assert.AreEqual(800m, report.TotalSpending);
        Assert.AreNotEqual(0, report.CategoryDistribution.Count());
        Assert.AreEqual(1, report.CategoryDistribution.Count());
        Assert.AreEqual("Mat", report.CategoryDistribution[0].CategoryName);
        Assert.AreEqual(800m, report.CategoryDistribution[0].Amount);
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_CalculatesCategoryPercentages()
    {
        // Arrange
        var category1 = new Category { CategoryId = 1, Name = "Mat", Color = "#FF0000" };
        var category2 = new Category { CategoryId = 2, Name = "Transport", Color = "#00FF00" };
        _context.Categories.AddRange(category1, category2);

        var transaction1 = new Transaction
        {
            TransactionId = 1,
            Amount = 700m,
            Date = DateTime.Today.AddDays(-10),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category1 }
            }
        };

        var transaction2 = new Transaction
        {
            TransactionId = 2,
            Amount = 300m,
            Date = DateTime.Today.AddDays(-5),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 2, Category = category2 }
            }
        };

        _context.Transactions.AddRange(transaction1, transaction2);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-1),
            DateTime.Today);

        // Assert
        Assert.AreEqual(1000m, report.TotalSpending);
        Assert.AreEqual(2, report.CategoryDistribution.Count);
        
        var matCategory = report.CategoryDistribution.First(c => c.CategoryName == "Mat");
        Assert.AreEqual(70m, matCategory.Percentage); // 700/1000 * 100
        
        var transportCategory = report.CategoryDistribution.First(c => c.CategoryName == "Transport");
        Assert.AreEqual(30m, transportCategory.Percentage); // 300/1000 * 100
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_IdentifiesTopCategories()
    {
        // Arrange - Create 6 categories with different amounts
        var categories = new List<Category>
        {
            new Category { CategoryId = 1, Name = "Cat1", Color = "#FF0000" },
            new Category { CategoryId = 2, Name = "Cat2", Color = "#00FF00" },
            new Category { CategoryId = 3, Name = "Cat3", Color = "#0000FF" },
            new Category { CategoryId = 4, Name = "Cat4", Color = "#FFFF00" },
            new Category { CategoryId = 5, Name = "Cat5", Color = "#FF00FF" },
            new Category { CategoryId = 6, Name = "Cat6", Color = "#00FFFF" }
        };
        _context.Categories.AddRange(categories);

        var amounts = new[] { 1000m, 800m, 600m, 400m, 200m, 100m };
        for (int i = 0; i < 6; i++)
        {
            var transaction = new Transaction
            {
                TransactionId = i + 1,
                Amount = amounts[i],
                Date = DateTime.Today.AddDays(-5),
                IsIncome = false,
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = i + 1, Category = categories[i] }
                }
            };
            _context.Transactions.Add(transaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-1),
            DateTime.Today);

        // Assert - Should only have top 5 categories
        Assert.AreEqual(5, report.TopCategories.Count);
        Assert.AreEqual("Cat1", report.TopCategories[0].CategoryName);
        Assert.AreEqual(1000m, report.TopCategories[0].Amount);
        Assert.AreEqual("Cat5", report.TopCategories[4].CategoryName);
        Assert.AreEqual(200m, report.TopCategories[4].Amount);
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_CalculatesMonthlyData()
    {
        // Arrange
        var category = new Category { CategoryId = 1, Name = "Mat", Color = "#FF0000" };
        _context.Categories.Add(category);

        // Add transactions across 2 months
        var month1Transaction = new Transaction
        {
            TransactionId = 1,
            Amount = 500m,
            Date = DateTime.Today.AddMonths(-2).AddDays(5),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };

        var month2Transaction = new Transaction
        {
            TransactionId = 2,
            Amount = 700m,
            Date = DateTime.Today.AddMonths(-1).AddDays(5),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };

        _context.Transactions.AddRange(month1Transaction, month2Transaction);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-3),
            DateTime.Today);

        // Assert
        Assert.AreNotEqual(0, report.MonthlyData.Count());
        Assert.AreEqual(2, report.MonthlyData.Count);
        Assert.AreEqual(500m, report.MonthlyData[0].TotalAmount);
        Assert.AreEqual(700m, report.MonthlyData[1].TotalAmount);
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_DetectsTrends()
    {
        // Arrange
        var category = new Category { CategoryId = 1, Name = "Mat", Color = "#FF0000" };
        _context.Categories.Add(category);

        // Create increasing trend: 100, 200, 300, 400, 500, 600
        for (int i = 0; i < 6; i++)
        {
            var transaction = new Transaction
            {
                TransactionId = i + 1,
                Amount = (i + 1) * 100m,
                Date = DateTime.Today.AddMonths(-6 + i).AddDays(5),
                IsIncome = false,
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow,
                TransactionCategories = new List<TransactionCategory>
                {
                    new TransactionCategory { CategoryId = 1, Category = category }
                }
            };
            _context.Transactions.Add(transaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-7),
            DateTime.Today);

        // Assert
        Assert.AreNotEqual(0, report.Trends.Count());
        var overallTrend = report.Trends.FirstOrDefault(t => t.CategoryName == "Total utgifter");
        Assert.IsNotNull(overallTrend);
        Assert.AreEqual("Increasing", overallTrend.TrendType);
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_GeneratesRecommendations()
    {
        // Arrange
        var category = new Category { CategoryId = 1, Name = "Mat", Color = "#FF0000" };
        _context.Categories.Add(category);

        // Create a category with high percentage (> 20%)
        var transaction = new Transaction
        {
            TransactionId = 1,
            Amount = 5000m,
            Date = DateTime.Today.AddDays(-5),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-1),
            DateTime.Today);

        // Assert
        Assert.AreNotEqual(0, report.Recommendations.Count());
        // Should have recommendation about high percentage category
        var highPercentageRec = report.Recommendations.FirstOrDefault(r => r.Type == "BudgetAlert");
        Assert.IsNotNull(highPercentageRec);
    }

    [TestMethod]
    public async Task GetSpendingPatternReportAsync_FiltersByHousehold()
    {
        // Arrange
        var category = new Category { CategoryId = 1, Name = "Mat", Color = "#FF0000" };
        _context.Categories.Add(category);

        var household1Transaction = new Transaction
        {
            TransactionId = 1,
            Amount = 500m,
            Date = DateTime.Today.AddDays(-5),
            IsIncome = false,
            HouseholdId = 1,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };

        var household2Transaction = new Transaction
        {
            TransactionId = 2,
            Amount = 300m,
            Date = DateTime.Today.AddDays(-5),
            IsIncome = false,
            HouseholdId = 2,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 1, Category = category }
            }
        };

        _context.Transactions.AddRange(household1Transaction, household2Transaction);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetSpendingPatternReportAsync(
            DateTime.Today.AddMonths(-1),
            DateTime.Today,
            householdId: 1);

        // Assert
        Assert.AreEqual(500m, report.TotalSpending);
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_WithNoData_ReturnsEmptyReport()
    {
        // Arrange
        var year = 2025;
        var month = 1;

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(year, report.Year);
        Assert.AreEqual(month, report.Month);
        Assert.AreEqual("januari", report.MonthName);
        Assert.AreEqual(0, report.TotalIncome);
        Assert.AreEqual(0, report.TotalExpenses);
        Assert.AreEqual(0, report.NetFlow);
        Assert.AreEqual(0, report.TransactionCount);
        Assert.AreEqual(0, report.CategorySummaries.Count());
        Assert.AreEqual(0, report.TopMerchants.Count());
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_WithTransactions_CalculatesCorrectTotals()
    {
        // Arrange
        var year = 2025;
        var month = 1;

        // Add income
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 30000m,
            Date = new DateTime(2025, 1, 25),
            Description = "Lön",
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        // Add expenses
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 5000m,
            Date = new DateTime(2025, 1, 5),
            Description = "Hyra",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 2000m,
            Date = new DateTime(2025, 1, 10),
            Description = "Mat",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(30000m, report.TotalIncome);
        Assert.AreEqual(7000m, report.TotalExpenses);
        Assert.AreEqual(23000m, report.NetFlow);
        Assert.AreEqual(3, report.TransactionCount);
        Assert.IsTrue(report.SavingsRate > 75m); // (23000 / 30000) * 100 ≈ 76.67%
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_CalculatesSavingsRate()
    {
        // Arrange
        var year = 2025;
        var month = 1;

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 10000m,
            Date = new DateTime(2025, 1, 25),
            Description = "Lön",
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 8000m,
            Date = new DateTime(2025, 1, 10),
            Description = "Utgifter",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        // Savings rate = (10000 - 8000) / 10000 * 100 = 20%
        Assert.AreEqual(20m, report.SavingsRate);
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_WithCategorizedExpenses_GeneratesCategorySummaries()
    {
        // Arrange
        var year = 2025;
        var month = 1;

        var category = new Category { CategoryId = 100, Name = "Mat", Color = "#FF0000" };
        _context.Categories.Add(category);

        var transaction = new Transaction
        {
            TransactionId = 100,
            Amount = 3000m,
            Date = new DateTime(2025, 1, 15),
            Description = "ICA",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            TransactionCategories = new List<TransactionCategory>
            {
                new TransactionCategory { CategoryId = 100, Category = category }
            }
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreNotEqual(0, report.CategorySummaries.Count());
        var matCategory = report.CategorySummaries.FirstOrDefault(c => c.CategoryName == "Mat");
        Assert.IsNotNull(matCategory);
        Assert.AreEqual(3000m, matCategory.Amount);
        Assert.AreEqual(100m, matCategory.Percentage); // Only category, so 100%
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_ComparesWithPreviousMonth()
    {
        // Arrange
        var year = 2025;
        var month = 2;

        // January transactions
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 25000m,
            Date = new DateTime(2025, 1, 25),
            Description = "Lön januari",
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 20000m,
            Date = new DateTime(2025, 1, 15),
            Description = "Utgifter januari",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        // February transactions
        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 30000m,
            Date = new DateTime(2025, 2, 25),
            Description = "Lön februari",
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 18000m,
            Date = new DateTime(2025, 2, 15),
            Description = "Utgifter februari",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        Assert.IsNotNull(report.PreviousMonthComparison);
        Assert.AreEqual(5000m, report.PreviousMonthComparison.IncomeChange); // 30000 - 25000
        Assert.AreEqual(-2000m, report.PreviousMonthComparison.ExpenseChange); // 18000 - 20000
        Assert.AreEqual(20m, report.PreviousMonthComparison.IncomeChangePercent); // (5000/25000) * 100
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_GeneratesInsights()
    {
        // Arrange
        var year = 2025;
        var month = 1;

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 50000m,
            Date = new DateTime(2025, 1, 25),
            Description = "Lön",
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 35000m,
            Date = new DateTime(2025, 1, 15),
            Description = "Utgifter",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        Assert.IsNotNull(report.Insights);
        Assert.AreNotEqual(0, report.Insights.Count());
        // Should have a savings rate insight
        Assert.IsTrue(report.Insights.Any(i => i.Title.Contains("sparande", StringComparison.OrdinalIgnoreCase) || 
                                              i.Title.Contains("sparprocent", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task GenerateMonthlyReportAsync_IncludesTopMerchants()
    {
        // Arrange
        var year = 2025;
        var month = 1;

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 3000m,
            Date = new DateTime(2025, 1, 5),
            Description = "ICA Supermarket",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 2500m,
            Date = new DateTime(2025, 1, 12),
            Description = "ICA Supermarket",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.Transactions.AddAsync(new Transaction
        {
            Amount = 500m,
            Date = new DateTime(2025, 1, 20),
            Description = "Coop Forum",
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GenerateMonthlyReportAsync(year, month);

        // Assert
        Assert.IsNotNull(report.TopMerchants);
        Assert.AreNotEqual(0, report.TopMerchants.Count());
        // ICA should be first as it has highest total
        Assert.AreEqual("ICA", report.TopMerchants.First().Name);
    }

    [TestMethod]
    public async Task GetMonthlyReportAsync_WithNoSavedReport_ReturnsNull()
    {
        // Act
        var report = await _reportService.GetMonthlyReportAsync(2025, 1, TestUserId);

        // Assert
        Assert.IsNull(report);
    }

    [TestMethod]
    public async Task SaveMonthlyReportAsync_SavesReportToDatabase()
    {
        // Arrange
        var reportData = new MonthlyReportData
        {
            Year = 2025,
            Month = 1,
            MonthName = "januari",
            TotalIncome = 30000m,
            TotalExpenses = 20000m,
            NetFlow = 10000m,
            SavingsRate = 33.33m,
            TransactionCount = 15,
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var savedReport = await _reportService.SaveMonthlyReportAsync(reportData, TestUserId);

        // Assert
        Assert.IsNotNull(savedReport);
        Assert.AreEqual(new DateTime(2025, 1, 1), savedReport.ReportMonth);
        Assert.AreEqual(30000m, savedReport.TotalIncome);
        Assert.AreEqual(20000m, savedReport.TotalExpenses);
        Assert.AreEqual(10000m, savedReport.NetFlow);
        Assert.AreEqual(ReportStatus.Generated, savedReport.Status);
        Assert.AreEqual(TestUserId, savedReport.UserId);
    }

    [TestMethod]
    public async Task SaveMonthlyReportAsync_UpdatesExistingReport()
    {
        // Arrange
        var initialReport = new MonthlyReportData
        {
            Year = 2025,
            Month = 1,
            MonthName = "januari",
            TotalIncome = 25000m,
            TotalExpenses = 20000m,
            NetFlow = 5000m,
            GeneratedAt = DateTime.UtcNow
        };

        await _reportService.SaveMonthlyReportAsync(initialReport, TestUserId);

        var updatedReport = new MonthlyReportData
        {
            Year = 2025,
            Month = 1,
            MonthName = "januari",
            TotalIncome = 30000m,
            TotalExpenses = 22000m,
            NetFlow = 8000m,
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var result = await _reportService.SaveMonthlyReportAsync(updatedReport, TestUserId);

        // Assert
        Assert.AreEqual(30000m, result.TotalIncome);
        Assert.AreEqual(22000m, result.TotalExpenses);
        Assert.AreEqual(8000m, result.NetFlow);

        // Verify only one report exists for this month
        var allReports = await _reportService.GetMonthlyReportsAsync(TestUserId);
        var reportsForJan2025 = allReports.Where(r => r.ReportMonth.Year == 2025 && r.ReportMonth.Month == 1);
        Assert.AreEqual(1, reportsForJan2025.Count());
    }

    [TestMethod]
    public async Task GetMonthlyReportsAsync_ReturnsReportsInDescendingOrder()
    {
        // Arrange
        var report1 = new MonthlyReportData { Year = 2024, Month = 11, GeneratedAt = DateTime.UtcNow };
        var report2 = new MonthlyReportData { Year = 2024, Month = 12, GeneratedAt = DateTime.UtcNow };
        var report3 = new MonthlyReportData { Year = 2025, Month = 1, GeneratedAt = DateTime.UtcNow };

        await _reportService.SaveMonthlyReportAsync(report1, TestUserId);
        await _reportService.SaveMonthlyReportAsync(report2, TestUserId);
        await _reportService.SaveMonthlyReportAsync(report3, TestUserId);

        // Act
        var reports = (await _reportService.GetMonthlyReportsAsync(TestUserId)).ToList();

        // Assert
        Assert.AreEqual(3, reports.Count);
        Assert.AreEqual(2025, reports[0].ReportMonth.Year);
        Assert.AreEqual(1, reports[0].ReportMonth.Month);
        Assert.AreEqual(2024, reports[1].ReportMonth.Year);
        Assert.AreEqual(12, reports[1].ReportMonth.Month);
        Assert.AreEqual(2024, reports[2].ReportMonth.Year);
        Assert.AreEqual(11, reports[2].ReportMonth.Month);
    }

    [TestMethod]
    public async Task GetReportPreferencesAsync_WithNoPreferences_ReturnsDefaults()
    {
        // Act
        var preferences = await _reportService.GetReportPreferencesAsync(TestUserId);

        // Assert
        Assert.IsNotNull(preferences);
        Assert.AreEqual(TestUserId, preferences.UserId);
        Assert.IsFalse(preferences.SendEmail);
        Assert.IsTrue(preferences.ShowInApp);
        Assert.AreEqual(1, preferences.PreferredDeliveryDay);
        Assert.IsTrue(preferences.IsEnabled);
    }

    [TestMethod]
    public async Task SaveReportPreferencesAsync_SavesPreferences()
    {
        // Arrange
        var preferences = new ReportPreference
        {
            UserId = TestUserId,
            SendEmail = true,
            ShowInApp = true,
            EmailAddress = "test@example.com",
            PreferredDeliveryDay = 5,
            IncludeCategoryDetails = true,
            IncludeTopMerchants = true,
            IncludeBudgetComparison = false,
            IncludeTrendAnalysis = true,
            IsEnabled = true
        };

        // Act
        var saved = await _reportService.SaveReportPreferencesAsync(preferences);

        // Assert
        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.SendEmail);
        Assert.AreEqual("test@example.com", saved.EmailAddress);
        Assert.AreEqual(5, saved.PreferredDeliveryDay);
        Assert.IsFalse(saved.IncludeBudgetComparison);
    }

    [TestMethod]
    public async Task SaveReportPreferencesAsync_UpdatesExistingPreferences()
    {
        // Arrange
        var initialPrefs = new ReportPreference
        {
            UserId = TestUserId,
            SendEmail = false,
            ShowInApp = true,
            IsEnabled = true
        };
        await _reportService.SaveReportPreferencesAsync(initialPrefs);

        var updatedPrefs = new ReportPreference
        {
            UserId = TestUserId,
            SendEmail = true,
            ShowInApp = false,
            EmailAddress = "updated@example.com",
            IsEnabled = false
        };

        // Act
        var result = await _reportService.SaveReportPreferencesAsync(updatedPrefs);

        // Assert
        Assert.IsTrue(result.SendEmail);
        Assert.IsFalse(result.ShowInApp);
        Assert.AreEqual("updated@example.com", result.EmailAddress);
        Assert.IsFalse(result.IsEnabled);
    }

    #region Historical Overview Tests

    [TestMethod]
    public async Task GetHistoricalOverviewAsync_WithNoData_ReturnsZeroValues()
    {
        // Arrange
        var asOfDate = DateTime.Today.AddMonths(-1);

        // Act
        var report = await _reportService.GetHistoricalOverviewAsync(asOfDate, TestUserId);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(asOfDate.Date, report.AsOfDate.Date);
        Assert.AreEqual(0, report.NetWorth);
        Assert.AreEqual(0, report.TotalAssets);
        Assert.AreEqual(0, report.TotalLiabilities);
        Assert.AreEqual(0, report.Accounts.Count());
        Assert.AreEqual(0, report.Investments.Count());
        Assert.AreEqual(0, report.Assets.Count());
        Assert.AreEqual(0, report.Loans.Count());
    }

    [TestMethod]
    public async Task GetHistoricalOverviewAsync_WithBankAccounts_CalculatesHistoricalBalance()
    {
        // Arrange
        var asOfDate = DateTime.Today.AddMonths(-1);
        var bankSource = new BankSource
        {
            Name = "Test Bank Account",
            AccountType = "savings",
            Currency = "SEK",
            InitialBalance = 10000m,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };
        _context.BankSources.Add(bankSource);
        await _context.SaveChangesAsync();

        // Add a transaction before the as-of date
        var transaction1 = new Transaction
        {
            Amount = 1000m,
            Description = "Deposit",
            Date = asOfDate.AddDays(-10),
            IsIncome = true,
            BankSourceId = bankSource.BankSourceId,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            ValidFrom = DateTime.UtcNow.AddMonths(-2)
        };
        _context.Transactions.Add(transaction1);
        
        // Add a transaction after the as-of date (should not be included)
        var transaction2 = new Transaction
        {
            Amount = 500m,
            Description = "Later Deposit",
            Date = DateTime.Today,
            IsIncome = true,
            BankSourceId = bankSource.BankSourceId,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction2);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetHistoricalOverviewAsync(asOfDate, TestUserId);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(1, report.Accounts.Count());
        // Should include initial balance + transaction before as-of date
        Assert.AreEqual(11000m, report.Accounts[0].Balance); // 10000 + 1000
    }

    [TestMethod]
    public async Task GetHistoricalOverviewAsync_WithLoans_CalculatesLiabilities()
    {
        // Arrange
        var asOfDate = DateTime.Today.AddMonths(-1);
        var loan = new Loan
        {
            Name = "Test Loan",
            Type = "Privatlån",
            Amount = 50000m,
            InterestRate = 5.5m,
            Currency = "SEK",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            ValidFrom = DateTime.UtcNow.AddMonths(-6)
        };
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetHistoricalOverviewAsync(asOfDate, TestUserId);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(1, report.Loans.Count());
        Assert.AreEqual(50000m, report.TotalLoans);
        Assert.AreEqual(50000m, report.TotalLiabilities);
        Assert.AreEqual(-50000m, report.NetWorth);
    }

    [TestMethod]
    public async Task GetHistoricalOverviewAsync_WithInvestments_CalculatesAssets()
    {
        // Arrange
        var asOfDate = DateTime.Today.AddMonths(-1);
        var investment = new Investment
        {
            Name = "Test Stock",
            Type = "Aktie",
            Quantity = 100,
            PurchasePrice = 50m,
            CurrentPrice = 60m,
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            LastUpdated = DateTime.UtcNow.AddMonths(-2),
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            ValidFrom = DateTime.UtcNow.AddMonths(-6)
        };
        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetHistoricalOverviewAsync(asOfDate, TestUserId);

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(1, report.Investments.Count());
        Assert.AreEqual(6000m, report.TotalInvestments); // 100 * 60
    }

    [TestMethod]
    public async Task GetHistoricalOverviewAsync_IncludesMonthlyTransactions()
    {
        // Arrange
        var asOfDate = new DateTime(2025, 1, 15);
        
        // Add income transaction in the same month
        var incomeTransaction = new Transaction
        {
            Amount = 30000m,
            Description = "Salary",
            Date = new DateTime(2025, 1, 5),
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(incomeTransaction);
        
        // Add expense transaction in the same month
        var expenseTransaction = new Transaction
        {
            Amount = 5000m,
            Description = "Rent",
            Date = new DateTime(2025, 1, 10),
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        };
        _context.Transactions.Add(expenseTransaction);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetHistoricalOverviewAsync(asOfDate, TestUserId);

        // Assert
        Assert.AreEqual(30000m, report.MonthlyIncome);
        Assert.AreEqual(5000m, report.MonthlyExpenses);
        Assert.AreEqual(25000m, report.MonthlyNetFlow);
        Assert.AreEqual(2, report.TransactionCount);
    }

    [TestMethod]
    public async Task GetHistoricalOverviewAsync_IncludesComparisonWithCurrentValues()
    {
        // Arrange
        var asOfDate = DateTime.Today.AddMonths(-1);
        
        // Add an asset
        var asset = new Asset
        {
            Name = "Test Asset",
            Type = "Property",
            CurrentValue = 100000m,
            PurchaseValue = 80000m,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            ValidFrom = DateTime.UtcNow.AddMonths(-2)
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var report = await _reportService.GetHistoricalOverviewAsync(asOfDate, TestUserId);

        // Assert
        Assert.IsNotNull(report.Comparison);
        Assert.IsTrue(report.Comparison.DaysElapsed > 0);
    }

    [TestMethod]
    public async Task GetTimelineKeyDatesAsync_WithNoData_ReturnsEmptyList()
    {
        // Act
        var keyDates = await _reportService.GetTimelineKeyDatesAsync(TestUserId, 12);

        // Assert
        Assert.IsNotNull(keyDates);
        Assert.AreEqual(0, keyDates.Count());
    }

    [TestMethod]
    public async Task GetTimelineKeyDatesAsync_WithTransactions_ReturnsMonthlyDates()
    {
        // Arrange - Add transactions in different months
        for (int i = 1; i <= 3; i++)
        {
            var transaction = new Transaction
            {
                Amount = 1000m * i,
                Description = $"Transaction {i}",
                Date = DateTime.Today.AddMonths(-i),
                IsIncome = true,
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var keyDates = await _reportService.GetTimelineKeyDatesAsync(TestUserId, 12);

        // Assert
        Assert.IsNotNull(keyDates);
        Assert.AreNotEqual(0, keyDates.Count());
        Assert.IsTrue(keyDates.Count <= 12);
    }

    [TestMethod]
    public async Task GetTimelineKeyDatesAsync_WithSnapshots_IncludesPeaksAndValleys()
    {
        // Arrange - Add net worth snapshots
        var snapshots = new List<NetWorthSnapshot>
        {
            new NetWorthSnapshot
            {
                Date = DateTime.Today.AddMonths(-3),
                NetWorth = 100000m,
                TotalAssets = 120000m,
                TotalLiabilities = 20000m,
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow
            },
            new NetWorthSnapshot
            {
                Date = DateTime.Today.AddMonths(-2),
                NetWorth = 150000m, // Peak
                TotalAssets = 170000m,
                TotalLiabilities = 20000m,
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow
            },
            new NetWorthSnapshot
            {
                Date = DateTime.Today.AddMonths(-1),
                NetWorth = 80000m, // Valley
                TotalAssets = 100000m,
                TotalLiabilities = 20000m,
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.NetWorthSnapshots.AddRange(snapshots);
        await _context.SaveChangesAsync();

        // Act
        var keyDates = await _reportService.GetTimelineKeyDatesAsync(TestUserId, 12);

        // Assert
        Assert.IsNotNull(keyDates);
        Assert.AreNotEqual(0, keyDates.Count());
    }

    [TestMethod]
    public async Task GetJourneyStartInfoAsync_WithNoTransactions_ReturnsNull()
    {
        // Act
        var journeyStart = await _reportService.GetJourneyStartInfoAsync(TestUserId);

        // Assert
        Assert.IsNull(journeyStart);
    }

    [TestMethod]
    public async Task GetJourneyStartInfoAsync_WithTransactions_ReturnsCorrectStartDate()
    {
        // Arrange - Add transactions with different dates
        var earliestDate = new DateTime(2023, 9, 15);
        var laterDate = new DateTime(2024, 3, 20);
        
        _context.Transactions.Add(new Transaction
        {
            Amount = 1000m,
            Description = "Earlier transaction",
            Date = earliestDate,
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        _context.Transactions.Add(new Transaction
        {
            Amount = 2000m,
            Description = "Later transaction",
            Date = laterDate,
            IsIncome = false,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var journeyStart = await _reportService.GetJourneyStartInfoAsync(TestUserId);

        // Assert
        Assert.IsNotNull(journeyStart);
        Assert.AreEqual(earliestDate.Date, journeyStart.StartDate.Date);
        Assert.AreEqual(2, journeyStart.TotalTransactions);
        Assert.IsTrue(journeyStart.DaysTracked >= 0);
    }

    [TestMethod]
    public async Task GetJourneyStartInfoAsync_WithUserFilter_ReturnsOnlyUserTransactions()
    {
        // Arrange - Add transactions for different users
        _context.Transactions.Add(new Transaction
        {
            Amount = 1000m,
            Description = "User 1 transaction",
            Date = new DateTime(2023, 1, 1),
            IsIncome = true,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        _context.Transactions.Add(new Transaction
        {
            Amount = 2000m,
            Description = "User 2 transaction - earlier",
            Date = new DateTime(2022, 6, 15),
            IsIncome = true,
            UserId = "other-user-456",
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var journeyStart = await _reportService.GetJourneyStartInfoAsync(TestUserId);

        // Assert
        Assert.IsNotNull(journeyStart);
        Assert.AreEqual(new DateTime(2023, 1, 1).Date, journeyStart.StartDate.Date);
        Assert.AreEqual(1, journeyStart.TotalTransactions);
    }

    #endregion
}
