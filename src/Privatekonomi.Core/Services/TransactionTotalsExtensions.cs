using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public static class TransactionTotalsExtensions
{
    public static IQueryable<Transaction> WhereIncludedInTotals(this IQueryable<Transaction> transactions)
    {
        return transactions.Where(t =>
            !t.IsInternalTransfer &&
            !t.IsExcludedFromTotals &&
            !t.TransactionCategories.Any(tc => tc.Category != null && tc.Category.IsInternalTransferCategory));
    }

    public static IEnumerable<Transaction> WhereIncludedInTotals(this IEnumerable<Transaction> transactions)
    {
        return transactions.Where(t => !t.IsExcludedFromTotalsTransaction);
    }
}