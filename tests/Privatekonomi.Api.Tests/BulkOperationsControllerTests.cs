using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Privatekonomi.Api.Controllers;
using Privatekonomi.Api.Models;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class BulkOperationsControllerTests
{
    private readonly FakeTransactionService _transactionService;
    private readonly FakeExportService _exportService;
    private readonly BulkOperationsController _controller;

    public BulkOperationsControllerTests()
    {
        _transactionService = new FakeTransactionService();
        _exportService = new FakeExportService();
        _controller = new BulkOperationsController(
            _transactionService,
            _exportService,
            NullLogger<BulkOperationsController>.Instance);
    }

    [TestMethod]
    public async Task BulkDelete_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new BulkDeleteRequest
        {
            TransactionIds = new List<int> { 1, 2, 3 }
        };

        var snapshot = new BulkOperationSnapshot
        {
            OperationType = BulkOperationType.Delete,
            AffectedTransactionIds = request.TransactionIds
        };

        var expectedResult = new BulkOperationResult
        {
            SuccessCount = 3,
            FailureCount = 0,
            TotalCount = 3
        };

        _transactionService.CreateOperationSnapshotAsyncImpl = (_, _) => Task.FromResult(snapshot);
        _transactionService.BulkDeleteTransactionsAsyncImpl = ids => Task.FromResult(expectedResult);

        // Act
        var result = await _controller.BulkDelete(request);

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType<BulkOperationResult>(okResult.Value);
        var bulkResult = (BulkOperationResult)okResult.Value;
        Assert.AreEqual(3, bulkResult.SuccessCount);
        Assert.AreEqual(0, bulkResult.FailureCount);
    }

    [TestMethod]
    public async Task BulkCategorize_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = new List<int> { 1, 2 },
            Categories = new List<BulkCategoryDto>
            {
                new() { CategoryId = 1, Amount = null }
            }
        };

        var expectedResult = new BulkOperationResult
        {
            SuccessCount = 2,
            FailureCount = 0,
            TotalCount = 2
        };

        _transactionService.BulkCategorizeTransactionsAsyncImpl = (ids, categories) =>
            Task.FromResult(expectedResult);

        // Act
        var result = await _controller.BulkCategorize(request);

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType<BulkOperationResult>(okResult.Value);
        var bulkResult = (BulkOperationResult)okResult.Value;
        Assert.AreEqual(2, bulkResult.SuccessCount);
    }

    [TestMethod]
    public async Task BulkLinkHousehold_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new BulkLinkHouseholdRequest
        {
            TransactionIds = new List<int> { 1, 2, 3 },
            HouseholdId = 5
        };

        var expectedResult = new BulkOperationResult
        {
            SuccessCount = 3,
            FailureCount = 0,
            TotalCount = 3
        };

        _transactionService.BulkLinkToHouseholdAsyncImpl = (ids, householdId) => Task.FromResult(expectedResult);

        // Act
        var result = await _controller.BulkLinkHousehold(request);

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType<BulkOperationResult>(okResult.Value);
        var bulkResult = (BulkOperationResult)okResult.Value;
        Assert.AreEqual(3, bulkResult.SuccessCount);
    }

    [TestMethod]
    public async Task BulkLinkHousehold_ShouldUnlink_WhenHouseholdIdIsNull()
    {
        // Arrange
        var request = new BulkLinkHouseholdRequest
        {
            TransactionIds = new List<int> { 1, 2 },
            HouseholdId = null
        };

        var expectedResult = new BulkOperationResult
        {
            SuccessCount = 2,
            FailureCount = 0,
            TotalCount = 2
        };

        _transactionService.BulkLinkToHouseholdAsyncImpl = (ids, householdId) => Task.FromResult(expectedResult);

        // Act
        var result = await _controller.BulkLinkHousehold(request);

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType<BulkOperationResult>(okResult.Value);
        var bulkResult = (BulkOperationResult)okResult.Value;
        Assert.AreEqual(2, bulkResult.SuccessCount);

        Assert.AreEqual(1, _transactionService.BulkLinkToHouseholdCallCount);
        Assert.IsNotNull(_transactionService.LastBulkLinkToHouseholdArgs);
        CollectionAssert.AreEqual(
            request.TransactionIds,
            _transactionService.LastBulkLinkToHouseholdArgs.Value.TransactionIds);
        Assert.IsNull(_transactionService.LastBulkLinkToHouseholdArgs.Value.HouseholdId);
    }

    [TestMethod]
    public async Task BulkExport_ShouldReturnCsvFile_WhenFormatIsCsv()
    {
        // Arrange
        var request = new BulkExportRequest
        {
            TransactionIds = new List<int> { 1, 2, 3 },
            Format = ExportFormat.CSV
        };

        var csvData = new byte[] { 1, 2, 3 };
        _exportService.ExportSelectedTransactionsToCsvAsyncImpl = ids => Task.FromResult(csvData);

        // Act
        var result = await _controller.BulkExport(request);

        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
        var fileResult = (FileContentResult)result;
        Assert.AreEqual("text/csv", fileResult.ContentType);
        StringAssert.Contains(fileResult.FileDownloadName, "transaktioner_");
        StringAssert.EndsWith(fileResult.FileDownloadName, ".csv");
        Assert.AreSequenceEqual(csvData, fileResult.FileContents);
    }

    [TestMethod]
    public async Task BulkExport_ShouldReturnJsonFile_WhenFormatIsJson()
    {
        // Arrange
        var request = new BulkExportRequest
        {
            TransactionIds = new List<int> { 1, 2, 3 },
            Format = ExportFormat.JSON
        };

        var jsonData = new byte[] { 1, 2, 3 };
        _exportService.ExportSelectedTransactionsToJsonAsyncImpl = ids => Task.FromResult(jsonData);

        // Act
        var result = await _controller.BulkExport(request);

        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
        var fileResult = (FileContentResult)result;
        Assert.AreEqual("application/json", fileResult.ContentType);
        StringAssert.Contains(fileResult.FileDownloadName, "transaktioner_");
        StringAssert.EndsWith(fileResult.FileDownloadName, ".json");
        Assert.AreSequenceEqual(jsonData, fileResult.FileContents);
    }

    [TestMethod]
    public async Task CreateSnapshot_ShouldReturnSnapshot()
    {
        // Arrange
        var transactionIds = new List<int> { 1, 2, 3 };
        var operationType = BulkOperationType.Categorize;

        var expectedSnapshot = new BulkOperationSnapshot
        {
            OperationType = operationType,
            AffectedTransactionIds = transactionIds,
            OperationId = "test-id"
        };

        _transactionService.CreateOperationSnapshotAsyncImpl = (_, _) => Task.FromResult(expectedSnapshot);

        // Act
        var result = await _controller.CreateSnapshot(operationType, transactionIds);

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType<BulkOperationSnapshot>(okResult.Value);
        var snapshot = (BulkOperationSnapshot)okResult.Value;
        Assert.AreEqual(operationType, snapshot.OperationType);
        Assert.AreEqual(3, snapshot.AffectedTransactionIds.Count);
    }

    [TestMethod]
    public async Task Undo_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var snapshot = new BulkOperationSnapshot
        {
            OperationType = BulkOperationType.Categorize,
            AffectedTransactionIds = new List<int> { 1, 2 },
            OperationId = "test-id"
        };

        var expectedResult = new BulkOperationResult
        {
            SuccessCount = 2,
            FailureCount = 0,
            TotalCount = 2
        };

        _transactionService.UndoBulkOperationAsyncImpl = s => Task.FromResult(expectedResult);

        // Act
        var result = await _controller.Undo(snapshot);

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        Assert.IsInstanceOfType<BulkOperationResult>(okResult.Value);
        var undoResult = (BulkOperationResult)okResult.Value;
        Assert.AreEqual(2, undoResult.SuccessCount);
    }

    [TestMethod]
    public async Task BulkCategorize_ShouldMapCategories_Correctly()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = new List<int> { 1 },
            Categories = new List<BulkCategoryDto>
            {
                new() { CategoryId = 1, Amount = 100m },
                new() { CategoryId = 2, Amount = null }
            }
        };

        _transactionService.BulkCategorizeTransactionsAsyncImpl = (ids, categories) =>
            Task.FromResult(new BulkOperationResult { SuccessCount = 1 });

        // Act
        await _controller.BulkCategorize(request);

        // Assert
        Assert.IsNotNull(_transactionService.LastBulkCategorizeCategories);
        Assert.AreEqual(2, _transactionService.LastBulkCategorizeCategories.Count);
        Assert.AreEqual(1, _transactionService.LastBulkCategorizeCategories[0].CategoryId);
        Assert.AreEqual(100m, _transactionService.LastBulkCategorizeCategories[0].Amount);
        Assert.AreEqual(2, _transactionService.LastBulkCategorizeCategories[1].CategoryId);
        Assert.IsNull(_transactionService.LastBulkCategorizeCategories[1].Amount);
    }

    private sealed class FakeTransactionService : ITransactionService
    {
        public Func<List<int>, BulkOperationType, Task<BulkOperationSnapshot>>? CreateOperationSnapshotAsyncImpl { get; set; }
        public Func<List<int>, Task<BulkOperationResult>>? BulkDeleteTransactionsAsyncImpl { get; set; }
        public Func<List<int>, List<(int CategoryId, decimal? Amount)>, Task<BulkOperationResult>>? BulkCategorizeTransactionsAsyncImpl { get; set; }
        public Func<List<int>, int?, Task<BulkOperationResult>>? BulkLinkToHouseholdAsyncImpl { get; set; }
        public Func<BulkOperationSnapshot, Task<BulkOperationResult>>? UndoBulkOperationAsyncImpl { get; set; }

        public int BulkLinkToHouseholdCallCount { get; private set; }
        public (List<int> TransactionIds, int? HouseholdId)? LastBulkLinkToHouseholdArgs { get; private set; }

        public List<(int CategoryId, decimal? Amount)>? LastBulkCategorizeCategories { get; private set; }

        public Task<BulkOperationResult> BulkDeleteTransactionsAsync(List<int> transactionIds)
        {
            if (BulkDeleteTransactionsAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for BulkDeleteTransactionsAsync");
            }

            return BulkDeleteTransactionsAsyncImpl(transactionIds);
        }

        public Task<BulkOperationResult> BulkCategorizeTransactionsAsync(List<int> transactionIds, List<(int CategoryId, decimal? Amount)> categories)
        {
            LastBulkCategorizeCategories = categories;

            if (BulkCategorizeTransactionsAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for BulkCategorizeTransactionsAsync");
            }

            return BulkCategorizeTransactionsAsyncImpl(transactionIds, categories);
        }

        public Task<BulkOperationResult> BulkLinkToHouseholdAsync(List<int> transactionIds, int? householdId)
        {
            BulkLinkToHouseholdCallCount++;
            LastBulkLinkToHouseholdArgs = (transactionIds, householdId);

            if (BulkLinkToHouseholdAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for BulkLinkToHouseholdAsync");
            }

            return BulkLinkToHouseholdAsyncImpl(transactionIds, householdId);
        }

        public Task<BulkOperationSnapshot> CreateOperationSnapshotAsync(List<int> transactionIds, BulkOperationType operationType)
        {
            if (CreateOperationSnapshotAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for CreateOperationSnapshotAsync");
            }

            return CreateOperationSnapshotAsyncImpl(transactionIds, operationType);
        }

        public Task<BulkOperationResult> UndoBulkOperationAsync(BulkOperationSnapshot snapshot)
        {
            if (UndoBulkOperationAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for UndoBulkOperationAsync");
            }

            return UndoBulkOperationAsyncImpl(snapshot);
        }

        public Task<IEnumerable<Transaction>> GetAllTransactionsAsync() => throw new NotImplementedException();
        public Task<Transaction?> GetTransactionByIdAsync(int id) => throw new NotImplementedException();
        public Task<Transaction> CreateTransactionAsync(Transaction transaction) => throw new NotImplementedException();
        public Task<Transaction> UpdateTransactionAsync(Transaction transaction) => throw new NotImplementedException();
        public Task<Transaction> UpdateTransactionWithAuditAsync(int id, decimal amount, DateTime date, string description, string? payee, string? notes, string? tags, List<(int CategoryId, decimal Amount)>? categories, DateTime? clientUpdatedAt, string? userId, string? ipAddress) => throw new NotImplementedException();
        public Task DeleteTransactionAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime to) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetUnmappedTransactionsAsync() => throw new NotImplementedException();
        public Task UpdateTransactionCategoriesAsync(int transactionId, List<TransactionCategory> categories) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAsync(int householdId) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAndDateRangeAsync(int householdId, DateTime from, DateTime to) => throw new NotImplementedException();
    }

    private sealed class FakeExportService : IExportService
    {
        public Func<List<int>, Task<byte[]>>? ExportSelectedTransactionsToCsvAsyncImpl { get; set; }
        public Func<List<int>, Task<byte[]>>? ExportSelectedTransactionsToJsonAsyncImpl { get; set; }

        public Task<byte[]> ExportSelectedTransactionsToCsvAsync(List<int> transactionIds)
        {
            if (ExportSelectedTransactionsToCsvAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for ExportSelectedTransactionsToCsvAsync");
            }

            return ExportSelectedTransactionsToCsvAsyncImpl(transactionIds);
        }

        public Task<byte[]> ExportSelectedTransactionsToJsonAsync(List<int> transactionIds)
        {
            if (ExportSelectedTransactionsToJsonAsyncImpl is null)
            {
                throw new InvalidOperationException("No implementation configured for ExportSelectedTransactionsToJsonAsync");
            }

            return ExportSelectedTransactionsToJsonAsyncImpl(transactionIds);
        }

        public Task<byte[]> ExportTransactionsToCsvAsync(DateTime? fromDate = null, DateTime? toDate = null) => throw new NotImplementedException();
        public Task<byte[]> ExportTransactionsToJsonAsync(DateTime? fromDate = null, DateTime? toDate = null) => throw new NotImplementedException();
        public Task<byte[]> ExportBudgetToCsvAsync(int budgetId) => throw new NotImplementedException();
        public Task<byte[]> ExportFullBackupAsync() => throw new NotImplementedException();
        public Task<List<int>> GetAvailableYearsAsync() => throw new NotImplementedException();
        public Task<byte[]> ExportYearDataToJsonAsync(int year) => throw new NotImplementedException();
        public Task<byte[]> ExportYearDataToCsvAsync(int year) => throw new NotImplementedException();
    }
}
