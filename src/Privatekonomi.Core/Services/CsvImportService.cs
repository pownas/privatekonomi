using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.Parsers;
using System.Text;
using System.Text.Json;

namespace Privatekonomi.Core.Services;

public class CsvImportService : ICsvImportService
{
    private readonly PrivatekonomyContext _context;
    private readonly ILogger<CsvImportService> _logger;
    private readonly List<ICsvParser> _parsers;

    public CsvImportService(PrivatekonomyContext context, ILogger<CsvImportService> logger)
    {
        _context = context;
        _logger = logger;
        _parsers = new List<ICsvParser>
        {
            new SwedbankParser(),
            new IcaBankenParser(),
            new OfxParser()
        };
    }

    /// <summary>
    /// Tries to detect which bank/parser can handle the given file content.
    /// Returns the bank name (e.g. "Swedbank") or null if no parser matches.
    /// </summary>
    public string? DetectBank(byte[] fileBytes)
    {
        // Try UTF-8 first, fall back to Windows-1252 for Swedish characters
        string content;
        try
        {
            content = Encoding.UTF8.GetString(fileBytes);
        }
        catch
        {
            content = string.Empty;
        }

        if (content.Contains('\uFFFD'))
        {
            try { content = Encoding.GetEncoding("Windows-1252").GetString(fileBytes); }
            catch { /* keep utf-8 attempt */ }
        }

        foreach (var parser in _parsers)
        {
            if (parser.CanParse(content))
            {
                _logger.LogInformation("Auto-detected bank '{BankName}' from file content", parser.BankName);
                return parser.BankName;
            }
        }

        _logger.LogWarning("Could not auto-detect bank from file content ({Bytes} bytes)", fileBytes.Length);
        return null;
    }

    public async Task<CsvImportResult> PreviewCsvAsync(Stream csvStream, string bankName, string? userId = null)
    {
        var result = new CsvImportResult { Success = false };

        try
        {
            _logger.LogInformation("Starting CSV preview for bank '{BankName}', userId='{UserId}'", bankName, userId ?? "(none)");

            // Find the appropriate parser
            var parser = GetParser(bankName);
            if (parser == null)
            {
                _logger.LogWarning("No parser found for bank '{BankName}'", bankName);
                result.Errors.Add(new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "InvalidBank",
                    ErrorMessage = $"Bank '{bankName}' stöds inte. Tillgängliga banker: {string.Join(", ", _parsers.Select(p => p.BankName))}."
                });
                return result;
            }

            // Parse transactions
            var parseResult = await parser.ParseAsync(csvStream);
            result.TotalRows = parseResult.Transactions.Count;
            result.Warnings.AddRange(parseResult.Warnings);

            _logger.LogInformation("Parser '{BankName}' returned {Count} transactions and {Warnings} warnings",
                bankName, parseResult.Transactions.Count, parseResult.Warnings.Count);

            // Validate transactions
            var validTransactions = new List<Transaction>();
            var rowNumber = 1;

            foreach (var transaction in parseResult.Transactions)
            {
                var (validationErrors, validationWarnings) = ValidateTransaction(transaction, rowNumber);

                if (validationErrors.Any())
                {
                    result.Errors.AddRange(validationErrors);
                    result.ErrorCount++;
                }
                else
                {
                    validTransactions.Add(transaction);
                }

                result.Warnings.AddRange(validationWarnings);
                rowNumber++;
            }

            // Check for duplicates — scoped to the importing user when userId is available
            var duplicates = await FindDuplicatesAsync(validTransactions, userId);
            result.DuplicateCount = duplicates.Count;

            _logger.LogInformation("Duplicate check found {Duplicates} duplicates for user '{UserId}'",
                duplicates.Count, userId ?? "(none)");

            // Remove duplicates from valid transactions for preview
            validTransactions = validTransactions
                .Where(t => !duplicates.Any(d => IsDuplicate(t, d)))
                .ToList();

            result.Transactions = validTransactions;
            result.ImportedCount = validTransactions.Count;
            result.Summary = CalculateSummary(validTransactions);
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV preview for bank '{BankName}'", bankName);
            result.Errors.Add(new CsvImportError
            {
                RowNumber = 0,
                ErrorType = "ParseError",
                ErrorMessage = $"Fel vid parsning av fil: {ex.Message}"
            });
        }

