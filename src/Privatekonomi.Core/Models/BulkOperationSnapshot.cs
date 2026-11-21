namespace Privatekonomi.Core.Models;

/// <summary>
/// Snapshot of data before a bulk operation for undo functionality
/// </summary>
public class BulkOperationSnapshot
{
    /// <summary>
    /// Unique identifier for this snapshot
    /// </summary>
    public string OperationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of operation performed
    /// </summary>
    public BulkOperationType OperationType { get; set; }
    
    /// <summary>
    /// When the operation was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User ID who performed the operation
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Snapshot of transactions before the operation (for deletions and updates)
    /// </summary>
    public List<TransactionSnapshot>? TransactionSnapshots { get; set; }
    
    /// <summary>
    /// Transaction IDs affected by the operation
    /// </summary>
    public List<int> AffectedTransactionIds { get; set; } = new();

    public BulkOperationSnapshot()
    {
        OperationId = Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Snapshot of a single transaction's state
/// </summary>
public class TransactionSnapshot
{
    public int TransactionId { get; set; }
    public int? HouseholdId { get; set; }
    public List<int> CategoryIds { get; set; } = new();
    public List<decimal> CategoryAmounts { get; set; } = new();
    public bool IsInternalTransfer { get; set; }
    public bool IsExcludedFromTotals { get; set; }
}

/// <summary>
/// Types of bulk operations that can be undone
/// </summary>
public enum BulkOperationType
{
    Delete,
    Categorize,
    LinkHousehold,
    UpdateCategories
}
