using System.Text;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1305 // Specify IFormatProvider

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for exporting transactions in SIE format (Standard Import Export)
/// SIE is a Swedish standard for exchanging accounting data
/// </summary>
public interface ISieExporter
{
    /// <summary>
    /// Export transactions to SIE format 4 (most common format)
    /// </summary>
    Task<string> ExportToSie4Async(int householdId, int year);
    
    /// <summary>
    /// Export transactions to SIE format 4 with custom date range
    /// </summary>
    Task<string> ExportToSie4Async(int householdId, DateTime fromDate, DateTime toDate);
}

public class SieExporter : ISieExporter
{
    private readonly PrivatekonomyContext _context;
    
    public SieExporter(PrivatekonomyContext context)
    {
        _context = context;
    }
    
    public async Task<string> ExportToSie4Async(int householdId, int year)
    {
        var fromDate = new DateTime(year, 1, 1);
        var toDate = new DateTime(year, 12, 31);
        return await ExportToSie4Async(householdId, fromDate, toDate);
    }
    
    public async Task<string> ExportToSie4Async(int householdId, DateTime fromDate, DateTime toDate)
    {
        // Fetch transactions with their categories
        var transactions = await _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
                .ThenInclude(tc => tc.Category)
            .Where(t => t.Date >= fromDate && t.Date <= toDate)
            .OrderBy(t => t.Date)
            .ToListAsync();
        
        // Fetch categories for account mapping
        var categories = await _context.Categories.ToListAsync();
        
        var sie = new StringBuilder();
        
        // Header section
        sie.AppendLine("#FLAGGA 0");
        sie.AppendLine("#PROGRAM \"Privatekonomi\" \"1.0\"");
        sie.AppendLine("#FORMAT PC8");
        sie.AppendLine($"#GEN {DateTime.Now:yyyyMMdd}");
        sie.AppendLine("#SIETYP 4");
        sie.AppendLine($"#FNR \"{householdId}\"");
        sie.AppendLine($"#ORGNR \"\"");
        sie.AppendLine($"#FNAMN \"Privatekonomi Hushåll {householdId}\"");
        sie.AppendLine();
        
        // Fiscal year (räkenskapsår)
        var year = fromDate.Year;
        sie.AppendLine($"#RAR 0 {fromDate:yyyyMMdd} {toDate:yyyyMMdd}");
        sie.AppendLine();
        
        // Chart of accounts (kontoplan)
        sie.AppendLine("# Kontoplan");
        sie.AppendLine("#KONTO 1930 \"Bank\"");
        sie.AppendLine("#KONTO 1940 \"Plusgiro\"");
        sie.AppendLine("#KONTO 1950 \"Bankgiro\"");
        
        // Income accounts (3000-3999)
        // Income categories are typically: "Lön", "Sparande", etc.
        var incomeCategories = categories.Where(c => 
            c.Name.Contains("Lön") || 
            c.Name.Contains("Inkomst") || 
            c.Name.Contains("Income")).ToList();
        var accountNumber = 3000;
        var categoryAccountMap = new Dictionary<int, int>();
        
        foreach (var category in incomeCategories)
        {
            sie.AppendLine($"#KONTO {accountNumber} \"{category.Name}\"");
            categoryAccountMap[category.CategoryId] = accountNumber;
            accountNumber += 10;
        }
        
        // Expense accounts (5000-7999)
        var expenseCategories = categories.Except(incomeCategories).ToList();
        accountNumber = 5000;
        
        foreach (var category in expenseCategories)
        {
            sie.AppendLine($"#KONTO {accountNumber} \"{category.Name}\"");
            categoryAccountMap[category.CategoryId] = accountNumber;
            accountNumber += 10;
        }
        
        sie.AppendLine();
        
        // Opening balances
        sie.AppendLine("# Ingående balanser");
        sie.AppendLine($"#IB 0 1930 0.00");
        sie.AppendLine();
        
        // Transactions (verifikationer)
        sie.AppendLine("# Verifikationer");
        var voucherNumber = 1;
        
        foreach (var transaction in transactions)
        {
            var date = transaction.Date.ToString("yyyyMMdd");
            var description = EscapeString(transaction.Description);
            
            sie.AppendLine($"#VER \"A\" \"{voucherNumber}\" {date} \"{description}\"");
            sie.AppendLine("{");
            
            // Bank account entry
            var bankAmount = transaction.Amount;
            sie.AppendLine($"   #TRANS 1930 {{}} {FormatAmount(bankAmount)}");
            
            // Category entries
            if (transaction.TransactionCategories.Any())
            {
                foreach (var tc in transaction.TransactionCategories)
                {
                    if (tc.Category != null && categoryAccountMap.TryGetValue(tc.Category.CategoryId, out var categoryAccount))
                    {
                        var categoryAmount = -bankAmount * (tc.Percentage / 100.0m);
                        sie.AppendLine($"   #TRANS {categoryAccount} {{}} {FormatAmount(categoryAmount)}");
                    }
                }
            }
            else
            {
                // No category - use default account
                var defaultAccount = transaction.IsIncome ? 3000 : 5000;
                sie.AppendLine($"   #TRANS {defaultAccount} {{}} {FormatAmount(-bankAmount)}");
            }
            
            sie.AppendLine("}");
            sie.AppendLine();
            
            voucherNumber++;
        }
        
        return sie.ToString();
    }
    
    private string FormatAmount(decimal amount)
    {
        // SIE uses dot as decimal separator and no thousand separators
        return amount.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
    }
    
    private string EscapeString(string input)
    {
        // Escape quotes and remove newlines
        return input.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }
}