        return result;
    }

    public async Task<CsvImportResult> ImportCsvAsync(Stream csvStream, string bankName, bool skipDuplicates = true)
    {
        var result = await PreviewCsvAsync(csvStream, bankName);
        
        if (!result.Success || result.Transactions.Count == 0)
        {
            return result;
        }

        try
        {
            // Determine file type based on bank/parser
            var fileType = bankName.Contains("OFX", StringComparison.OrdinalIgnoreCase) ? "OFX" : "CSV";
            var importSource = $"{bankName} {fileType} (manuell)";

            // Group transactions by account (clearing + account number) so each gets the correct BankSource
            var transactionsByAccount = result.Transactions
                .GroupBy(t => (ClearingNumber: t.ClearingNumber, AccountNumber: t.AccountNumber))
                .ToList();

            foreach (var accountGroup in transactionsByAccount)
            {
                var bankSource = await FindOrCreateBankSourceAsync(
                    bankName,
                    accountGroup.Key.ClearingNumber,
                    accountGroup.Key.AccountNumber,
                    userId: null);

                foreach (var transaction in accountGroup)
                {
                    transaction.BankSourceId = bankSource?.BankSourceId;
                    transaction.Imported = true;
                    transaction.ImportSource = importSource;
                    transaction.CreatedAt = DateTime.UtcNow;
                    transaction.ValidFrom = DateTime.UtcNow;
                    _context.Transactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(new CsvImportError
            {
                RowNumber = 0,
                ErrorType = "DatabaseError",
                ErrorMessage = $"Fel vid sparande till databas: {ex.Message}"
            });
        }

        return result;
    }
    
    /// <summary>
    /// Import transactions and create an ImportJob record for tracking.
    /// </summary>
    public async Task<(CsvImportResult Result, ImportJob Job)> ImportWithJobAsync(
        Stream stream, 
        string bankName, 
        string fileName, 
        long fileSize, 
        string? userId = null,
        bool skipDuplicates = true)
    {
        // Create import job
        var importJob = new ImportJob
        {
            BankName = bankName,
            FileType = bankName.Contains("OFX", StringComparison.OrdinalIgnoreCase) ? "OFX" : "CSV",
            FileName = fileName,
            FileSize = fileSize,
            Source = "manual",
            UserId = userId,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        
        _context.ImportJobs.Add(importJob);
        await _context.SaveChangesAsync();
        
        try
        {
            var result = await PreviewCsvAsync(stream, bankName, userId);
            
            importJob.TotalRows = result.TotalRows;
            importJob.DuplicateCount = result.DuplicateCount;
            importJob.ErrorCount = result.ErrorCount;
            
            if (!result.Success || result.Transactions.Count == 0)
            {
                importJob.Status = result.Transactions.Count == 0 && result.Success ? "Completed" : "Failed";
                importJob.ErrorMessages = result.Errors.Any() 
                    ? JsonSerializer.Serialize(result.Errors) 
                    : null;
                importJob.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (result, importJob);
            }

            // Get BankSource by name or by account info (clearing + account number)
            var importSource = $"{bankName} {importJob.FileType} (manuell)";

            // Group transactions by account (clearing + account number) so each gets the correct BankSource
            var transactionsByAccount = result.Transactions
                .GroupBy(t => (ClearingNumber: t.ClearingNumber, AccountNumber: t.AccountNumber))
                .ToList();

            foreach (var accountGroup in transactionsByAccount)
            {
                var bankSource = await FindOrCreateBankSourceAsync(
                    bankName,
                    accountGroup.Key.ClearingNumber,
                    accountGroup.Key.AccountNumber,
                    userId);

                foreach (var transaction in accountGroup)
                {
                    transaction.BankSourceId = bankSource?.BankSourceId;
                    transaction.Imported = true;
                    transaction.ImportSource = importSource;
                    transaction.UserId = userId;
                    transaction.CreatedAt = DateTime.UtcNow;
                    transaction.ValidFrom = DateTime.UtcNow;
                    _context.Transactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync();
            
            importJob.ImportedCount = result.Transactions.Count;
            importJob.Status = "Completed";
            importJob.CompletedAt = DateTime.UtcNow;
            
            if (result.Errors.Any())
            {
                importJob.ErrorMessages = JsonSerializer.Serialize(result.Errors);
            }
            
            await _context.SaveChangesAsync();
            
            result.Success = true;
            return (result, importJob);
        }
        catch (Exception ex)
        {
            importJob.Status = "Failed";
            importJob.CompletedAt = DateTime.UtcNow;
            importJob.ErrorMessages = JsonSerializer.Serialize(new[]
            {
                new CsvImportError
                {
                    RowNumber = 0,
                    ErrorType = "ImportError",
                    ErrorMessage = ex.Message
                }
            });
            
            await _context.SaveChangesAsync();
            
            var failedResult = new CsvImportResult
            {
                Success = false,
                Errors = { new CsvImportError { RowNumber = 0, ErrorType = "ImportError", ErrorMessage = ex.Message } }
            };
            
            return (failedResult, importJob);
        }
    }
    
    /// <summary>
    /// Get an import job by ID.
    /// </summary>
    public async Task<ImportJob?> GetImportJobAsync(int importJobId)
    {
        return await _context.ImportJobs.FindAsync(importJobId);
    }
    
    /// <summary>
    /// Get all import jobs for a user.
    /// </summary>
    public async Task<List<ImportJob>> GetUserImportJobsAsync(string userId)
    {
        return await _context.ImportJobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    private ICsvParser? GetParser(string bankName)
    {
        // First try exact match
        var parser = _parsers.FirstOrDefault(p => 
            p.BankName.Equals(bankName, StringComparison.OrdinalIgnoreCase));
        
        // If not found and it's OFX, use the OFX parser
        if (parser == null && bankName.Contains("OFX", StringComparison.OrdinalIgnoreCase))
        {
            parser = _parsers.FirstOrDefault(p => p is OfxParser);
        }
        
        return parser;
    }

    private (List<CsvImportError> Errors, List<ParseWarning> Warnings) ValidateTransaction(Transaction transaction, int rowNumber)
    {
        var errors = new List<CsvImportError>();
        var warnings = new List<ParseWarning>();

        // Validate date
        if (transaction.Date > DateTime.Now.AddDays(7))
        {
            errors.Add(new CsvImportError
            {
                RowNumber = rowNumber,
                ErrorType = "InvalidDate",
                ErrorMessage = $"Datumet {transaction.Date:yyyy-MM-dd} får inte vara senare än 7 dagar framåt i tiden."
            });
        }

        if (transaction.Date < DateTime.Now.AddYears(-10))
        {
            warnings.Add(new ParseWarning
            {
                RowNumber = rowNumber,
                WarningType = "OldDate",
                Message = $"Rad {rowNumber}: Datumet {transaction.Date:yyyy-MM-dd} är mer än 10 år gammalt – kontrollera att det stämmer.",
                RawData = transaction.Description
            });
        }

        // Validate amount
        if (transaction.Amount == 0)
        {
            warnings.Add(new ParseWarning
            {
                RowNumber = rowNumber,
                WarningType = "ZeroAmount",
                Message = $"Rad {rowNumber}: Beloppet är noll – raden importeras men kan vara felaktig.",
                RawData = transaction.Description
            });
        }

        if (transaction.Amount > 10_000_000)
        {
            errors.Add(new CsvImportError
            {
                RowNumber = rowNumber,
                ErrorType = "InvalidAmount",
                ErrorMessage = $"Beloppet {transaction.Amount:N2} kr på rad {rowNumber} överstiger gränsen på 10 miljoner kr."
            });
        }

        // Validate description
        if (string.IsNullOrWhiteSpace(transaction.Description))
        {
            errors.Add(new CsvImportError
            {
                RowNumber = rowNumber,
                ErrorType = "MissingDescription",
                ErrorMessage = $"Beskrivning saknas på rad {rowNumber}."
            });
        }

        return (errors, warnings);
    }

    private async Task<List<Transaction>> FindDuplicatesAsync(List<Transaction> transactions, string? userId)
    {
        var duplicates = new List<Transaction>();

        // Build a query window: only look at existing transactions within the date range of the import batch
        // to avoid scanning the entire transactions table.
        if (transactions.Count == 0)
            return duplicates;

        var minDate = transactions.Min(t => t.Date.Date);
        var maxDate = transactions.Max(t => t.Date.Date);

        var query = _context.Transactions
            .Where(t => t.Date.Date >= minDate && t.Date.Date <= maxDate);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            // Always scope to the importing user when available to prevent false-positive
            // duplicate matches against another user's transactions.
            query = query.Where(t => t.UserId == userId);
        }
        else
        {
            // No userId available (e.g. anonymous/legacy call path) — log a warning so
            // developers know duplicate detection is not user-scoped in this case.
            _logger.LogWarning(
                "FindDuplicatesAsync called without userId; duplicate detection is not scoped to a specific user.");
        }

        var existingTransactions = await query.ToListAsync();

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

    private bool IsDuplicate(Transaction t1, Transaction t2)
    {
        return t1.Date.Date == t2.Date.Date &&
               t1.Amount == t2.Amount &&
               t1.IsIncome == t2.IsIncome &&
               string.Equals(t1.Description, t2.Description, StringComparison.OrdinalIgnoreCase);
    }

    private ImportSummary CalculateSummary(List<Transaction> transactions)
    {
        var summary = new ImportSummary();

        foreach (var transaction in transactions)
        {
            if (transaction.IsIncome)
            {
                summary.IncomeAmount += transaction.Amount;
                summary.IncomeCount++;
            }
            else
            {
                summary.ExpenseAmount += transaction.Amount;
                summary.ExpenseCount++;
            }
        }

        summary.TotalAmount = summary.IncomeAmount - summary.ExpenseAmount;

        return summary;
    }

    /// <summary>
    /// Finds an existing BankSource matching the given bank name, clearing number and account number,
    /// or creates a new one if no match is found. This enables automatic linking of transactions
    /// to individual bank accounts when clearing/account numbers are present in the import file.
    /// </summary>
    private async Task<BankSource?> FindOrCreateBankSourceAsync(
        string bankName,
        string? clearingNumber,
        string? accountNumber,
        string? userId)
    {
        // Build query to find matching BankSource
        var query = _context.BankSources
            .Where(b => b.Name.Equals(bankName, StringComparison.OrdinalIgnoreCase) ||
                        (b.Institution != null && b.Institution.Equals(bankName, StringComparison.OrdinalIgnoreCase)));

        if (userId != null)
        {
            query = query.Where(b => b.UserId == userId);
        }

        // If we have account number info, try to find by clearing + account
        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            var byAccount = await query
                .Where(b => b.AccountNumber == accountNumber &&
                            (clearingNumber == null || b.ClearingNumber == clearingNumber))
                .FirstOrDefaultAsync();

            if (byAccount != null)
                return byAccount;

            // No matching account found - create a new BankSource for this account
            var displayName = !string.IsNullOrWhiteSpace(clearingNumber)
                ? $"{bankName} {clearingNumber}-{accountNumber}"
                : $"{bankName} {accountNumber}";

            _logger.LogInformation(
                "Auto-creating BankSource '{DisplayName}' for user '{UserId}'", displayName, userId ?? "(none)");

            var newBankSource = new BankSource
            {
                Name = displayName,
                Institution = bankName,
                ClearingNumber = clearingNumber,
                AccountNumber = accountNumber,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            };

            _context.BankSources.Add(newBankSource);
            await _context.SaveChangesAsync();
            return newBankSource;
        }

        // No account number in import data - fall back to matching by bank name only
        return await query.FirstOrDefaultAsync();
    }
}
