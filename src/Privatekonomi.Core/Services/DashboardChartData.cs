using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>Prepares category allocations for dashboard charts.</summary>
public static class DashboardChartData
{
    /// <summary>Returns each category's allocated amount, including uncategorized transactions.</summary>
    public static IEnumerable<(DateTime Date, bool IsIncome, string Category, decimal Amount)> GetCategoryAmounts(
        IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            if (transaction.IsExcludedFromTotalsTransaction)
            {
                continue;
            }

            if (transaction.TransactionCategories.Count == 0)
            {
                yield return (transaction.Date, transaction.IsIncome, "Okategoriserad", Math.Abs(transaction.Amount));
                continue;
            }

            foreach (var category in transaction.TransactionCategories)
            {
                yield return (transaction.Date, transaction.IsIncome, category.Category.Name, Math.Abs(category.Amount));
            }
        }
    }
}