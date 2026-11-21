using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Extensions;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Example service demonstrating how to use temporal tracking for historical data queries.
/// This service extends TransactionService with temporal query capabilities.
/// </summary>
public class HistoricalTransactionService
{
    private readonly PrivatekonomyContext _context;
    private readonly TemporalEntityService _temporalService;
    private readonly ICurrentUserService? _currentUserService;

    public HistoricalTransactionService(
        PrivatekonomyContext context,
        TemporalEntityService temporalService,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _temporalService = temporalService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Gets all transactions as they existed at a specific date.
    /// If asOfDate is null, returns current/active transactions.
    /// </summary>
    public async Task<IEnumerable<Transaction>> GetTransactionsAsOfDateAsync(DateTime? asOfDate = null)
    {
        var query = _context.Transactions
            .AsOf(asOfDate) // Apply temporal filter
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    /// <summary>
    /// Gets the complete history of a transaction (all versions).
    /// </summary>
    public async Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int transactionId)
    {
        // Get all versions of this transaction
        // Note: In a real implementation, you'd need a way to link versions
        // For now, we demonstrate the concept
        var history = await _temporalService.GetAllVersionsAsync<Transaction>(
            t => t.TransactionId == transactionId
        );

        return history;
    }

    /// <summary>
    /// Creates a new transaction with temporal tracking.
    /// </summary>
    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        // Ensure user is set
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            transaction.UserId = _currentUserService.UserId;
        }

        transaction.CreatedAt = DateTime.UtcNow;

        // Create with temporal tracking
        return await _temporalService.CreateTemporalEntityAsync(transaction);
    }

    /// <summary>
    /// Updates a transaction by closing the old version and creating a new one.
    /// This preserves the history of changes.
    /// </summary>
    public async Task<Transaction?> UpdateTransactionAsync(int id, Transaction updatedData)
    {
        // Get the current version
        var current = await _context.Transactions
            .CurrentOnly()
            .FirstOrDefaultAsync(t => t.TransactionId == id);

        if (current == null)
        {
            return null;
        }

        // Verify user ownership
        if (_currentUserService?.IsAuthenticated == true && 
            _currentUserService.UserId != null && 
            current.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this transaction");
        }

        // Create new version with updated data
        var newVersion = new Transaction
        {
            Amount = updatedData.Amount,
            Description = updatedData.Description,
            Date = updatedData.Date,
            IsIncome = updatedData.IsIncome,
            Currency = updatedData.Currency,
            BankSourceId = updatedData.BankSourceId,
            PocketId = updatedData.PocketId,
            HouseholdId = updatedData.HouseholdId,
            UserId = current.UserId,
            CreatedAt = DateTime.UtcNow,
            // Copy other fields as needed
            Payee = updatedData.Payee,
            Tags = updatedData.Tags,
            Notes = updatedData.Notes,
            Imported = current.Imported,
            ImportSource = current.ImportSource,
            Cleared = updatedData.Cleared
        };

        // Use temporal service to close old version and create new one
        return await _temporalService.UpdateTemporalEntityAsync(current, newVersion);
    }

    /// <summary>
    /// Soft deletes a transaction by setting its ValidTo date.
    /// The transaction remains in the database for historical queries.
    /// </summary>
    public async Task<bool> DeleteTransactionAsync(int id)
    {
        // Get the current version
        var current = await _context.Transactions
            .CurrentOnly()
            .FirstOrDefaultAsync(t => t.TransactionId == id);

        if (current == null)
        {
            return false;
        }

        // Verify user ownership
        if (_currentUserService?.IsAuthenticated == true && 
            _currentUserService.UserId != null && 
            current.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this transaction");
        }

        // Soft delete with temporal tracking
        await _temporalService.DeleteTemporalEntityAsync(current);
        return true;
    }

    /// <summary>
    /// Gets income and expense totals as they were at a specific date.
    /// Useful for "time travel" dashboard views.
    /// </summary>
    public async Task<(decimal TotalIncome, decimal TotalExpenses, decimal NetAmount)> GetTotalsAsOfDateAsync(
        DateTime? asOfDate = null)
    {
        var transactions = await GetTransactionsAsOfDateAsync(asOfDate);

        var totalIncome = transactions
            .Where(t => t.IsIncome && !t.IsExcludedFromTotalsTransaction)
            .Sum(t => t.Amount);
        var totalExpenses = transactions
            .Where(t => !t.IsIncome && !t.IsExcludedFromTotalsTransaction)
            .Sum(t => t.Amount);
        var netAmount = totalIncome - totalExpenses;

        return (totalIncome, totalExpenses, netAmount);
    }

    /// <summary>
    /// Compares financial data between two dates.
    /// Shows how the economic situation changed over time.
    /// </summary>
    public async Task<FinancialComparison> ComparePeriodsAsync(DateTime fromDate, DateTime toDate)
    {
        var oldData = await GetTotalsAsOfDateAsync(fromDate);
        var newData = await GetTotalsAsOfDateAsync(toDate);

        return new FinancialComparison
        {
            FromDate = fromDate,
            ToDate = toDate,
            OldIncome = oldData.TotalIncome,
            NewIncome = newData.TotalIncome,
            IncomeChange = newData.TotalIncome - oldData.TotalIncome,
            OldExpenses = oldData.TotalExpenses,
            NewExpenses = newData.TotalExpenses,
            ExpensesChange = newData.TotalExpenses - oldData.TotalExpenses,
            OldNetAmount = oldData.NetAmount,
            NewNetAmount = newData.NetAmount,
            NetAmountChange = newData.NetAmount - oldData.NetAmount
        };
    }
}

/// <summary>
/// Model for comparing financial data between two periods.
/// </summary>
public class FinancialComparison
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OldIncome { get; set; }
    public decimal NewIncome { get; set; }
    public decimal IncomeChange { get; set; }
    public decimal OldExpenses { get; set; }
    public decimal NewExpenses { get; set; }
    public decimal ExpensesChange { get; set; }
    public decimal OldNetAmount { get; set; }
    public decimal NewNetAmount { get; set; }
    public decimal NetAmountChange { get; set; }
    
    public decimal IncomeChangePercentage => OldIncome > 0 ? (IncomeChange / OldIncome) * 100 : 0;
    public decimal ExpensesChangePercentage => OldExpenses > 0 ? (ExpensesChange / OldExpenses) * 100 : 0;
}
