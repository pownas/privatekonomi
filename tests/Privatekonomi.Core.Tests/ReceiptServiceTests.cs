using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class ReceiptServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly ReceiptService _receiptService;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private const string TestUserId = "test-user-123";

    public ReceiptServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _receiptService = new ReceiptService(_context, _auditLogServiceMock.Object);
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
    public async Task CreateReceiptAsync_WithLineItems_SavesSuccessfully()
    {
        // Arrange
        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Test Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 150.00m,
            Currency = "SEK",
            ReceiptType = "Physical",
            ReceiptLineItems = new List<ReceiptLineItem>
            {
                new ReceiptLineItem
                {
                    Description = "Product 1",
                    Quantity = 2,
                    UnitPrice = 50.00m,
                    TotalPrice = 100.00m
                },
                new ReceiptLineItem
                {
                    Description = "Product 2",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    TotalPrice = 50.00m
                }
            }
        };

        // Act
        var result = await _receiptService.CreateReceiptAsync(receipt);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ReceiptId > 0);
        Assert.AreEqual("Test Store", result.Merchant);
        Assert.AreEqual(2, result.ReceiptLineItems.Count);
        Assert.AreEqual(150.00m, result.TotalAmount);
    }

    [TestMethod]
    public async Task CreateReceiptAsync_WithImagePath_SavesImagePath()
    {
        // Arrange
        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Photo Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 99.99m,
            Currency = "SEK",
            ReceiptType = "Scanned",
            ImagePath = "receipt-12345.jpg"
        };

        // Act
        var result = await _receiptService.CreateReceiptAsync(receipt);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("receipt-12345.jpg", result.ImagePath);
        Assert.AreEqual("Scanned", result.ReceiptType);
    }

    [TestMethod]
    public async Task GetReceiptByIdAsync_WithLineItemsAndCategories_LoadsAllData()
    {
        // Arrange
        var category = new Category { Name = "Groceries", Color = "#FF5733" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Grocery Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "E-Receipt",
            ReceiptNumber = "RCP-001",
            PaymentMethod = "Credit Card",
            Notes = "Weekly shopping",
            ReceiptLineItems = new List<ReceiptLineItem>
            {
                new ReceiptLineItem
                {
                    Description = "Milk",
                    Quantity = 2,
                    UnitPrice = 25.00m,
                    TotalPrice = 50.00m,
                    CategoryId = category.CategoryId
                }
            }
        };
        
        var created = await _receiptService.CreateReceiptAsync(receipt);

        // Act
        var result = await _receiptService.GetReceiptByIdAsync(created.ReceiptId, TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Grocery Store", result.Merchant);
        Assert.AreEqual("RCP-001", result.ReceiptNumber);
        Assert.AreEqual("Credit Card", result.PaymentMethod);
        Assert.AreEqual("Weekly shopping", result.Notes);
        Assert.AreEqual(1, result.ReceiptLineItems.Count());
        Assert.IsNotNull(result.ReceiptLineItems[0].Category);
        Assert.AreEqual("Groceries", result.ReceiptLineItems[0].Category!.Name);
    }

    [TestMethod]
    public async Task UpdateReceiptAsync_UpdatesAllFields()
    {
        // Arrange
        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Old Store",
            ReceiptDate = DateTime.Today.AddDays(-1),
            TotalAmount = 50.00m,
            Currency = "SEK",
            ReceiptType = "Physical"
        };
        
        var created = await _receiptService.CreateReceiptAsync(receipt);

        // Act
        created.Merchant = "New Store";
        created.TotalAmount = 75.00m;
        created.PaymentMethod = "Cash";
        var result = await _receiptService.UpdateReceiptAsync(created);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("New Store", result.Merchant);
        Assert.AreEqual(75.00m, result.TotalAmount);
        Assert.AreEqual("Cash", result.PaymentMethod);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task GetReceiptsAsync_ReturnsOnlyUserReceipts()
    {
        // Arrange
        var receipt1 = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Store A",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "Physical"
        };
        
        var receipt2 = new Receipt
        {
            UserId = "other-user",
            Merchant = "Store B",
            ReceiptDate = DateTime.Today,
            TotalAmount = 200.00m,
            Currency = "SEK",
            ReceiptType = "Physical"
        };

        await _receiptService.CreateReceiptAsync(receipt1);
        await _receiptService.CreateReceiptAsync(receipt2);

        // Act
        var results = await _receiptService.GetReceiptsAsync(TestUserId);

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("Store A", results[0].Merchant);
    }

    [TestMethod]
    public void LineItemTotalCalculation_QuantityTimesUnitPrice_CalculatesCorrectly()
    {
        // Arrange
        var lineItem = new ReceiptLineItem
        {
            Description = "Test Product",
            Quantity = 3.5m,
            UnitPrice = 29.99m,
            TotalPrice = 0
        };

        // Act - Simulate the calculation that happens in the UI
        lineItem.TotalPrice = lineItem.Quantity * lineItem.UnitPrice;

        // Assert
        Assert.AreEqual(104.965m, lineItem.TotalPrice);
    }

    [TestMethod]
    public void LineItemTotalCalculation_WhenQuantityChanges_UpdatesTotal()
    {
        // Arrange
        var lineItem = new ReceiptLineItem
        {
            Description = "Test Product",
            Quantity = 2,
            UnitPrice = 50.00m,
            TotalPrice = 100.00m
        };

        // Act - User changes quantity from 2 to 5
        lineItem.Quantity = 5;
        lineItem.TotalPrice = lineItem.Quantity * lineItem.UnitPrice;

        // Assert
        Assert.AreEqual(250.00m, lineItem.TotalPrice);
    }

    [TestMethod]
    public async Task DeleteReceiptAsync_RemovesReceiptAndLineItems()
    {
        // Arrange
        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Test Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "Physical",
            ReceiptLineItems = new List<ReceiptLineItem>
            {
                new ReceiptLineItem
                {
                    Description = "Item 1",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m
                }
            }
        };
        
        var created = await _receiptService.CreateReceiptAsync(receipt);

        // Act
        await _receiptService.DeleteReceiptAsync(created.ReceiptId, TestUserId);

        // Assert
        var result = await _receiptService.GetReceiptByIdAsync(created.ReceiptId, TestUserId);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetReceiptsByTransactionIdAsync_ReturnsOnlyReceiptsForTransaction()
    {
        // Arrange
        var transaction = new Transaction
        {
            UserId = TestUserId,
            Description = "Test Transaction",
            Amount = 100.00m,
            Date = DateTime.Today,
            IsIncome = false,
            ValidFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var receipt1 = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Store A",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "Physical",
            TransactionId = transaction.TransactionId
        };

        var receipt2 = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Store B",
            ReceiptDate = DateTime.Today,
            TotalAmount = 50.00m,
            Currency = "SEK",
            ReceiptType = "Physical"
            // No TransactionId set
        };

        await _receiptService.CreateReceiptAsync(receipt1);
        await _receiptService.CreateReceiptAsync(receipt2);

        // Act
        var results = await _receiptService.GetReceiptsByTransactionIdAsync(transaction.TransactionId, TestUserId);

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("Store A", results[0].Merchant);
        Assert.AreEqual(transaction.TransactionId, results[0].TransactionId);
    }

    [TestMethod]
    public async Task LinkReceiptToTransactionAsync_LinksReceiptSuccessfully()
    {
        // Arrange
        var transaction = new Transaction
        {
            UserId = TestUserId,
            Description = "Test Transaction",
            Amount = 100.00m,
            Date = DateTime.Today,
            IsIncome = false,
            ValidFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Test Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "Physical"
        };
        var created = await _receiptService.CreateReceiptAsync(receipt);

        // Act
        await _receiptService.LinkReceiptToTransactionAsync(created.ReceiptId, transaction.TransactionId, TestUserId);

        // Assert
        var updated = await _receiptService.GetReceiptByIdAsync(created.ReceiptId, TestUserId);
        Assert.IsNotNull(updated);
        Assert.AreEqual(transaction.TransactionId, updated.TransactionId);
        Assert.IsNotNull(updated.UpdatedAt);
        
        _auditLogServiceMock.Verify(
            x => x.LogAsync("Link", "Receipt", created.ReceiptId, 
                It.IsAny<string>(), TestUserId, null),
            Times.Once);
    }

    [TestMethod]
    public async Task LinkReceiptToTransactionAsync_ThrowsWhenReceiptNotFound()
    {
        // Arrange
        var transaction = new Transaction
        {
            UserId = TestUserId,
            Description = "Test Transaction",
            Amount = 100.00m,
            Date = DateTime.Today,
            IsIncome = false,
            ValidFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act & Assert
        try
        {
            await _receiptService.LinkReceiptToTransactionAsync(999, transaction.TransactionId, TestUserId);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task LinkReceiptToTransactionAsync_ThrowsWhenTransactionNotFound()
    {
        // Arrange
        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Test Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "Physical"
        };
        var created = await _receiptService.CreateReceiptAsync(receipt);

        // Act & Assert
        try
        {
            await _receiptService.LinkReceiptToTransactionAsync(created.ReceiptId, 999, TestUserId);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task UnlinkReceiptFromTransactionAsync_UnlinksReceiptSuccessfully()
    {
        // Arrange
        var transaction = new Transaction
        {
            UserId = TestUserId,
            Description = "Test Transaction",
            Amount = 100.00m,
            Date = DateTime.Today,
            IsIncome = false,
            ValidFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var receipt = new Receipt
        {
            UserId = TestUserId,
            Merchant = "Test Store",
            ReceiptDate = DateTime.Today,
            TotalAmount = 100.00m,
            Currency = "SEK",
            ReceiptType = "Physical",
            TransactionId = transaction.TransactionId
        };
        var created = await _receiptService.CreateReceiptAsync(receipt);

        // Act
        await _receiptService.UnlinkReceiptFromTransactionAsync(created.ReceiptId, TestUserId);

        // Assert
        var updated = await _receiptService.GetReceiptByIdAsync(created.ReceiptId, TestUserId);
        Assert.IsNotNull(updated);
        Assert.IsNull(updated.TransactionId);
        Assert.IsNotNull(updated.UpdatedAt);
        
        _auditLogServiceMock.Verify(
            x => x.LogAsync("Unlink", "Receipt", created.ReceiptId, 
                It.IsAny<string>(), TestUserId, null),
            Times.Once);
    }

    [TestMethod]
    public async Task UnlinkReceiptFromTransactionAsync_ThrowsWhenReceiptNotFound()
    {
        // Act & Assert
        try
        {
            await _receiptService.UnlinkReceiptFromTransactionAsync(999, TestUserId);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }
}
