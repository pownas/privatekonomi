using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Base class for bank API services with common functionality
/// </summary>
public abstract class BankApiServiceBase : IBankApiService
{
    private readonly PrivatekonomyContext _contextField;
    private readonly HttpClient _httpClientField;

    protected PrivatekonomyContext Context => _contextField;
    protected HttpClient HttpClient => _httpClientField;

    protected BankApiServiceBase(PrivatekonomyContext context, HttpClient httpClient)
    {
        _contextField = context;
        _httpClientField = httpClient;
    }

    public abstract string BankName { get; }
    
    public abstract Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);
    
    public abstract Task<BankConnection> ExchangeCodeForTokenAsync(string code, string redirectUri);
    
    public abstract Task<BankConnection> RefreshTokenAsync(BankConnection connection);
    
    public abstract Task<List<BankApiAccount>> GetAccountsAsync(BankConnection connection);
    
    public abstract Task<List<BankApiTransaction>> GetTransactionsAsync(
        BankConnection connection, 
        string accountId, 
        DateTime fromDate, 
        DateTime toDate);

    /// <summary>
    /// Common implementation for importing transactions from API
    /// </summary>
    public virtual async Task<BankApiImportResult> ImportTransactionsAsync(
        BankConnection connection,
        string accountId,
        DateTime fromDate,
        DateTime toDate,
        bool skipDuplicates = true)
    {
        var result = new BankApiImportResult { Success = false };

        try
        {
            // Fetch transactions from bank API
            var apiTransactions = await GetTransactionsAsync(connection, accountId, fromDate, toDate);
            
            if (apiTransactions == null || !apiTransactions.Any())
            {
                result.Success = true;
                return result;
            }

            // Convert to internal Transaction model
            var transactions = apiTransactions.Select(MapToTransaction).ToList();

            // Find duplicates if needed
            if (skipDuplicates)
            {
                var duplicates = await FindDuplicatesAsync(transactions);
                result.DuplicateCount = duplicates.Count;
                transactions = transactions.Where(t => !duplicates.Contains(t)).ToList();
            }

            // Import non-duplicate transactions
            foreach (var transaction in transactions)
            {
                transaction.BankSourceId = connection.BankSourceId;
                Context.Transactions.Add(transaction);
            }

            await Context.SaveChangesAsync();
            
            // Update last synced timestamp
            connection.LastSyncedAt = DateTime.UtcNow;
            connection.UpdatedAt = DateTime.UtcNow;
            Context.BankConnections.Update(connection);
            await Context.SaveChangesAsync();

            result.Success = true;
            result.ImportedCount = transactions.Count;
            result.Transactions = transactions;
            result.LastTransactionDate = transactions.Any() ? transactions.Max(t => t.Date) : null;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Error importing transactions: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Maps a BankApiTransaction to internal Transaction model
    /// </summary>
    protected virtual Transaction MapToTransaction(BankApiTransaction apiTrans)
    {
        return new Transaction
        {
            Date = apiTrans.BookingDate ?? apiTrans.Date,
            Amount = Math.Abs(apiTrans.Amount),
            IsIncome = apiTrans.IsIncome,
            Description = BuildDescription(apiTrans),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Builds transaction description from API transaction data
    /// </summary>
    protected virtual string BuildDescription(BankApiTransaction apiTrans)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(apiTrans.Creditor))
            parts.Add(apiTrans.Creditor);
        else if (!string.IsNullOrWhiteSpace(apiTrans.Debtor))
            parts.Add(apiTrans.Debtor);
            
        if (!string.IsNullOrWhiteSpace(apiTrans.Description))
            parts.Add(apiTrans.Description);
            
        if (!string.IsNullOrWhiteSpace(apiTrans.Reference))
            parts.Add(apiTrans.Reference);

        var description = string.Join(" - ", parts);
        return description.Length > 500 ? description.Substring(0, 500) : description;
    }

    /// <summary>
    /// Finds duplicate transactions in the database
    /// </summary>
    protected virtual async Task<List<Transaction>> FindDuplicatesAsync(List<Transaction> transactions)
    {
        var duplicates = new List<Transaction>();
        var existingTransactions = await Context.Transactions.ToListAsync();

        foreach (var transaction in transactions)
        {
            var isDuplicate = existingTransactions.Any(existing => IsDuplicate(transaction, existing));
            if (isDuplicate)
            {
                duplicates.Add(transaction);
            }
        }

        return duplicates;
    }

    /// <summary>
    /// Checks if two transactions are duplicates
    /// </summary>
    protected virtual bool IsDuplicate(Transaction t1, Transaction t2)
    {
        return t1.Date.Date == t2.Date.Date &&
               t1.Amount == t2.Amount &&
               t1.IsIncome == t2.IsIncome &&
               string.Equals(t1.Description, t2.Description, StringComparison.OrdinalIgnoreCase);
    }
}
