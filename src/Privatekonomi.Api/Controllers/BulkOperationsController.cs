using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/transactions/bulk")]
public class BulkOperationsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IExportService _exportService;
    private readonly ILogger<BulkOperationsController> _logger;

    public BulkOperationsController(
        ITransactionService transactionService,
        IExportService exportService,
        ILogger<BulkOperationsController> logger)
    {
        _transactionService = transactionService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Delete multiple transactions
    /// </summary>
    [HttpPost("delete")]
    public async Task<ActionResult<BulkOperationResult>> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk delete requested for {Count} transactions", request.TransactionIds.Count);
            
            // Create snapshot before deletion (cannot be undone, but log for audit)
            var snapshot = await _transactionService.CreateOperationSnapshotAsync(
                request.TransactionIds, 
                BulkOperationType.Delete);
            
            var result = await _transactionService.BulkDeleteTransactionsAsync(request.TransactionIds);
            
            _logger.LogInformation("Bulk delete completed: {SuccessCount} succeeded, {FailureCount} failed", 
                result.SuccessCount, result.FailureCount);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete operation");
            throw new BadRequestException("Failed to delete transactions: " + ex.Message);
        }
    }

    /// <summary>
    /// Categorize multiple transactions
    /// </summary>
    [HttpPost("categorize")]
    public async Task<ActionResult<BulkOperationResult>> BulkCategorize([FromBody] BulkCategorizeRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk categorize requested for {Count} transactions with {CategoryCount} categories", 
                request.TransactionIds.Count, request.Categories.Count);
            
            var categories = request.Categories
                .Select(c => (c.CategoryId, c.Amount))
                .ToList();
            
            var result = await _transactionService.BulkCategorizeTransactionsAsync(
                request.TransactionIds, 
                categories);
            
            _logger.LogInformation("Bulk categorize completed: {SuccessCount} succeeded, {FailureCount} failed", 
                result.SuccessCount, result.FailureCount);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk categorize operation");
            throw new BadRequestException("Failed to categorize transactions: " + ex.Message);
        }
    }

    /// <summary>
    /// Link multiple transactions to a household
    /// </summary>
    [HttpPost("link-household")]
    public async Task<ActionResult<BulkOperationResult>> BulkLinkHousehold([FromBody] BulkLinkHouseholdRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk link household requested for {Count} transactions to household {HouseholdId}", 
                request.TransactionIds.Count, request.HouseholdId ?? null);
            
            var result = await _transactionService.BulkLinkToHouseholdAsync(
                request.TransactionIds, 
                request.HouseholdId);
            
            _logger.LogInformation("Bulk link household completed: {SuccessCount} succeeded, {FailureCount} failed", 
                result.SuccessCount, result.FailureCount);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk link household operation");
            throw new BadRequestException("Failed to link transactions to household: " + ex.Message);
        }
    }

    /// <summary>
    /// Export multiple transactions to CSV or JSON
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> BulkExport([FromBody] BulkExportRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk export requested for {Count} transactions in {Format} format", 
                request.TransactionIds.Count, request.Format);
            
            byte[] fileData;
            string contentType;
            string fileName;

            if (request.Format == ExportFormat.CSV)
            {
                fileData = await _exportService.ExportSelectedTransactionsToCsvAsync(request.TransactionIds);
                contentType = "text/csv";
                fileName = $"transaktioner_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            }
            else
            {
                fileData = await _exportService.ExportSelectedTransactionsToJsonAsync(request.TransactionIds);
                contentType = "application/json";
                fileName = $"transaktioner_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            }
            
            _logger.LogInformation("Bulk export completed: {ByteCount} bytes", fileData.Length);
            
            return File(fileData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk export operation");
            throw new BadRequestException("Failed to export transactions: " + ex.Message);
        }
    }

    /// <summary>
    /// Create a snapshot of transactions before an operation (for undo support)
    /// </summary>
    [HttpPost("snapshot")]
    public async Task<ActionResult<BulkOperationSnapshot>> CreateSnapshot(
        [FromQuery] BulkOperationType operationType,
        [FromBody] List<int> transactionIds)
    {
        try
        {
            _logger.LogInformation("Creating snapshot for {Count} transactions, operation type: {OperationType}", 
                transactionIds.Count, operationType);
            
            var snapshot = await _transactionService.CreateOperationSnapshotAsync(
                transactionIds, 
                operationType);
            
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating snapshot");
            throw new BadRequestException("Failed to create snapshot: " + ex.Message);
        }
    }

    /// <summary>
    /// Undo a bulk operation using a snapshot
    /// </summary>
    [HttpPost("undo")]
    public async Task<ActionResult<BulkOperationResult>> Undo([FromBody] BulkOperationSnapshot snapshot)
    {
        try
        {
            _logger.LogInformation("Undo requested for operation {OperationId}, type: {OperationType}", 
                snapshot.OperationId, snapshot.OperationType);
            
            var result = await _transactionService.UndoBulkOperationAsync(snapshot);
            
            _logger.LogInformation("Undo completed: {SuccessCount} succeeded, {FailureCount} failed", 
                result.SuccessCount, result.FailureCount);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during undo operation");
            throw new BadRequestException("Failed to undo operation: " + ex.Message);
        }
    }
}
