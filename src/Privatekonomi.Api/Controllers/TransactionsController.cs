using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;
using Privatekonomi.Api.Models;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryRuleService _categoryRuleService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService, 
        ICategoryRuleService categoryRuleService,
        ICategoryService categoryService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _categoryRuleService = categoryRuleService;
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<TransactionListResponse>> GetTransactions(
        [FromQuery] int? accountId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? categoryId,
        [FromQuery] int? householdId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50)
    {
        var transactions = await _transactionService.GetAllTransactionsAsync();
        
        // Apply filters
        if (accountId.HasValue)
        {
            transactions = transactions.Where(t => t.BankSourceId == accountId.Value);
        }
        
        if (startDate.HasValue)
        {
            transactions = transactions.Where(t => t.Date >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            transactions = transactions.Where(t => t.Date <= endDate.Value);
        }
        
        if (categoryId.HasValue)
        {
            transactions = transactions.Where(t => 
                t.TransactionCategories.Any(tc => tc.CategoryId == categoryId.Value));
        }
        
        if (householdId.HasValue)
        {
            transactions = transactions.Where(t => t.HouseholdId == householdId.Value);
        }
        
        // Apply pagination
        var totalCount = transactions.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
        
        var paginatedTransactions = transactions
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToList();
        
        return Ok(new TransactionListResponse
        {
            Transactions = paginatedTransactions,
            Page = page,
            PerPage = perPage,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            throw new NotFoundException("Transaction", id);
        }
        return Ok(transaction);
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByDateRange(
        [FromQuery] DateTime from, 
        [FromQuery] DateTime to)
    {
        var transactions = await _transactionService.GetTransactionsByDateRangeAsync(from, to);
        return Ok(transactions);
    }

    [HttpGet("unmapped")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetUnmappedTransactions()
    {
        var transactions = await _transactionService.GetUnmappedTransactionsAsync();
        return Ok(transactions);
    }

    [HttpPut("{id}/categories")]
    public async Task<IActionResult> UpdateTransactionCategories(int id, List<TransactionCategory> categories)
    {
        await _transactionService.UpdateTransactionCategoriesAsync(id, categories);
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
    {
        var createdTransaction = await _transactionService.CreateTransactionAsync(transaction);
        return CreatedAtAction(nameof(GetTransaction), new { id = createdTransaction.TransactionId }, createdTransaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionRequest request)
    {
        try
        {
            // Convert category DTOs to tuples
            var categories = request.Categories?
                .Select(c => (c.CategoryId, c.Amount))
                .ToList();

            // Get user ID and IP address for audit logging
            var userId = User?.Identity?.Name;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var updatedTransaction = await _transactionService.UpdateTransactionWithAuditAsync(
                id,
                request.Amount,
                request.Date,
                request.Description,
                request.Payee,
                request.Notes,
                request.Tags,
                categories,
                request.UpdatedAt,
                userId,
                ipAddress);

            return Ok(updatedTransaction);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            throw new NotFoundException("Transaction", id);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked"))
        {
            throw new ForbiddenException(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            throw new ConflictException(ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        await _transactionService.DeleteTransactionAsync(id);
        return NoContent();
    }

    [HttpGet("household/{householdId}")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByHousehold(int householdId)
    {
        var transactions = await _transactionService.GetTransactionsByHouseholdAsync(householdId);
        return Ok(transactions);
    }

    [HttpGet("household/{householdId}/date-range")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByHouseholdAndDateRange(
        int householdId,
        [FromQuery] DateTime from, 
        [FromQuery] DateTime to)
    {
        var transactions = await _transactionService.GetTransactionsByHouseholdAndDateRangeAsync(householdId, from, to);
        return Ok(transactions);
    }

    /// <summary>
    /// Quick categorize a transaction with a single category.
    /// Optionally creates a categorization rule from the transaction.
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="request">Quick categorize request with category ID and optional rule creation</param>
    /// <returns>The updated transaction</returns>
    [HttpPost("{id}/categorize")]
    public async Task<ActionResult<QuickCategorizeResponse>> QuickCategorize(int id, [FromBody] QuickCategorizeRequest request)
    {
        // Get the transaction
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            throw new NotFoundException("Transaction", id);
        }

        // Verify category exists
        var category = await _categoryService.GetCategoryByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        // Update the transaction category
        var categories = new List<TransactionCategory>
        {
            new TransactionCategory
            {
                TransactionId = id,
                CategoryId = request.CategoryId,
                Amount = transaction.Amount,
                Percentage = 100
            }
        };

        await _transactionService.UpdateTransactionCategoriesAsync(id, categories);

        CategoryRule? createdRule = null;

        // Optionally create a categorization rule
        if (request.CreateRule)
        {
            var pattern = request.RulePattern ?? transaction.Description;
            
            // Create the rule
            var rule = new CategoryRule
            {
                Pattern = pattern,
                MatchType = PatternMatchType.Contains,
                CategoryId = request.CategoryId,
                Field = MatchField.Both,
                Priority = 50,
                IsActive = true,
                Description = $"Skapad från transaktion: {transaction.Description}"
            };

            try
            {
                var userId = User?.Identity?.Name;
                createdRule = await _categoryRuleService.CreateRuleAsync(rule, userId);
                _logger.LogInformation("Created categorization rule {RuleId} from transaction {TransactionId}", 
                    createdRule.CategoryRuleId, id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create rule from transaction {TransactionId}", id);
                // Don't fail the categorization if rule creation fails
            }
        }

        // Reload transaction with updated categories
        var updatedTransaction = await _transactionService.GetTransactionByIdAsync(id);

        return Ok(new QuickCategorizeResponse
        {
            Transaction = updatedTransaction!,
            CreatedRule = createdRule
        });
    }
}

public class TransactionListResponse
{
    public IEnumerable<Transaction> Transactions { get; set; } = new List<Transaction>();
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
