using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Extensions;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class TemporalEntityTests
{
    private PrivatekonomyContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    [TestMethod]
    public async Task CreateTemporalEntityShouldSetValidFromAndValidTo()
    {
        // Arrange
        var context = GetInMemoryContext();
        var service = new TemporalEntityService(context);
        var transaction = new Transaction
        {
            Amount = 100m,
            Description = "Test Transaction",
            Date = DateTime.UtcNow,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await service.CreateTemporalEntityAsync(transaction);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual(default(DateTime), result.ValidFrom);
        Assert.IsNull(result.ValidTo);
    }

    [TestMethod]
    public async Task UpdateTemporalEntityShouldCloseOldVersionAndCreateNew()
    {
        // Arrange
        var context = GetInMemoryContext();
        var service = new TemporalEntityService(context);
        var originalTransaction = new Transaction
        {
            Amount = 100m,
            Description = "Original Transaction",
            Date = DateTime.UtcNow,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = DateTime.UtcNow
        };

        var created = await service.CreateTemporalEntityAsync(originalTransaction);
        
        var updatedTransaction = new Transaction
        {
            Amount = 150m,
            Description = "Updated Transaction",
            Date = DateTime.UtcNow,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await service.UpdateTemporalEntityAsync(created, updatedTransaction);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(150m, result.Amount);
        Assert.IsNull(result.ValidTo); // New version is active
        Assert.IsNotNull(created.ValidTo); // Old version is closed
        Assert.AreEqual(result.ValidFrom, created.ValidTo); // Versions should align
    }

    [TestMethod]
    public async Task DeleteTemporalEntityShouldSetValidTo()
    {
        // Arrange
        var context = GetInMemoryContext();
        var service = new TemporalEntityService(context);
        var transaction = new Transaction
        {
            Amount = 100m,
            Description = "Test Transaction",
            Date = DateTime.UtcNow,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = DateTime.UtcNow
        };

        var created = await service.CreateTemporalEntityAsync(transaction);

        // Act
        await service.DeleteTemporalEntityAsync(created);

        // Assert
        Assert.IsNotNull(created.ValidTo);
    }

    [TestMethod]
    public async Task AsOfShouldReturnCorrectVersion()
    {
        // Arrange
        var context = GetInMemoryContext();
        var service = new TemporalEntityService(context);
        
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Create version 1 (valid from Jan 1)
        var version1 = new Transaction
        {
            TransactionId = 1,
            Amount = 100m,
            Description = "Version 1",
            Date = baseDate,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = baseDate,
            ValidFrom = baseDate,
            ValidTo = null
        };
        context.Transactions.Add(version1);
        await context.SaveChangesAsync();

        // Update to version 2 (valid from Feb 1)
        var febDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        version1.ValidTo = febDate;
        var version2 = new Transaction
        {
            TransactionId = 2,
            Amount = 200m,
            Description = "Version 2",
            Date = febDate,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = febDate,
            ValidFrom = febDate,
            ValidTo = null
        };
        context.Transactions.Add(version2);
        await context.SaveChangesAsync();

        // Act - Query as of Jan 15 (should get version 1)
        var janResult = await context.Transactions
            .AsOf(new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc))
            .Where(t => t.Description.StartsWith("Version"))
            .FirstOrDefaultAsync();

        // Query as of Feb 15 (should get version 2)
        var febResult = await context.Transactions
            .AsOf(new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc))
            .Where(t => t.Description.StartsWith("Version"))
            .FirstOrDefaultAsync();

        // Query current version (should get version 2)
        var currentResult = await context.Transactions
            .AsOf(null) // null means current
            .Where(t => t.Description.StartsWith("Version"))
            .FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(janResult);
        Assert.AreEqual(100m, janResult.Amount);
        Assert.AreEqual("Version 1", janResult.Description);

        Assert.IsNotNull(febResult);
        Assert.AreEqual(200m, febResult.Amount);
        Assert.AreEqual("Version 2", febResult.Description);

        Assert.IsNotNull(currentResult);
        Assert.AreEqual(200m, currentResult.Amount);
        Assert.AreEqual("Version 2", currentResult.Description);
    }

    [TestMethod]
    public async Task CurrentOnlyShouldReturnOnlyActiveRecords()
    {
        // Arrange
        var context = GetInMemoryContext();
        var baseDate = DateTime.UtcNow;

        // Add active transaction
        var activeTransaction = new Transaction
        {
            Amount = 100m,
            Description = "Active",
            Date = baseDate,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = baseDate,
            ValidFrom = baseDate,
            ValidTo = null // Active
        };

        // Add closed transaction
        var closedTransaction = new Transaction
        {
            Amount = 200m,
            Description = "Closed",
            Date = baseDate,
            IsIncome = true,
            Currency = "SEK",
            CreatedAt = baseDate,
            ValidFrom = baseDate.AddDays(-10),
            ValidTo = baseDate.AddDays(-5) // Closed 5 days ago
        };

        context.Transactions.AddRange(activeTransaction, closedTransaction);
        await context.SaveChangesAsync();

        // Act
        var currentTransactions = await context.Transactions
            .CurrentOnly()
            .ToListAsync();

        // Assert
        Assert.AreEqual(1, currentTransactions.Count());
        Assert.AreEqual("Active", currentTransactions[0].Description);
    }

    [TestMethod]
    public void IsActiveShouldReturnCorrectValue()
    {
        // Arrange
        var activeEntity = new Transaction
        {
            ValidFrom = DateTime.UtcNow,
            ValidTo = null
        };

        var closedEntity = new Transaction
        {
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidTo = DateTime.UtcNow.AddDays(-5)
        };

        // Act & Assert
        Assert.IsTrue(activeEntity.IsActive());
        Assert.IsFalse(closedEntity.IsActive());
    }

    [TestMethod]
    public void IsActiveAtShouldReturnCorrectValue()
    {
        // Arrange
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new Transaction
        {
            ValidFrom = baseDate,
            ValidTo = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        Assert.IsTrue(entity.IsActiveAt(new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc))); // Within range
        Assert.IsFalse(entity.IsActiveAt(new DateTime(2023, 12, 15, 0, 0, 0, DateTimeKind.Utc))); // Before
        Assert.IsFalse(entity.IsActiveAt(new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc))); // After
    }

    [TestMethod]
    public async Task GetAllVersionsShouldReturnAllVersionsOrdered()
    {
        // Arrange
        var context = GetInMemoryContext();
        var service = new TemporalEntityService(context);
        
        var baseDate = DateTime.UtcNow;
        
        // Create multiple versions
        var v1 = new Asset
        {
            AssetId = 1,
            Name = "House",
            Type = "Property",
            PurchaseValue = 100000m,
            CurrentValue = 100000m,
            Currency = "SEK",
            CreatedAt = baseDate,
            ValidFrom = baseDate.AddYears(-2),
            ValidTo = baseDate.AddYears(-1)
        };

        var v2 = new Asset
        {
            AssetId = 2,
            Name = "House",
            Type = "Property",
            PurchaseValue = 100000m,
            CurrentValue = 120000m,
            Currency = "SEK",
            CreatedAt = baseDate,
            ValidFrom = baseDate.AddYears(-1),
            ValidTo = baseDate
        };

        var v3 = new Asset
        {
            AssetId = 3,
            Name = "House",
            Type = "Property",
            PurchaseValue = 100000m,
            CurrentValue = 150000m,
            Currency = "SEK",
            CreatedAt = baseDate,
            ValidFrom = baseDate,
            ValidTo = null
        };

        context.Assets.AddRange(v1, v2, v3);
        await context.SaveChangesAsync();

        // Act
        var allVersions = await service.GetAllVersionsAsync<Asset>(a => a.Name == "House");

        // Assert
        Assert.AreEqual(3, allVersions.Count);
        Assert.AreEqual(100000m, allVersions[0].CurrentValue);
        Assert.AreEqual(120000m, allVersions[1].CurrentValue);
        Assert.AreEqual(150000m, allVersions[2].CurrentValue);
    }
}
