using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using System.Text;
using System.Text.Json;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BulkExportTests
{
    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    [TestMethod]
    public async Task ExportSelectedTransactionsToCsvAsyncShouldExportSelectedTransactions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var category = new Category { CategoryId = 1, Name = "Food", Color = "#FF0000" };
        var bankSource = new BankSource { BankSourceId = 1, Name = "Test Bank", Color = "#0000FF" };
        context.Categories.Add(category);
        context.BankSources.Add(bankSource);

        var transactions = new List<Transaction>
        {
            new() 
            { 
                TransactionId = 1, 
                Amount = 100.50m, 
                Description = "Grocery shopping", 
                Date = new DateTime(2024, 1, 15),
                IsIncome = false,
                BankSourceId = 1
            },
            new() 
            { 
                TransactionId = 2, 
                Amount = 200.75m, 
                Description = "Restaurant", 
                Date = new DateTime(2024, 1, 16),
                IsIncome = false,
                BankSourceId = 1
            },
            new() 
            { 
                TransactionId = 3, 
                Amount = 300, 
                Description = "Should not be exported", 
                Date = new DateTime(2024, 1, 17),
                IsIncome = false
            }
        };

        context.Transactions.AddRange(transactions);
        
        var transactionCategory = new TransactionCategory
        {
            TransactionId = 1,
            CategoryId = 1,
            Amount = 100.50m
        };
        context.TransactionCategories.Add(transactionCategory);
        
        await context.SaveChangesAsync();

        var service = new ExportService(context, mockCurrentUserService.Object);

        // Act
        var csvData = await service.ExportSelectedTransactionsToCsvAsync(new List<int> { 1, 2 });

        // Assert
        Assert.IsNotNull(csvData);
        Assert.IsTrue(csvData.Length > 0);

        // Skip BOM and convert to string
        var csvString = Encoding.UTF8.GetString(csvData, 3, csvData.Length - 3);
        
        // Verify header
        StringAssert.Contains(csvString, "Datum,Beskrivning,Belopp,Typ,Bank,Kategorier");
        
        // Verify data
        StringAssert.Contains(csvString, "2024-01-15");
        StringAssert.Contains(csvString, "Grocery shopping");
        StringAssert.Contains(csvString, "100.50");
        StringAssert.Contains(csvString, "2024-01-16");
        StringAssert.Contains(csvString, "Restaurant");
        
        // Verify third transaction is not exported
        Assert.IsFalse(csvString.Contains("Should not be exported"));
    }

    [TestMethod]
    public async Task ExportSelectedTransactionsToJsonAsyncShouldExportSelectedTransactions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var category = new Category { CategoryId = 1, Name = "Food", Color = "#FF0000" };
        var bankSource = new BankSource { BankSourceId = 1, Name = "Test Bank", Color = "#0000FF" };
        var household = new Household { HouseholdId = 1, Name = "Test Household" };
        
        context.Categories.Add(category);
        context.BankSources.Add(bankSource);
        context.Households.Add(household);

        var transactions = new List<Transaction>
        {
            new() 
            { 
                TransactionId = 1, 
                Amount = 100.50m, 
                Description = "Grocery shopping", 
                Date = new DateTime(2024, 1, 15),
                IsIncome = false,
                BankSourceId = 1,
                HouseholdId = 1,
                Tags = "food,essential"
            },
            new() 
            { 
                TransactionId = 2, 
                Amount = 200.75m, 
                Description = "Restaurant", 
                Date = new DateTime(2024, 1, 16),
                IsIncome = false,
                BankSourceId = 1
            }
        };

        context.Transactions.AddRange(transactions);
        
        var transactionCategory = new TransactionCategory
        {
            TransactionId = 1,
            CategoryId = 1,
            Amount = 100.50m
        };
        context.TransactionCategories.Add(transactionCategory);
        
        await context.SaveChangesAsync();

        var service = new ExportService(context, mockCurrentUserService.Object);

        // Act
        var jsonData = await service.ExportSelectedTransactionsToJsonAsync(new List<int> { 1, 2 });

        // Assert
        Assert.IsNotNull(jsonData);
        Assert.IsTrue(jsonData.Length > 0);

        var jsonString = Encoding.UTF8.GetString(jsonData);
        var exportData = JsonSerializer.Deserialize<JsonElement>(jsonString);
        
        Assert.IsTrue(exportData.TryGetProperty("transactionCount", out var countProp));
        Assert.AreEqual(2, countProp.GetInt32());
        
        Assert.IsTrue(exportData.TryGetProperty("transactions", out var txProp));
        var transactions_array = txProp.EnumerateArray().ToList();
        Assert.AreEqual(2, transactions_array.Count);
        
        var firstTx = transactions_array[0];
        Assert.AreEqual(1, firstTx.GetProperty("transactionId").GetInt32());
        Assert.AreEqual("Grocery shopping", firstTx.GetProperty("description").GetString());
        Assert.AreEqual("food,essential", firstTx.GetProperty("tags").GetString());
    }

    [TestMethod]
    public async Task ExportSelectedTransactionsToCsvAsyncShouldHandleSpecialCharacters()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var transactions = new List<Transaction>
        {
            new() 
            { 
                TransactionId = 1, 
                Amount = 100, 
                Description = "Test with \"quotes\" and, commas", 
                Date = DateTime.Now,
                IsIncome = false,
                Notes = "Swedish characters: åäö ÅÄÖ"
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new ExportService(context, mockCurrentUserService.Object);

        // Act
        var csvData = await service.ExportSelectedTransactionsToCsvAsync(new List<int> { 1 });

        // Assert
        var csvString = Encoding.UTF8.GetString(csvData);
        
        // Verify special characters are properly escaped
        StringAssert.Contains(csvString, "Test with \"\"quotes\"\" and, commas");
        StringAssert.Contains(csvString, "åäö ÅÄÖ");
    }

    [TestMethod]
    public async Task ExportSelectedTransactionsAsyncShouldFilterByCurrentUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        mockCurrentUserService.Setup(s => s.UserId).Returns("user123");

        var transactions = new List<Transaction>
        {
            new() 
            { 
                TransactionId = 1, 
                Amount = 100, 
                Description = "User 123 transaction", 
                Date = DateTime.Now,
                UserId = "user123"
            },
            new() 
            { 
                TransactionId = 2, 
                Amount = 200, 
                Description = "Other user transaction", 
                Date = DateTime.Now,
                UserId = "otheruser"
            }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new ExportService(context, mockCurrentUserService.Object);

        // Act
        var csvData = await service.ExportSelectedTransactionsToCsvAsync(new List<int> { 1, 2 });
        var csvString = Encoding.UTF8.GetString(csvData);

        // Assert
        StringAssert.Contains(csvString, "User 123 transaction");
        Assert.IsFalse(csvString.Contains("Other user transaction"));
    }

    [TestMethod]
    public async Task ExportSelectedTransactionsToJsonAsyncShouldIncludeAllRelevantFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var category = new Category { CategoryId = 1, Name = "Food", Color = "#FF0000" };
        context.Categories.Add(category);

        var transaction = new Transaction 
        { 
            TransactionId = 1, 
            Amount = 100.50m, 
            Description = "Test transaction", 
            Date = new DateTime(2024, 1, 15),
            IsIncome = false,
            Currency = "SEK",
            Payee = "Test Payee",
            Tags = "tag1,tag2",
            Notes = "Test notes",
            PaymentMethod = "Swish",
            OCR = "123456",
            IsRecurring = true,
            Cleared = true,
            ImportSource = "Manual"
        };

        context.Transactions.Add(transaction);
        
        var transactionCategory = new TransactionCategory
        {
            TransactionId = 1,
            CategoryId = 1,
            Amount = 100.50m
        };
        context.TransactionCategories.Add(transactionCategory);
        
        await context.SaveChangesAsync();

        var service = new ExportService(context, mockCurrentUserService.Object);

        // Act
        var jsonData = await service.ExportSelectedTransactionsToJsonAsync(new List<int> { 1 });
        var jsonString = Encoding.UTF8.GetString(jsonData);
        var exportData = JsonSerializer.Deserialize<JsonElement>(jsonString);

        // Assert
        var txArray = exportData.GetProperty("transactions").EnumerateArray().ToList();
        var tx = txArray[0];
        
        Assert.AreEqual("Test transaction", tx.GetProperty("description").GetString());
        Assert.AreEqual("SEK", tx.GetProperty("currency").GetString());
        Assert.AreEqual("Test Payee", tx.GetProperty("payee").GetString());
        Assert.AreEqual("tag1,tag2", tx.GetProperty("tags").GetString());
        Assert.AreEqual("Test notes", tx.GetProperty("notes").GetString());
        Assert.AreEqual("Swish", tx.GetProperty("paymentMethod").GetString());
        Assert.AreEqual("123456", tx.GetProperty("ocr").GetString());
        Assert.IsTrue(tx.GetProperty("isRecurring").GetBoolean());
        Assert.IsTrue(tx.GetProperty("cleared").GetBoolean());
        Assert.AreEqual("Manual", tx.GetProperty("importSource").GetString());
    }
}
