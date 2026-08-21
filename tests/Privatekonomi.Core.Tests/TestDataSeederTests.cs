using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class TestDataSeederTests : IDisposable
{
    private const int ExpectedCategorizedTransactions = 300;
    private const int ExpectedUnmappedTransactions = 5;
    private const int ExpectedAdditionalTransactions = 300;
    private const int ExpectedTotalTransactions = ExpectedCategorizedTransactions + ExpectedUnmappedTransactions + ExpectedAdditionalTransactions;
    private const int ExpectedDateRangeDays = 730; // Approximately 2 years
    private const int MinimumExpectedDateRangeDays = 650; // Allow some margin for randomness
    
    private readonly PrivatekonomyContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

    public TestDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);

        // Setup mock UserManager
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var optionsAccessorMock = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<ApplicationUser>>();
        var userValidatorMock = new Mock<IUserValidator<ApplicationUser>>();
        var passwordValidatorMock = new Mock<IPasswordValidator<ApplicationUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var serviceProviderMock = new Mock<System.IServiceProvider>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>();

        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, 
            optionsAccessorMock.Object, 
            passwordHasherMock.Object, 
            new[] { userValidatorMock.Object }, 
            new[] { passwordValidatorMock.Object }, 
            keyNormalizerMock.Object, 
            errorsMock.Object, 
            serviceProviderMock.Object, 
            loggerMock.Object);
        
        // Setup FindByEmailAsync to return null (user doesn't exist)
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        
        // Setup CreateAsync to succeed and set the user ID
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) => 
            {
                user.Id = Guid.NewGuid().ToString();
            });
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
    public async Task SeedTestDataAsync_GeneratesTransactionsWithin2YearsDateRange()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var expectedMinDate = today.AddDays(-ExpectedDateRangeDays);
        
        // Act
        await TestDataSeeder.SeedTestDataAsync(_context, _mockUserManager.Object);
        
        // Assert
        var transactions = await _context.Transactions.ToListAsync();
        
        // Should have generated the expected number of transactions
        Assert.AreEqual(ExpectedTotalTransactions, transactions.Count, "The total number of transactions generated is incorrect.");
        
        // Verify all transaction dates are within the expected range
        foreach (var transaction in transactions)
        {
            Assert.IsTrue(transaction.Date >= expectedMinDate, 
                $"Transaction date {transaction.Date:yyyy-MM-dd} is before expected minimum {expectedMinDate:yyyy-MM-dd}");
            Assert.IsTrue(transaction.Date <= today, 
                $"Transaction date {transaction.Date:yyyy-MM-dd} is after today {today:yyyy-MM-dd}");
        }
        
        // Verify we have transactions spread across the date range
        var oldestTransaction = transactions.Min(t => t.Date);
        var newestTransaction = transactions.Max(t => t.Date);
        var dateRangeDays = (newestTransaction - oldestTransaction).TotalDays;
        
        // We expect the range to be close to ExpectedDateRangeDays (with some margin for randomness)
        Assert.IsTrue(dateRangeDays >= MinimumExpectedDateRangeDays, 
            $"Date range should be at least {MinimumExpectedDateRangeDays} days, but was {dateRangeDays:F0} days");
    }

    [TestMethod]
    public async Task SeedTestDataAsync_GeneratesCorrectNumberOfTransactions()
    {
        // Act
        await TestDataSeeder.SeedTestDataAsync(_context, _mockUserManager.Object);
        
        // Assert
        var transactions = await _context.Transactions.ToListAsync();
        
        // Should have the expected total (categorized + unmapped transactions)
        Assert.AreEqual(ExpectedTotalTransactions, transactions.Count);
    }

    [TestMethod]
    public async Task SeedTestDataAsync_TransactionDatesAreRelativeToCurrentDate()
    {
        // Arrange
        var testStartTime = DateTime.UtcNow;
        
        // Act
        await TestDataSeeder.SeedTestDataAsync(_context, _mockUserManager.Object);
        
        // Assert
        var transactions = await _context.Transactions.ToListAsync();
        var testEndTime = DateTime.UtcNow;
        
        // All transactions should have dates between approximately 18 months ago and now
        var expectedMinDate = testStartTime.AddDays(-ExpectedDateRangeDays);
        var expectedMaxDate = testEndTime;
        
        foreach (var transaction in transactions)
        {
            Assert.IsTrue(transaction.Date >= expectedMinDate.AddDays(-1), // Small margin for test execution time
                $"Transaction date {transaction.Date:yyyy-MM-dd HH:mm:ss} should be after {expectedMinDate:yyyy-MM-dd HH:mm:ss}");
            Assert.IsTrue(transaction.Date <= expectedMaxDate.AddDays(1), // Small margin for test execution time
                $"Transaction date {transaction.Date:yyyy-MM-dd HH:mm:ss} should be before {expectedMaxDate:yyyy-MM-dd HH:mm:ss}");
        }
    }

    [TestMethod]
    public void SeedProductionReferenceData_SeedsChallengeTemplates()
    {
        // Act
        TestDataSeeder.SeedProductionReferenceData(_context);
        
        // Assert
        var templates = _context.ChallengeTemplates.ToList();
        
        // Verify that challenge templates were seeded
        Assert.AreNotEqual(0, templates.Count());
        
        // Verify that at least some expected templates exist
        Assert.IsTrue(templates.Any(t => t.Name == "No Spend Weekend"));
        Assert.IsTrue(templates.Any(t => t.Name == "Matlåda varje dag"));
        Assert.IsTrue(templates.Any(t => t.Type == ChallengeType.NoSpendWeekend));
        Assert.IsTrue(templates.Any(t => t.Type == ChallengeType.LunchBox));
        
        // Verify all templates are active by default
        foreach (var t in templates) { Assert.IsTrue(t.IsActive); }
    }

    [TestMethod]
    public void SeedProductionReferenceData_DoesNotDuplicateTemplates()
    {
        // Act - Seed twice
        TestDataSeeder.SeedProductionReferenceData(_context);
        var firstCount = _context.ChallengeTemplates.Count();
        
        TestDataSeeder.SeedProductionReferenceData(_context);
        var secondCount = _context.ChallengeTemplates.Count();
        
        // Assert - Count should be the same after second seeding
        Assert.AreEqual(firstCount, secondCount);
    }

    [TestMethod]
    public void SeedProductionReferenceData_TemplatesHaveRequiredProperties()
    {
        // Act
        TestDataSeeder.SeedProductionReferenceData(_context);
        
        // Assert
        var templates = _context.ChallengeTemplates.ToList();
        
        foreach (var template in templates)
        {
            // Verify each template has required properties
            Assert.IsFalse(string.IsNullOrEmpty(template.Name), "Template should have a name");
            Assert.IsFalse(string.IsNullOrEmpty(template.Description), "Template should have a description");
            Assert.IsFalse(string.IsNullOrEmpty(template.Icon), "Template should have an icon");
            Assert.IsTrue(template.DurationDays > 0, "Template should have a positive duration");
            Assert.IsTrue(template.Difficulty >= DifficultyLevel.VeryEasy && template.Difficulty <= DifficultyLevel.VeryHard, 
                "Template should have a valid difficulty level");
        }
    }
}
