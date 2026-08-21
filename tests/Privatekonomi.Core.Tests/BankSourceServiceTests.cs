using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BankSourceServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly BankSourceService _bankSourceService;
    private readonly string _testUserId = "test-user-123";

    public BankSourceServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _bankSourceService = new BankSourceService(_context);
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
    public async Task CreateBankSourceAsync_CreatesSuccessfully()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Test Checking Account",
            AccountType = "checking",
            Currency = "SEK",
            CreatedAt = DateTime.UtcNow,
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Checking Account", result.Name);
        Assert.AreNotEqual(0, result.BankSourceId); // Should have an ID assigned
    }

    [TestMethod]
    public async Task CreateBankSourceAsync_WithAccountNumber_StoresCorrectly()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Swedbank Lönekonto",
            AccountType = "checking",
            Currency = "SEK",
            ClearingNumber = "8327",
            AccountNumber = "123456789",
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("8327", result.ClearingNumber);
        Assert.AreEqual("123456789", result.AccountNumber);
    }

    [TestMethod]
    public async Task CreateBankSourceAsync_WithChartOfAccounts_StoresCorrectly()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Business Account",
            AccountType = "checking",
            Currency = "SEK",
            ChartOfAccountsCode = "1930",
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1930", result.ChartOfAccountsCode);
    }

    [TestMethod]
    public async Task CreateBankSourceAsync_CreditCardAccount_StoresCorrectType()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Nordea MasterCard",
            AccountType = "credit_card",
            Currency = "SEK",
            Institution = "Nordea",
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("credit_card", result.AccountType);
        Assert.AreEqual("Nordea", result.Institution);
    }

    [TestMethod]
    public async Task CreateBankSourceAsync_PensionAccount_StoresCorrectType()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "SEB Pensionssparande",
            AccountType = "pension",
            Currency = "SEK",
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("pension", result.AccountType);
    }

    [TestMethod]
    public async Task CreateBankSourceAsync_LoanAccount_StoresCorrectType()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Bolån Swedbank",
            AccountType = "loan",
            Currency = "SEK",
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("loan", result.AccountType);
    }

    [TestMethod]
    public async Task GetAllBankSourcesAsync_ReturnsAllAccounts()
    {
        // Arrange
        var bankSources = new[]
        {
            new BankSource { Name = "Checking", AccountType = "checking", Currency = "SEK", UserId = _testUserId },
            new BankSource { Name = "Savings", AccountType = "savings", Currency = "SEK", UserId = _testUserId },
            new BankSource { Name = "Credit Card", AccountType = "credit_card", Currency = "SEK", UserId = _testUserId }
        };

        foreach (var bs in bankSources)
        {
            await _bankSourceService.CreateBankSourceAsync(bs);
        }

        // Act
        var result = await _bankSourceService.GetAllBankSourcesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count());
    }

    [TestMethod]
    public async Task GetBankSourceByIdAsync_ReturnsCorrectAccount()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Test Account",
            AccountType = "checking",
            Currency = "SEK",
            AccountNumber = "987654321",
            UserId = _testUserId
        };

        var created = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Act
        var result = await _bankSourceService.GetBankSourceByIdAsync(created.BankSourceId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Account", result.Name);
        Assert.AreEqual("987654321", result.AccountNumber);
    }

    [TestMethod]
    public async Task UpdateBankSourceAsync_UpdatesAccountDetails()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Original Name",
            AccountType = "checking",
            Currency = "SEK",
            UserId = _testUserId
        };

        var created = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Modify the account
        created.Name = "Updated Name";
        created.AccountNumber = "111222333";
        created.ChartOfAccountsCode = "1920";

        // Act
        var result = await _bankSourceService.UpdateBankSourceAsync(created);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Updated Name", result.Name);
        Assert.AreEqual("111222333", result.AccountNumber);
        Assert.AreEqual("1920", result.ChartOfAccountsCode);
    }

    [TestMethod]
    public async Task DeleteBankSourceAsync_RemovesAccount()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "To Be Deleted",
            AccountType = "checking",
            Currency = "SEK",
            UserId = _testUserId
        };

        var created = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Act
        await _bankSourceService.DeleteBankSourceAsync(created.BankSourceId);

        // Assert
        var deleted = await _bankSourceService.GetBankSourceByIdAsync(created.BankSourceId);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task CreateBankSourceAsync_WithOpenedDate_StoresCorrectly()
    {
        // Arrange
        var openedDate = new DateTime(2023, 1, 15);
        var bankSource = new BankSource
        {
            Name = "New Account",
            AccountType = "savings",
            Currency = "SEK",
            OpenedDate = openedDate,
            InitialBalance = 10000,
            UserId = _testUserId
        };

        // Act
        var result = await _bankSourceService.CreateBankSourceAsync(bankSource);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(openedDate, result.OpenedDate);
        Assert.AreEqual(10000, result.InitialBalance);
    }

    [TestMethod]
    public async Task UpdateBankSourceAsync_CanCloseAccount()
    {
        // Arrange
        var bankSource = new BankSource
        {
            Name = "Active Account",
            AccountType = "checking",
            Currency = "SEK",
            UserId = _testUserId
        };

        var created = await _bankSourceService.CreateBankSourceAsync(bankSource);
        
        // Close the account
        created.ClosedDate = DateTime.UtcNow;

        // Act
        var result = await _bankSourceService.UpdateBankSourceAsync(created);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.ClosedDate);
        Assert.IsTrue(result.ClosedDate <= DateTime.UtcNow);
    }
}
