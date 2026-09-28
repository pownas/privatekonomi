using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BulkOperationTests
{
    private static DbContextOptions<PrivatekonomyContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [TestMethod]
    public async Task BulkDeleteTransactionsAsyncShouldDeleteMultipleTransactions()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var transactions = new List<Transaction>
        {
            new() { TransactionId = 1, Amount = 100, Description = "Test 1", Date = DateTime.Now },
            new() { TransactionId = 2, Amount = 200, Description = "Test 2", Date = DateTime.Now },
            new() { TransactionId = 3, Amount = 300, Description = "Test 3", Date = DateTime.Now }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var result = await service.BulkDeleteTransactionsAsync(new List<int> { 1, 2 });

        // Assert
        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(0, result.FailureCount);
        Assert.IsTrue(result.IsSuccess);
        await using var verifyContext = contextFactory.CreateDbContext();
        Assert.AreEqual(1, await verifyContext.Transactions.CountAsync());
    }

    [TestMethod]
    public async Task BulkDeleteTransactionsAsyncShouldNotDeleteLockedTransactions()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var transactions = new List<Transaction>
        {
            new() { TransactionId = 1, Amount = 100, Description = "Test 1", Date = DateTime.Now, IsLocked = true },
            new() { TransactionId = 2, Amount = 200, Description = "Test 2", Date = DateTime.Now }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var result = await service.BulkDeleteTransactionsAsync(new List<int> { 1, 2 });

        // Assert
        Assert.AreEqual(1, result.SuccessCount);
        Assert.AreEqual(1, result.FailureCount);
        Assert.IsTrue(result.IsPartialSuccess);
        StringAssert.Contains(result.Errors[0], "locked");
        await using var verifyContext = contextFactory.CreateDbContext();
        Assert.AreEqual(1, await verifyContext.Transactions.CountAsync());
    }

    [TestMethod]
    public async Task BulkCategorizeTransactionsAsyncShouldCategorizeMultipleTransactions()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var category = new Category { CategoryId = 1, Name = "Food", Color = "#FF0000" };
        context.Categories.Add(category);

        var transactions = new List<Transaction>
        {
            new() { TransactionId = 1, Amount = 100, Description = "Test 1", Date = DateTime.Now },
            new() { TransactionId = 2, Amount = 200, Description = "Test 2", Date = DateTime.Now }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var categories = new List<(int CategoryId, decimal? Amount)> { (1, null) };
        var result = await service.BulkCategorizeTransactionsAsync(new List<int> { 1, 2 }, categories);

        // Assert
        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(0, result.FailureCount);
        Assert.IsTrue(result.IsSuccess);
        
        await using var verifyContext = contextFactory.CreateDbContext();
        var tx1 = await verifyContext.Transactions.Include(t => t.TransactionCategories)
            .FirstAsync(t => t.TransactionId == 1);
        Assert.AreEqual(1, tx1.TransactionCategories.Count());
        Assert.AreEqual(1, tx1.TransactionCategories.First().CategoryId);
    }

    [TestMethod]
    public async Task BulkCategorizeTransactionsAsyncShouldFailWithInvalidCategory()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var transactions = new List<Transaction>
        {
            new() { TransactionId = 1, Amount = 100, Description = "Test 1", Date = DateTime.Now }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var categories = new List<(int CategoryId, decimal? Amount)> { (999, null) };
        var result = await service.BulkCategorizeTransactionsAsync(new List<int> { 1 }, categories);

        // Assert
        Assert.AreEqual(0, result.SuccessCount);
        Assert.AreNotEqual(0, result.Errors.Count());
        StringAssert.Contains(result.Errors[0], "Invalid category");
    }

    [TestMethod]
    public async Task BulkLinkToHouseholdAsyncShouldLinkMultipleTransactions()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var household = new Household { HouseholdId = 1, Name = "Test Household" };
        context.Households.Add(household);

        var transactions = new List<Transaction>
        {
            new() { TransactionId = 1, Amount = 100, Description = "Test 1", Date = DateTime.Now },
            new() { TransactionId = 2, Amount = 200, Description = "Test 2", Date = DateTime.Now }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var result = await service.BulkLinkToHouseholdAsync(new List<int> { 1, 2 }, 1);

        // Assert
        Assert.AreEqual(2, result.SuccessCount);
        Assert.AreEqual(0, result.FailureCount);
        Assert.IsTrue(result.IsSuccess);
        
        await using var verifyContext = contextFactory.CreateDbContext();
        var tx1 = await verifyContext.Transactions.FindAsync(1);
        Assert.AreEqual(1, tx1!.HouseholdId);
    }

    [TestMethod]
    public async Task BulkLinkToHouseholdAsyncShouldUnlinkWhenHouseholdIdIsNull()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var household = new Household { HouseholdId = 1, Name = "Test Household" };
        context.Households.Add(household);

        var transactions = new List<Transaction>
        {
            new() { TransactionId = 1, Amount = 100, Description = "Test 1", Date = DateTime.Now, HouseholdId = 1 }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var result = await service.BulkLinkToHouseholdAsync(new List<int> { 1 }, null);

        // Assert
        Assert.AreEqual(1, result.SuccessCount);
        Assert.IsTrue(result.IsSuccess);
        
        await using var verifyContext = contextFactory.CreateDbContext();
        var tx1 = await verifyContext.Transactions.FindAsync(1);
        Assert.IsNull(tx1!.HouseholdId);
    }

    [TestMethod]
    public async Task CreateOperationSnapshotAsyncShouldCreateSnapshot()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns("user123");
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var category = new Category { CategoryId = 1, Name = "Food", Color = "#FF0000" };
        context.Categories.Add(category);

        var transaction = new Transaction 
        { 
            TransactionId = 1, 
            Amount = 100, 
            Description = "Test", 
            Date = DateTime.Now,
            HouseholdId = 5
        };
        context.Transactions.Add(transaction);

        var transactionCategory = new TransactionCategory
        {
            TransactionId = 1,
            CategoryId = 1,
            Amount = 100
        };
        context.TransactionCategories.Add(transactionCategory);
        
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Act
        var snapshot = await service.CreateOperationSnapshotAsync(new List<int> { 1 }, 
            BulkOperationType.Categorize);

        // Assert
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(BulkOperationType.Categorize, snapshot.OperationType);
        Assert.AreEqual("user123", snapshot.UserId);
        Assert.AreEqual(1, snapshot.AffectedTransactionIds.Count());
        Assert.AreEqual(1, snapshot.TransactionSnapshots!.Count());
        Assert.AreEqual(5, snapshot.TransactionSnapshots![0].HouseholdId);
        Assert.AreEqual(1, snapshot.TransactionSnapshots[0].CategoryIds.Count());
        Assert.AreEqual(1, snapshot.TransactionSnapshots[0].CategoryIds[0]);
    }

    [TestMethod]
    public async Task UndoBulkOperationAsyncShouldRestoreCategorization()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var category1 = new Category { CategoryId = 1, Name = "Food", Color = "#FF0000" };
        var category2 = new Category { CategoryId = 2, Name = "Transport", Color = "#00FF00" };
        context.Categories.AddRange(category1, category2);

        var transaction = new Transaction 
        { 
            TransactionId = 1, 
            Amount = 100, 
            Description = "Test", 
            Date = DateTime.Now
        };
        context.Transactions.Add(transaction);

        var originalCategory = new TransactionCategory
        {
            TransactionId = 1,
            CategoryId = 1,
            Amount = 100
        };
        context.TransactionCategories.Add(originalCategory);
        
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        // Create snapshot before change
        var snapshot = await service.CreateOperationSnapshotAsync(new List<int> { 1 }, 
            BulkOperationType.Categorize);

        // Change categorization
        await service.BulkCategorizeTransactionsAsync(new List<int> { 1 }, 
            new List<(int CategoryId, decimal? Amount)> { (2, 100) });

        // Verify change
        await using var verifyChangeContext = contextFactory.CreateDbContext();
        var txAfterChange = await verifyChangeContext.Transactions.Include(t => t.TransactionCategories)
            .FirstAsync(t => t.TransactionId == 1);
        Assert.AreEqual(2, txAfterChange.TransactionCategories.First().CategoryId);

        // Act - Undo
        var result = await service.UndoBulkOperationAsync(snapshot);

        // Assert
        Assert.AreEqual(1, result.SuccessCount);
        Assert.IsTrue(result.IsSuccess);
        
        await using var verifyUndoContext = contextFactory.CreateDbContext();
        var txAfterUndo = await verifyUndoContext.Transactions.Include(t => t.TransactionCategories)
            .FirstAsync(t => t.TransactionId == 1);
        Assert.AreEqual(1, txAfterUndo.TransactionCategories.First().CategoryId);
    }

    [TestMethod]
    public async Task UndoBulkOperationAsyncShouldNotUndoDelete()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        var snapshot = new BulkOperationSnapshot
        {
            OperationType = BulkOperationType.Delete,
            AffectedTransactionIds = new List<int> { 1, 2 }
        };

        // Act
        var result = await service.UndoBulkOperationAsync(snapshot);

        // Assert
        Assert.AreEqual(2, result.FailureCount);
        Assert.AreNotEqual(0, result.Errors.Count());
        StringAssert.Contains(result.Errors[0], "Cannot undo delete");
    }

    [TestMethod]
    public async Task BulkDeleteTransactionsAsyncShouldHandleLargeNumberOfTransactions()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var contextFactory = new TestDbContextFactory(options);
        await using var context = new PrivatekonomyContext(options);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCategoryRuleService = new Mock<ICategoryRuleService>();
        var mockAuditLogService = new Mock<IAuditLogService>();

        // Create 150 transactions to test performance requirement (100+ transactions)
        var transactions = Enumerable.Range(1, 150).Select(i => new Transaction
        {
            TransactionId = i,
            Amount = i * 10,
            Description = $"Test {i}",
            Date = DateTime.Now.AddDays(-i)
        }).ToList();

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var service = new TransactionService(contextFactory, mockCategoryRuleService.Object, 
            mockAuditLogService.Object, mockCurrentUserService.Object);

        var transactionIds = Enumerable.Range(1, 150).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await service.BulkDeleteTransactionsAsync(transactionIds);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(150, result.SuccessCount);
        Assert.AreEqual(0, result.FailureCount);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Bulk delete took {stopwatch.ElapsedMilliseconds}ms, should be < 2000ms");
        await using var verifyContext = contextFactory.CreateDbContext();
        Assert.AreEqual(0, await verifyContext.Transactions.CountAsync());
    }
}
