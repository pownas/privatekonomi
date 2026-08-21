using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetAllTransactionsAsync();
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<Transaction> CreateTransactionAsync(Transaction transaction);
    Task<Transaction> UpdateTransactionAsync(Transaction transaction);
    
    /// <summary>
    /// Updates a transaction with optimistic locking and audit logging
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="amount">New amount</param>
    /// <param name="date">New date</param>
    /// <param name="description">New description</param>
    /// <param name="payee">New payee</param>
    /// <param name="notes">New notes</param>
    /// <param name="tags">New tags</param>
    /// <param name="categories">New categories with split amounts</param>
    /// <param name="clientUpdatedAt">Client's UpdatedAt timestamp for optimistic locking</param>
    /// <param name="userId">User ID for audit logging</param>
    /// <param name="ipAddress">IP address for audit logging</param>
    /// <returns>Updated transaction</returns>
    Task<Transaction> UpdateTransactionWithAuditAsync(
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
        string? ipAddress);
    
    Task DeleteTransactionAsync(int id);
    Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime toDate);
    Task<IEnumerable<Transaction>> GetUnmappedTransactionsAsync();
    Task UpdateTransactionCategoriesAsync(int transactionId, List<TransactionCategory> categories);
    Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAsync(int householdId);
    Task<IEnumerable<Transaction>> GetTransactionsByHouseholdAndDateRangeAsync(int householdId, DateTime from, DateTime toDate);
    
    // Bulk operations
    Task<BulkOperationResult> BulkDeleteTransactionsAsync(List<int> transactionIds);
    Task<BulkOperationResult> BulkCategorizeTransactionsAsync(List<int> transactionIds, List<(int CategoryId, decimal? Amount)> categories);
    Task<BulkOperationResult> BulkLinkToHouseholdAsync(List<int> transactionIds, int? householdId);
    Task<BulkOperationSnapshot> CreateOperationSnapshotAsync(List<int> transactionIds, BulkOperationType operationType);
    Task<BulkOperationResult> UndoBulkOperationAsync(BulkOperationSnapshot snapshot);
}
