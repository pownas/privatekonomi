using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Privatekonomi.Core.Services;

public class ExportService : IExportService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;

    private static readonly JsonSerializerOptions JsonIndentedCamelCaseOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions JsonIndentedCamelCaseIgnoreCyclesOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public ExportService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<byte[]> ExportTransactionsToCsvAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.Date <= toDate.Value);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Datum,Beskrivning,Belopp,Typ,Bank,Kategorier,Taggar,Noteringar,Källa,Valuta");

        // Data rows
        foreach (var transaction in transactions)
        {
            var categories = string.Join("; ", transaction.TransactionCategories.Select(tc => tc.Category.Name));
            var bankName = transaction.BankSource?.Name ?? "";
            var type = transaction.IsIncome ? "Inkomst" : "Utgift";
            
            csv.AppendLine(CultureInfo.InvariantCulture, $"{transaction.Date:yyyy-MM-dd}," +
                          $"\"{EscapeCsv(transaction.Description)}\"," +
                          $"{transaction.Amount:F2}," +
                          $"{type}," +
                          $"\"{EscapeCsv(bankName)}\"," +
                          $"\"{EscapeCsv(categories)}\"," +
                          $"\"{EscapeCsv(transaction.Tags ?? "")}\"," +
                          $"\"{EscapeCsv(transaction.Notes ?? "")}\"," +
                          $"\"{EscapeCsv(transaction.ImportSource ?? "")}\"," +
                          $"{transaction.Currency}");
        }

        // Use UTF-8 with BOM for proper Excel compatibility with Swedish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<byte[]> ExportTransactionsToJsonAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.Date <= toDate.Value);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();

        var exportData = transactions.Select(t => new
        {
            t.TransactionId,
            t.Date,
            t.Description,
            t.Amount,
            Type = t.IsIncome ? "Inkomst" : "Utgift",
            Bank = t.BankSource?.Name,
            Categories = t.TransactionCategories.Select(tc => new
            {
                tc.Category.Name,
                tc.Category.Color,
                tc.Amount,
                tc.Percentage
            }).ToList(),
            t.Tags,
            t.Notes,
            t.Currency,
            t.Payee,
            t.ImportSource,
            t.Cleared,
            t.CreatedAt,
            t.UpdatedAt
        });

        var json = JsonSerializer.Serialize(exportData, JsonIndentedCamelCaseOptions);
        
        // Use UTF-8 with BOM for proper character encoding
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(json);
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<byte[]> ExportBudgetToCsvAsync(int budgetId)
    {
        var budget = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.BudgetId == budgetId);

        if (budget == null)
        {
            throw new ArgumentException($"Budget with ID {budgetId} not found");
        }

        var csv = new StringBuilder();
        
        // Budget header info
        csv.AppendLine(CultureInfo.InvariantCulture, $"Budget: {budget.Name}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Period: {budget.StartDate:yyyy-MM-dd} - {budget.EndDate:yyyy-MM-dd}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Typ: {budget.Period}");
        csv.AppendLine();
        
        // Category data header
        csv.AppendLine("Kategori,Planerat Belopp");

        // Budget categories
        foreach (var bc in budget.BudgetCategories)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"\"{EscapeCsv(bc.Category.Name)}\",{bc.PlannedAmount:F2}");
        }

        // Use UTF-8 with BOM for proper Excel compatibility with Swedish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<byte[]> ExportFullBackupAsync()
    {
        var backup = new
        {
            ExportDate = DateTime.UtcNow,
            Version = "1.0",
            Data = new
            {
                Transactions = await _context.Transactions
                    .Include(t => t.BankSource)
                    .Include(t => t.TransactionCategories)
                    .ThenInclude(tc => tc.Category)
                    .ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                Budgets = await _context.Budgets
                    .Include(b => b.BudgetCategories)
                    .ThenInclude(bc => bc.Category)
                    .ToListAsync(),
                Goals = await _context.Goals.ToListAsync(),
                Investments = await _context.Investments.ToListAsync(),
                Loans = await _context.Loans.ToListAsync(),
                BankSources = await _context.BankSources.ToListAsync(),
                BankConnections = await _context.BankConnections.ToListAsync(),
                Assets = await _context.Assets.ToListAsync(),
                Households = await _context.Households
                    .Include(h => h.Members)
                    .ToListAsync()
            }
        };

        var json = JsonSerializer.Serialize(backup, JsonIndentedCamelCaseIgnoreCyclesOptions);
        
        // Use UTF-8 with BOM for proper character encoding
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(json);
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<List<int>> GetAvailableYearsAsync()
    {
        var query = _context.Transactions.AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        var years = await query
            .Select(t => t.Date.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        return years;
    }

    public async Task<byte[]> ExportYearDataToJsonAsync(int year)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);

        var userId = _currentUserService?.UserId;

        // Build queries for all data types for the year
        var transactionsQuery = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        var budgetsQuery = _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .Where(b => (b.StartDate.Year == year || b.EndDate.Year == year));

        var goalsQuery = _context.Goals
            .Where(g => g.CreatedAt.Year == year || (g.TargetDate.HasValue && g.TargetDate.Value.Year == year));

        var investmentsQuery = _context.Investments.AsQueryable();
        
        var loansQuery = _context.Loans.AsQueryable();

        var salaryHistoryQuery = _context.SalaryHistories
            .Where(s => s.Period.Year == year);

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && userId != null)
        {
            transactionsQuery = transactionsQuery.Where(t => t.UserId == userId);
            budgetsQuery = budgetsQuery.Where(b => b.UserId == userId);
            goalsQuery = goalsQuery.Where(g => g.UserId == userId);
            investmentsQuery = investmentsQuery.Where(i => i.UserId == userId);
            loansQuery = loansQuery.Where(l => l.UserId == userId);
            salaryHistoryQuery = salaryHistoryQuery.Where(s => s.UserId == userId);
        }

        var yearData = new
        {
            Year = year,
            ExportDate = DateTime.UtcNow,
            Version = "1.0",
            Data = new
            {
                Transactions = await transactionsQuery.OrderBy(t => t.Date).ToListAsync(),
                Budgets = await budgetsQuery.ToListAsync(),
                Goals = await goalsQuery.ToListAsync(),
                Investments = await investmentsQuery.ToListAsync(),
                Loans = await loansQuery.ToListAsync(),
                SalaryHistory = await salaryHistoryQuery.ToListAsync()
            }
        };

        var json = JsonSerializer.Serialize(yearData, JsonIndentedCamelCaseIgnoreCyclesOptions);
        
        // Use UTF-8 with BOM for proper character encoding
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(json);
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<byte[]> ExportYearDataToCsvAsync(int year)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);

        var query = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();

        var csv = new StringBuilder();
        
        // Header with year information
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Privatekonomi Export - År {year}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Exportdatum: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Antal transaktioner: {transactions.Count}");
        csv.AppendLine();
        
        // Column headers
        csv.AppendLine("Datum,Beskrivning,Belopp,Typ,Bank,Kategorier,Taggar,Noteringar,Källa,Valuta");

        // Data rows
        foreach (var transaction in transactions)
        {
            var categories = string.Join("; ", transaction.TransactionCategories.Select(tc => tc.Category.Name));
            var bankName = transaction.BankSource?.Name ?? "";
            var type = transaction.IsIncome ? "Inkomst" : "Utgift";
            
            csv.AppendLine(CultureInfo.InvariantCulture, $"{transaction.Date:yyyy-MM-dd}," +
                          $"\"{EscapeCsv(transaction.Description)}\"," +
                          $"{transaction.Amount:F2}," +
                          $"{type}," +
                          $"\"{EscapeCsv(bankName)}\"," +
                          $"\"{EscapeCsv(categories)}\"," +
                          $"\"{EscapeCsv(transaction.Tags ?? "")}\"," +
                          $"\"{EscapeCsv(transaction.Notes ?? "")}\"," +
                          $"\"{EscapeCsv(transaction.ImportSource ?? "")}\"," +
                          $"{transaction.Currency}");
        }

        // Summary section
        csv.AppendLine();
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Summering {year}");
        var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var totalExpenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var netResult = totalIncome - totalExpenses;
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Totala inkomster: {totalIncome:F2} SEK");
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Totala utgifter: {totalExpenses:F2} SEK");
        csv.AppendLine(CultureInfo.InvariantCulture, $"# Nettoresultat: {netResult:F2} SEK");

        // Use UTF-8 with BOM for proper Excel compatibility with Swedish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<byte[]> ExportSelectedTransactionsToCsvAsync(List<int> transactionIds)
    {
        var query = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Where(t => transactionIds.Contains(t.TransactionId))
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Datum,Beskrivning,Belopp,Typ,Bank,Kategorier,Hushåll,Taggar,Noteringar,Källa,Valuta");

        // Data rows
        foreach (var transaction in transactions)
        {
            var categories = string.Join("; ", transaction.TransactionCategories.Select(tc => tc.Category.Name));
            var bankName = transaction.BankSource?.Name ?? "";
            var householdName = transaction.Household?.Name ?? "";
            var type = transaction.IsIncome ? "Inkomst" : "Utgift";
            
            csv.AppendLine(CultureInfo.InvariantCulture, $"{transaction.Date:yyyy-MM-dd}," +
                          $"\"{EscapeCsv(transaction.Description)}\"," +
                          $"{transaction.Amount:F2}," +
                          $"{type}," +
                          $"\"{EscapeCsv(bankName)}\"," +
                          $"\"{EscapeCsv(categories)}\"," +
                          $"\"{EscapeCsv(householdName)}\"," +
                          $"\"{EscapeCsv(transaction.Tags ?? "")}\"," +
                          $"\"{EscapeCsv(transaction.Notes ?? "")}\"," +
                          $"\"{EscapeCsv(transaction.ImportSource ?? "")}\"," +
                          $"{transaction.Currency}");
        }

        // Use UTF-8 with BOM for proper Excel compatibility with Swedish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<byte[]> ExportSelectedTransactionsToJsonAsync(List<int> transactionIds)
    {
        var query = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.Household)
            .Include(t => t.Receipts)
            .Where(t => transactionIds.Contains(t.TransactionId))
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();

        var exportData = new
        {
            ExportDate = DateTime.UtcNow,
            TransactionCount = transactions.Count,
            Transactions = transactions.Select(t => new
            {
                t.TransactionId,
                t.Date,
                t.Description,
                t.Amount,
                t.IsIncome,
                t.Currency,
                BankSource = t.BankSource?.Name,
                Household = t.Household?.Name,
                Categories = t.TransactionCategories.Select(tc => new
                {
                    tc.Category.Name,
                    tc.Category.Color,
                    tc.Amount
                }),
                t.Payee,
                t.Tags,
                t.Notes,
                t.PaymentMethod,
                t.OCR,
                t.IsRecurring,
                t.Cleared,
                t.ImportSource,
                ReceiptCount = t.Receipts.Count
            })
        };

        var json = JsonSerializer.Serialize(exportData, JsonIndentedCamelCaseOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Escape quotes by doubling them
        return value.Replace("\"", "\"\"");
    }
}
