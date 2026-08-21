using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.ML;

namespace Privatekonomi.Core.Services;

public class TransactionService : ITransactionService
{
    private readonly IDbContextFactory<PrivatekonomyContext> _contextFactory;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ICategoryRuleService _categoryRuleService;
    private readonly IAuditLogService _auditLogService;
    private readonly ITransactionMLService? _mlService;
    private readonly IRoundUpService? _roundUpService;
    private readonly ILogger<TransactionService>? _logger;

    public TransactionService(
        IDbContextFactory<PrivatekonomyContext> contextFactory,
        ICategoryRuleService categoryRuleService,
        IAuditLogService auditLogService,
        ICurrentUserService? currentUserService = null,
        ITransactionMLService? mlService = null,
        IRoundUpService? roundUpService = null,
        ILogger<TransactionService>? logger = null)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _categoryRuleService = categoryRuleService;
        _auditLogService = auditLogService;
        _mlService = mlService;
        _roundUpService = roundUpService;
        _logger = logger;
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Include(t => t.Receipts)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Include(t => t.Receipts)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.FirstOrDefaultAsync(t => t.TransactionId == id);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        using var context = _contextFactory.CreateDbContext();
        // Set audit fields
        transaction.CreatedAt = DateTime.UtcNow;
        
        // Set user ID for new transactions
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            transaction.UserId = _currentUserService.UserId;
        }
        
        // Auto-categorize based on ML, rules, or similar descriptions if no categories assigned
        if (!transaction.TransactionCategories.Any())
        {
            Category? suggestedCategory = null;
            
            // First try ML-based categorization if available
            if (_mlService != null && !string.IsNullOrEmpty(transaction.UserId))
            {
                var mlPrediction = await _mlService.PredictCategoryAsync(transaction, transaction.UserId);
                
                // Use ML prediction if confidence is high enough
                if (mlPrediction != null && !mlPrediction.IsUncertain)
                {
                    suggestedCategory = await context.Categories
                        .FirstOrDefaultAsync(c => c.Name == mlPrediction.Category);
                }
            }
            
            // Fall back to rule-based categorization
            if (suggestedCategory == null)
            {
                var ruleCategoryId = await _categoryRuleService.ApplyCategoryRulesAsync(
                    transaction.Description, 
                    transaction.Payee);
                
                if (ruleCategoryId.HasValue)
                {
                    suggestedCategory = await context.Categories.FindAsync(ruleCategoryId.Value);
                }
                else
                {
                    // Fall back to similarity-based categorization
                    suggestedCategory = await SuggestCategoryAsync(transaction.Description, context);
                }
            }
            
            if (suggestedCategory != null)
            {
                transaction.TransactionCategories.Add(new TransactionCategory
                {
                    CategoryId = suggestedCategory.CategoryId,
                    Amount = transaction.Amount
                });
            }
        }

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        
        // Process round-up if service is available
        if (_roundUpService != null)
        {
            try
            {
                if (transaction.IsIncome)
                {
                    // Process salary auto-save for income transactions
                    await _roundUpService.ProcessSalaryAutoSaveAsync(transaction.TransactionId);
                }
                else
                {
                    // Process round-up for expense transactions
                    await _roundUpService.ProcessRoundUpForTransactionAsync(transaction.TransactionId);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail transaction creation
                _logger?.LogWarning(ex, "Failed to process round-up for transaction {TransactionId}", transaction.TransactionId);
            }
        }
        
        return transaction;
    }

    public async Task<Transaction> UpdateTransactionAsync(Transaction transaction)
    {
        using var context = _contextFactory.CreateDbContext();
        transaction.UpdatedAt = DateTime.UtcNow;
        context.Entry(transaction).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateTransactionWithAuditAsync(
        int id,
        decimal amount,
        DateTime transactionDate,
        string description,
        string? payee,
        string? notes,
        string? tags,
        List<(int CategoryId, decimal Amount)>? categories,
        DateTime? clientUpdatedAt,
        string? userId,
        string? ipAddress)
    {
        using var context = _contextFactory.CreateDbContext();
        // Fetch the existing transaction with related data
        var transaction = await context.Transactions
            .Include(t => t.TransactionCategories)
            .FirstOrDefaultAsync(t => t.TransactionId == id);

        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction with ID {id} not found");
        }

        // Check if transaction is locked
        if (transaction.IsLocked)
        {
            throw new InvalidOperationException($"Transaction {id} is locked and cannot be edited");
        }

        // Optimistic locking check
        if (clientUpdatedAt.HasValue && transaction.UpdatedAt.HasValue)
        {
            // Compare timestamps (allow small tolerance for precision differences)
            var timeDifference = Math.Abs((transaction.UpdatedAt.Value - clientUpdatedAt.Value).TotalSeconds);
            if (timeDifference > 1) // More than 1 second difference indicates concurrent modification
            {
                throw new InvalidOperationException(
                    $"Transaction {id} has been modified by another user. Please refresh and try again.");
            }
        }

        // Input validation
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required", nameof(description));
        }

        // Create a copy of the original for audit logging
        var originalTransaction = new Transaction
        {
            TransactionId = transaction.TransactionId,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Description = transaction.Description,
            Payee = transaction.Payee,
            Notes = transaction.Notes,
            Tags = transaction.Tags,
            UpdatedAt = transaction.UpdatedAt
        };

        // Update transaction fields
        transaction.Amount = amount;
        transaction.Date = transactionDate;
        transaction.Description = description;
        transaction.Payee = payee;
        transaction.Notes = notes;
        transaction.Tags = tags;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Update categories if provided
        if (categories != null)
        {
            // Remove existing categories
            context.TransactionCategories.RemoveRange(transaction.TransactionCategories);
            
            // Add new categories
            transaction.TransactionCategories = categories
                .Select(c => new TransactionCategory
                {
                    TransactionId = id,
                    CategoryId = c.CategoryId,
                    Amount = c.Amount
                })
                .ToList();
        }

        await context.SaveChangesAsync();

        // Create audit log
        await _auditLogService.LogTransactionUpdateAsync(
            originalTransaction,
            transaction,
            userId,
            ipAddress);

        return transaction;
    }

    public async Task DeleteTransactionAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var transaction = await context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime toDate)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Where(t => t.Date >= from && t.Date <= toDate)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    private async Task<Category?> SuggestCategoryAsync(string description, PrivatekonomyContext? contextOverride = null)
    {
        var context = contextOverride ?? _contextFactory.CreateDbContext();
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var prefixLength = Math.Min(3, description.Length);
        var prefix = description[..prefixLength];

        // Find similar transactions and suggest the most common category
        var query = context.Transactions
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => t.Description != null && EF.Functions.Like(t.Description, $"%{prefix}%"))
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        var similarTransactions = await query.ToListAsync();

        if (similarTransactions.Any())
        {
            var mostCommonCategory = similarTransactions
                .SelectMany(t => t.TransactionCategories)
                .GroupBy(tc => tc.CategoryId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (mostCommonCategory != null)
            {
                return await context.Categories.FindAsync(mostCommonCategory.Key);
            }
        }

        return null;
    }

    public async Task<IEnumerable<Transaction>> GetUnmappedTransactionsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Where(t => !t.TransactionCategories.Any())
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task UpdateTransactionCategoriesAsync(int transactionId, List<TransactionCategory> categories)
    {
        using var context = _contextFactory.CreateDbContext();
        var transaction = await context.Transactions
            .Include(t => t.TransactionCategories)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

        if (transaction == null)
        {
            throw new ArgumentException($"Transaction with ID {transactionId} not found");
        }

        // Remove existing categories
        context.TransactionCategories.RemoveRange(transaction.TransactionCategories);

        // Add new categories
        foreach (var category in categories)
        {
            category.TransactionId = transactionId;
            context.TransactionCategories.Add(category);
        }

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAsync(int householdId)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Where(t => t.HouseholdId == householdId)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAndDateRangeAsync(int householdId, DateTime from, DateTime toDate)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Where(t => t.HouseholdId == householdId && t.Date >= from && t.Date <= toDate)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<BulkOperationResult> BulkDeleteTransactionsAsync(List<int> transactionIds)
    {
        using var context = _contextFactory.CreateDbContext();
        var result = new BulkOperationResult
        {
            TotalCount = transactionIds.Count,
            OperationType = "Delete",
            OperationId = Guid.NewGuid().ToString()
        };

        foreach (var id in transactionIds)
        {
            try
            {
                var transaction = await context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} not found");
                    continue;
                }

                // Check if transaction is locked
                if (transaction.IsLocked)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} is locked and cannot be deleted");
                    continue;
                }

                // Check user ownership if authenticated
                if (_currentUserService?.IsAuthenticated == true && 
                    _currentUserService.UserId != null && 
                    transaction.UserId != _currentUserService.UserId)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} does not belong to current user");
                    continue;
                }

                context.Transactions.Remove(transaction);
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.Errors.Add($"Error deleting transaction {id}: {ex.Message}");
            }
        }

        if (result.SuccessCount > 0)
        {
            await context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<BulkOperationResult> BulkCategorizeTransactionsAsync(
        List<int> transactionIds, 
        List<(int CategoryId, decimal? Amount)> categories)
    {
        using var context = _contextFactory.CreateDbContext();
        var result = new BulkOperationResult
        {
            TotalCount = transactionIds.Count,
            OperationType = "Categorize",
            OperationId = Guid.NewGuid().ToString()
        };

        // Validate categories exist
        var categoryIds = categories.Select(c => c.CategoryId).Distinct().ToList();
        var existingCategories = await context.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .Select(c => c.CategoryId)
            .ToListAsync();

        var invalidCategoryIds = categoryIds.Except(existingCategories).ToList();
        if (invalidCategoryIds.Any())
        {
            result.Errors.Add($"Invalid category IDs: {string.Join(", ", invalidCategoryIds)}");
            return result;
        }

        foreach (var id in transactionIds)
        {
            try
            {
                var transaction = await context.Transactions
                    .Include(t => t.TransactionCategories)
                    .FirstOrDefaultAsync(t => t.TransactionId == id);

                if (transaction == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} not found");
                    continue;
                }

                // Check if transaction is locked
                if (transaction.IsLocked)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} is locked and cannot be edited");
                    continue;
                }

                // Check user ownership if authenticated
                if (_currentUserService?.IsAuthenticated == true && 
                    _currentUserService.UserId != null && 
                    transaction.UserId != _currentUserService.UserId)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} does not belong to current user");
                    continue;
                }

                // Remove existing categories
                context.TransactionCategories.RemoveRange(transaction.TransactionCategories);

                // Add new categories
                foreach (var category in categories)
                {
                    var amount = category.Amount ?? transaction.Amount;
                    context.TransactionCategories.Add(new TransactionCategory
                    {
                        TransactionId = id,
                        CategoryId = category.CategoryId,
                        Amount = amount
                    });
                }

                transaction.UpdatedAt = DateTime.UtcNow;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.Errors.Add($"Error categorizing transaction {id}: {ex.Message}");
            }
        }

        if (result.SuccessCount > 0)
        {
            await context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<BulkOperationResult> BulkLinkToHouseholdAsync(List<int> transactionIds, int? householdId)
    {
        using var context = _contextFactory.CreateDbContext();
        var result = new BulkOperationResult
        {
            TotalCount = transactionIds.Count,
            OperationType = "LinkHousehold",
            OperationId = Guid.NewGuid().ToString()
        };

        // Validate household exists if provided
        if (householdId.HasValue)
        {
            var householdExists = await context.Households.AnyAsync(h => h.HouseholdId == householdId.Value);
            if (!householdExists)
            {
                result.Errors.Add($"Household {householdId.Value} not found");
                return result;
            }
        }

        foreach (var id in transactionIds)
        {
            try
            {
                var transaction = await context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} not found");
                    continue;
                }

                // Check if transaction is locked
                if (transaction.IsLocked)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} is locked and cannot be edited");
                    continue;
                }

                // Check user ownership if authenticated
                if (_currentUserService?.IsAuthenticated == true && 
                    _currentUserService.UserId != null && 
                    transaction.UserId != _currentUserService.UserId)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.Errors.Add($"Transaction {id} does not belong to current user");
                    continue;
                }

                transaction.HouseholdId = householdId;
                transaction.UpdatedAt = DateTime.UtcNow;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.Errors.Add($"Error linking transaction {id} to household: {ex.Message}");
            }
        }

        if (result.SuccessCount > 0)
        {
            await context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<BulkOperationSnapshot> CreateOperationSnapshotAsync(
        List<int> transactionIds, 
        BulkOperationType operationType)
    {
        using var context = _contextFactory.CreateDbContext();
        var snapshot = new BulkOperationSnapshot
        {
            OperationType = operationType,
            AffectedTransactionIds = transactionIds,
            UserId = _currentUserService?.UserId
        };

        var transactions = await context.Transactions
            .Include(t => t.TransactionCategories)
            .Where(t => transactionIds.Contains(t.TransactionId))
            .ToListAsync();

        snapshot.TransactionSnapshots = transactions.Select(t => new TransactionSnapshot
        {
            TransactionId = t.TransactionId,
            HouseholdId = t.HouseholdId,
            CategoryIds = t.TransactionCategories.Select(tc => tc.CategoryId).ToList(),
            CategoryAmounts = t.TransactionCategories.Select(tc => tc.Amount).ToList()
        }).ToList();

        return snapshot;
    }

    public async Task<BulkOperationResult> UndoBulkOperationAsync(BulkOperationSnapshot snapshot)
    {
        using var context = _contextFactory.CreateDbContext();
        var result = new BulkOperationResult
        {
            TotalCount = snapshot.AffectedTransactionIds.Count,
            OperationType = $"Undo{snapshot.OperationType}",
            OperationId = Guid.NewGuid().ToString()
        };

        switch (snapshot.OperationType)
        {
            case BulkOperationType.Delete:
                // Cannot undo deletions - transactions are gone
                result.Errors.Add("Cannot undo delete operations - data has been permanently removed");
                result.FailureCount = snapshot.AffectedTransactionIds.Count;
                result.FailedIds.AddRange(snapshot.AffectedTransactionIds);
                break;

            case BulkOperationType.Categorize:
            case BulkOperationType.UpdateCategories:
                await UndoCategorizeAsync(snapshot, result, context);
                break;

            case BulkOperationType.LinkHousehold:
                await UndoLinkHouseholdAsync(snapshot, result, context);
                break;

            default:
                result.Errors.Add($"Unknown operation type: {snapshot.OperationType}");
                result.FailureCount = snapshot.AffectedTransactionIds.Count;
                result.FailedIds.AddRange(snapshot.AffectedTransactionIds);
                break;
        }

        return result;
    }

    private async Task UndoCategorizeAsync(BulkOperationSnapshot snapshot, BulkOperationResult result, PrivatekonomyContext context)
    {
        if (snapshot.TransactionSnapshots == null)
        {
            result.Errors.Add("No snapshot data available for undo");
            result.FailureCount = snapshot.AffectedTransactionIds.Count;
            result.FailedIds.AddRange(snapshot.AffectedTransactionIds);
            return;
        }

        foreach (var txSnapshot in snapshot.TransactionSnapshots)
        {
            try
            {
                var transaction = await context.Transactions
                    .Include(t => t.TransactionCategories)
                    .FirstOrDefaultAsync(t => t.TransactionId == txSnapshot.TransactionId);

                if (transaction == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(txSnapshot.TransactionId);
                    result.Errors.Add($"Transaction {txSnapshot.TransactionId} not found");
                    continue;
                }

                // Remove current categories
                context.TransactionCategories.RemoveRange(transaction.TransactionCategories);

                // Restore original categories
                for (int i = 0; i < txSnapshot.CategoryIds.Count; i++)
                {
                    context.TransactionCategories.Add(new TransactionCategory
                    {
                        TransactionId = txSnapshot.TransactionId,
                        CategoryId = txSnapshot.CategoryIds[i],
                        Amount = txSnapshot.CategoryAmounts[i]
                    });
                }

                transaction.UpdatedAt = DateTime.UtcNow;
                result.SuccessCount++;
                result.SuccessfulIds.Add(txSnapshot.TransactionId);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(txSnapshot.TransactionId);
                result.Errors.Add($"Error undoing categorization for transaction {txSnapshot.TransactionId}: {ex.Message}");
            }
        }

        if (result.SuccessCount > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    private async Task UndoLinkHouseholdAsync(BulkOperationSnapshot snapshot, BulkOperationResult result, PrivatekonomyContext context)
    {
        if (snapshot.TransactionSnapshots == null)
        {
            result.Errors.Add("No snapshot data available for undo");
            result.FailureCount = snapshot.AffectedTransactionIds.Count;
            result.FailedIds.AddRange(snapshot.AffectedTransactionIds);
            return;
        }

        foreach (var txSnapshot in snapshot.TransactionSnapshots)
        {
            try
            {
                var transaction = await context.Transactions.FindAsync(txSnapshot.TransactionId);
                if (transaction == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(txSnapshot.TransactionId);
                    result.Errors.Add($"Transaction {txSnapshot.TransactionId} not found");
                    continue;
                }

                transaction.HouseholdId = txSnapshot.HouseholdId;
                transaction.UpdatedAt = DateTime.UtcNow;
                result.SuccessCount++;
                result.SuccessfulIds.Add(txSnapshot.TransactionId);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(txSnapshot.TransactionId);
                result.Errors.Add($"Error undoing household link for transaction {txSnapshot.TransactionId}: {ex.Message}");
            }
        }

        if (result.SuccessCount > 0)
        {
            await context.SaveChangesAsync();
        }
    }
}
