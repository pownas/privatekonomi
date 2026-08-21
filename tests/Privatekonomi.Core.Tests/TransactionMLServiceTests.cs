using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.ML;
using Privatekonomi.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class TransactionMLServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<ILogger<TransactionMLService>> _mockLogger;
    private readonly TransactionMLService _mlService;
    private readonly string _testUserId = "test-user-123";
    private readonly string _testModelPath;

    public TransactionMLServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _mockLogger = new Mock<ILogger<TransactionMLService>>();
        _testModelPath = Path.Combine(Path.GetTempPath(), "ml_test_models_" + Guid.NewGuid().ToString());
        _mlService = new TransactionMLService(_context, _mockLogger.Object, _testModelPath);
        
        Directory.CreateDirectory(_testModelPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test model directory
        if (Directory.Exists(_testModelPath))
        {
            Directory.Delete(_testModelPath, true);
        }

        Dispose();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task IsModelTrainedAsync_NoModel_ReturnsFalse()
    {
        // Act
        var result = await _mlService.IsModelTrainedAsync(_testUserId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task TrainModelAsync_InsufficientData_ReturnsNull()
    {
        // Arrange - Create less than 50 transactions
        await SeedTransactionsAsync(_testUserId, 30);

        // Act
        var result = await _mlService.TrainModelAsync(_testUserId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task TrainModelAsync_SufficientData_ReturnsMetrics()
    {
        // Arrange - Create sufficient transactions with multiple categories
        await SeedTransactionsAsync(_testUserId, 100);

        // Act
        var result = await _mlService.TrainModelAsync(_testUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Accuracy >= 0 && result.Accuracy <= 1);
        Assert.IsTrue(result.MicroAccuracy >= 0 && result.MicroAccuracy <= 1);
    }

    [TestMethod]
    public async Task TrainModelAsync_SavesModelMetadata()
    {
        // Arrange
        await SeedTransactionsAsync(_testUserId, 100);

        // Act
        var result = await _mlService.TrainModelAsync(_testUserId);

        // Assert
        var modelMetadata = await _context.MLModels
            .FirstOrDefaultAsync(m => m.UserId == _testUserId);
        
        Assert.IsNotNull(modelMetadata);
        Assert.AreEqual(_testUserId, modelMetadata.UserId);
        Assert.IsTrue(modelMetadata.TrainingRecordsCount > 0);
        Assert.IsTrue(File.Exists(modelMetadata.ModelPath));
    }

    [TestMethod]
    public async Task PredictCategoryAsync_NoModel_ReturnsNull()
    {
        // Arrange
        var transaction = new Transaction
        {
            Description = "ICA Maxi",
            Amount = 285m,
            Date = DateTime.UtcNow,
            UserId = _testUserId
        };

        // Act
        var result = await _mlService.PredictCategoryAsync(transaction, _testUserId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task PredictCategoryAsync_WithTrainedModel_ReturnsPrediction()
    {
        // Arrange
        await SeedTransactionsAsync(_testUserId, 100);
        await _mlService.TrainModelAsync(_testUserId);

        var transaction = new Transaction
        {
            Description = "ICA Supermarket",
            Amount = 285m,
            Date = DateTime.UtcNow,
            UserId = _testUserId
        };

        // Act
        var result = await _mlService.PredictCategoryAsync(transaction, _testUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.Category));
        Assert.IsTrue(result.Confidence >= 0 && result.Confidence <= 1);
    }

    [TestMethod]
    public async Task UpdateModelWithFeedbackAsync_StoresFeedback()
    {
        // Arrange
        var transaction = new Transaction
        {
            TransactionId = 1,
            Description = "Test Transaction",
            Amount = 100m,
            Date = DateTime.UtcNow,
            UserId = _testUserId
        };
        
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        await _mlService.UpdateModelWithFeedbackAsync(
            _testUserId, 
            transaction, 
            "Mat & Dryck", 
            "Shopping", 
            0.65f);

        // Assert
        var feedback = await _context.UserFeedbacks
            .FirstOrDefaultAsync(f => f.TransactionId == transaction.TransactionId);
        
        Assert.IsNotNull(feedback);
        Assert.AreEqual(_testUserId, feedback.UserId);
        Assert.AreEqual("Shopping", feedback.PredictedCategory);
        Assert.AreEqual("Mat & Dryck", feedback.ActualCategory);
        Assert.IsTrue(feedback.WasCorrectionNeeded);
        Assert.AreEqual(0.65f, feedback.PredictedConfidence);
    }

    [TestMethod]
    public async Task IsModelTrainedAsync_AfterTraining_ReturnsTrue()
    {
        // Arrange
        await SeedTransactionsAsync(_testUserId, 100);
        await _mlService.TrainModelAsync(_testUserId);

        // Act
        var result = await _mlService.IsModelTrainedAsync(_testUserId);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task PredictBatchAsync_ReturnsMultiplePredictions()
    {
        // Arrange
        await SeedTransactionsAsync(_testUserId, 100);
        await _mlService.TrainModelAsync(_testUserId);

        var transactions = new List<Transaction>
        {
            new() { Description = "ICA", Amount = 250m, Date = DateTime.UtcNow, UserId = _testUserId },
            new() { Description = "SL", Amount = 890m, Date = DateTime.UtcNow, UserId = _testUserId },
            new() { Description = "Netflix", Amount = 139m, Date = DateTime.UtcNow, UserId = _testUserId }
        };

        // Act
        var results = await _mlService.PredictBatchAsync(transactions, _testUserId);

        // Assert
        Assert.AreNotEqual(0, results.Count());
        foreach (var r in results)
        {
            Assert.IsFalse(string.IsNullOrEmpty(r.Category));
        }
    }

    // Helper methods

    private async Task SeedTransactionsAsync(string userId, int count)
    {
        // Create some test categories
        var categories = new List<Category>
        {
            new() { CategoryId = 1, Name = "Mat & Dryck", Color = "#FF0000" },
            new() { CategoryId = 2, Name = "Transport", Color = "#00FF00" },
            new() { CategoryId = 3, Name = "Nöje", Color = "#0000FF" },
            new() { CategoryId = 4, Name = "Shopping", Color = "#FFFF00" },
            new() { CategoryId = 5, Name = "Boende", Color = "#FF00FF" }
        };
        
        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();

        // Create transactions with patterns
        var random = new Random(42); // Fixed seed for reproducibility
        var transactions = new List<Transaction>();

        for (int i = 0; i < count; i++)
        {
            var categoryIndex = i % categories.Count;
            var category = categories[categoryIndex];
            
            var description = categoryIndex switch
            {
                0 => $"ICA {random.Next(1, 100)}",
                1 => $"SL Kort {random.Next(1, 100)}",
                2 => $"Netflix {random.Next(1, 100)}",
                3 => $"H&M {random.Next(1, 100)}",
                4 => $"Hyra {random.Next(1, 100)}",
                _ => $"Other {random.Next(1, 100)}"
            };

            var transaction = new Transaction
            {
                Description = description,
                Amount = random.Next(50, 500),
                Date = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            transactions.Add(transaction);
        }

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Add transaction categories
        var transactionCategories = new List<TransactionCategory>();
        foreach (var transaction in transactions)
        {
            var categoryIndex = transactions.IndexOf(transaction) % categories.Count;
            transactionCategories.Add(new TransactionCategory
            {
                TransactionId = transaction.TransactionId,
                CategoryId = categories[categoryIndex].CategoryId,
                Amount = transaction.Amount
            });
        }

        _context.TransactionCategories.AddRange(transactionCategories);
        await _context.SaveChangesAsync();
    }
}
