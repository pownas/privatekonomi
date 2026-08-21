using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.Parsers;

namespace Privatekonomi.Core.Services;

public class InvestmentService : IInvestmentService
{
    private readonly PrivatekonomyContext _context;
    private readonly List<IInvestmentCsvParser> _parsers;
    private readonly AvanzaTransactionParser _transactionParser;
    private readonly ICurrentUserService? _currentUserService;

    public InvestmentService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _parsers = new List<IInvestmentCsvParser>
        {
            new AvanzaHoldingsPerAccountParser(),
            new AvanzaConsolidatedHoldingsParser()
        };
        _transactionParser = new AvanzaTransactionParser();
    }

    public async Task<IEnumerable<Investment>> GetAllInvestmentsAsync()
    {
        var query = _context.Investments
            .Include(i => i.BankSource)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(i => i.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(i => i.LastUpdated).ToListAsync();
    }

    public async Task<Investment?> GetInvestmentByIdAsync(int id)
    {
        var query = _context.Investments
            .Include(i => i.BankSource)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(i => i.UserId == _currentUserService.UserId);
        }

        return await query.FirstOrDefaultAsync(i => i.InvestmentId == id);
    }

    public async Task<Investment> AddInvestmentAsync(Investment investment)
    {
        investment.LastUpdated = DateTime.Now;
        
        // Set user ID for new investments
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            investment.UserId = _currentUserService.UserId;
        }
        
        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();
        return investment;
    }

    public async Task<Investment> UpdateInvestmentAsync(Investment investment)
    {
        investment.LastUpdated = DateTime.Now;
        investment.UpdatedAt = DateTime.UtcNow;
        _context.Investments.Update(investment);
        await _context.SaveChangesAsync();
        return investment;
    }

    public async Task DeleteInvestmentAsync(int id)
    {
        var investment = await _context.Investments.FindAsync(id);
        if (investment != null)
        {
            _context.Investments.Remove(investment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<CsvImportResult> ImportFromCsvAsync(Stream csvStream, int bankSourceId)
    {
        var result = new CsvImportResult();
        Stream workingStream = csvStream;
        MemoryStream? bufferedStream = null;
        
        try
        {
            // Some upload streams (e.g. browser streams) are not seekable; buffer to allow multiple passes.
            if (!csvStream.CanSeek)
            {
                bufferedStream = new MemoryStream();
                await csvStream.CopyToAsync(bufferedStream);
                bufferedStream.Position = 0;
                workingStream = bufferedStream;
            }
            else
            {
                csvStream.Position = 0;
            }

            // Read CSV content to detect format
            using var reader = new StreamReader(workingStream, Encoding.UTF8, leaveOpen: true);
            var content = await reader.ReadToEndAsync();

            // Detect Avanza transaction history format first
            if (_transactionParser.CanParse(content))
            {
                return await ImportTransactionHistoryAsync(content, bankSourceId);
            }
            
            // Find appropriate holdings parser
            IInvestmentCsvParser? parser = null;
            foreach (var p in _parsers)
            {
                if (p.CanParse(content))
                {
                    parser = p;
                    break;
                }
            }
            
            if (parser == null)
            {
                result.Errors.Add(new CsvImportError 
                { 
                    ErrorType = "Format", 
                    ErrorMessage = "Kunde inte identifiera CSV-format. Kontrollera att filen kommer från Avanza." 
                });
                result.ErrorCount++;
                return result;
            }
            
            // Parse investments
            if (workingStream.CanSeek)
            {
                workingStream.Position = 0;
            }
            var parsedInvestments = await parser.ParseAsync(workingStream);
            
            result.TotalRows = parsedInvestments.Count;
            
            // Process each investment
            foreach (var parsedInvestment in parsedInvestments)
            {
                parsedInvestment.BankSourceId = bankSourceId;
                
                // Check for duplicates
                Investment? existing = null;
                
                if (!string.IsNullOrEmpty(parsedInvestment.ISIN) && !string.IsNullOrEmpty(parsedInvestment.AccountNumber))
                {
                    existing = await _context.Investments
                        .FirstOrDefaultAsync(i => i.ISIN == parsedInvestment.ISIN && 
                                                  i.AccountNumber == parsedInvestment.AccountNumber);
                }
                else if (!string.IsNullOrEmpty(parsedInvestment.ISIN))
                {
                    existing = await _context.Investments
                        .FirstOrDefaultAsync(i => i.ISIN == parsedInvestment.ISIN);
                }
                else
                {
                    existing = await _context.Investments
                        .FirstOrDefaultAsync(i => i.Name == parsedInvestment.Name && 
                                                  i.Type == parsedInvestment.Type &&
                                                  i.BankSourceId == bankSourceId);
                }
                
                if (existing != null)
                {
                    // Update existing investment
                    existing.Name = parsedInvestment.Name;
                    existing.ShortName = parsedInvestment.ShortName;
                    existing.Type = parsedInvestment.Type;
                    existing.Quantity = parsedInvestment.Quantity;
                    existing.PurchasePrice = parsedInvestment.PurchasePrice;
                    existing.CurrentPrice = parsedInvestment.CurrentPrice;
                    existing.AccountNumber = parsedInvestment.AccountNumber;
                    existing.ISIN = parsedInvestment.ISIN;
                    existing.Currency = parsedInvestment.Currency;
                    existing.Country = parsedInvestment.Country;
                    existing.Market = parsedInvestment.Market;
                    existing.BankSourceId = bankSourceId;
                    existing.LastUpdated = DateTime.Now;
                    
                    result.DuplicateCount++;
                }
                else
                {
                    // Add new investment
                    _context.Investments.Add(parsedInvestment);
                    result.ImportedCount++;
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
                ErrorType = "Import", 
                ErrorMessage = $"Ett fel uppstod vid import: {ex.Message}" 
            });
            result.ErrorCount++;
        }
        finally
        {
            bufferedStream?.Dispose();
        }
        
        return result;
    }

    public Task<string> ExportToCsvAsync(IEnumerable<Investment> investments)
    {
        var sb = new StringBuilder();
        var culture = new CultureInfo("sv-SE");
        
        // Header
        sb.AppendLine("Namn;Kortnamn;Typ;Bank;Konto;ISIN;Antal;Köpkurs;Aktuell kurs;Totalt värde;Kostnad;Vinst/Förlust;Avkastning %;Valuta;Land;Marknad");
        
        // Data rows
        foreach (var inv in investments)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{EscapeCsv(inv.Name)};{EscapeCsv(inv.ShortName ?? "")};{EscapeCsv(inv.Type)};" +
                         $"{EscapeCsv(inv.BankSource?.Name ?? "")};{EscapeCsv(inv.AccountNumber ?? "")};{EscapeCsv(inv.ISIN ?? "")};" +
                         $"{inv.Quantity.ToString("N4", culture)};{inv.PurchasePrice.ToString("N2", culture)};" +
                         $"{inv.CurrentPrice.ToString("N2", culture)};{inv.TotalValue.ToString("N2", culture)};" +
                         $"{inv.TotalCost.ToString("N2", culture)};{inv.ProfitLoss.ToString("N2", culture)};" +
                         $"{inv.ProfitLossPercentage.ToString("N2", culture)};{EscapeCsv(inv.Currency ?? "")};" +
                         $"{EscapeCsv(inv.Country ?? "")};{EscapeCsv(inv.Market ?? "")}");
        }
        
        return Task.FromResult(sb.ToString());
    }

    public async Task<IEnumerable<Investment>> GetInvestmentsByBankAsync(int bankSourceId)
    {
        return await _context.Investments
            .Include(i => i.BankSource)
            .Where(i => i.BankSourceId == bankSourceId)
            .OrderByDescending(i => i.LastUpdated)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>> GetInvestmentsByAccountAsync(string accountNumber)
    {
        return await _context.Investments
            .Include(i => i.BankSource)
            .Where(i => i.AccountNumber == accountNumber)
            .OrderByDescending(i => i.LastUpdated)
            .ToListAsync();
    }

    /// <summary>
    /// Imports Avanza transaction history rows as <see cref="InvestmentTransaction"/> records.
    /// For each unique security (identified by ISIN or name), a stub <see cref="Investment"/>
    /// is created if one does not already exist. Rows without a security (deposits, withdrawals)
    /// are skipped.
    /// </summary>
    private async Task<CsvImportResult> ImportTransactionHistoryAsync(string csvContent, int bankSourceId)
    {
        var result = new CsvImportResult();

        try
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
            var rows = await _transactionParser.ParseTransactionsAsync(stream);

            result.TotalRows = rows.Count;

            var userId = _currentUserService?.IsAuthenticated == true ? _currentUserService.UserId : null;

            // Cache of Investment lookups to avoid repeated DB queries within the same import
            var investmentCache = new Dictionary<string, Investment>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                // Only import rows that refer to a security
                if (string.IsNullOrWhiteSpace(row.ISIN) && string.IsNullOrWhiteSpace(row.SecurityName))
                    continue;

                try
                {
                    var cacheKey = row.ISIN ?? row.SecurityName!;
                    if (!investmentCache.TryGetValue(cacheKey, out var investment))
                    {
                        // Find or create Investment stub for this security
                        investment = string.IsNullOrWhiteSpace(row.ISIN)
                            ? await _context.Investments.FirstOrDefaultAsync(i =>
                                  i.Name == row.SecurityName && i.BankSourceId == bankSourceId)
                            : await _context.Investments.FirstOrDefaultAsync(i =>
                                  i.ISIN == row.ISIN);

                        if (investment == null)
                        {
                            investment = new Investment
                            {
                                Name = row.SecurityName ?? row.ISIN!,
                                ISIN = row.ISIN,
                                Currency = row.Currency,
                                BankSourceId = bankSourceId,
                                AccountNumber = row.AccountNumber,
                                LastUpdated = DateTime.Now,
                                UserId = userId
                            };
                            _context.Investments.Add(investment);
                            await _context.SaveChangesAsync(); // persist to get an ID
                        }

                        investmentCache[cacheKey] = investment;
                    }

                    // Check duplicate: same date + type + amount on same investment
                    var isDuplicate = await _context.InvestmentTransactions.AnyAsync(t =>
                        t.InvestmentId == investment.InvestmentId &&
                        t.TransactionDate == row.Date &&
                        t.TransactionType == row.TransactionType &&
                        t.TotalAmount == row.TotalAmount);

                    if (isDuplicate)
                    {
                        result.DuplicateCount++;
                        continue;
                    }

                    var invTransaction = new InvestmentTransaction
                    {
                        InvestmentId = investment.InvestmentId,
                        TransactionType = row.TransactionType,
                        TransactionDate = row.Date,
                        Quantity = row.Quantity,
                        PricePerShare = row.PricePerShare,
                        // TotalAmount is always stored as a positive magnitude; directionality
                        // is already encoded in TransactionType ("Köp", "Sälj", "Utdelning", etc.).
                        TotalAmount = Math.Abs(row.TotalAmount),
                        Fees = row.Fees,
                        Currency = row.Currency,
                        CreatedAt = DateTime.UtcNow,
                        UserId = userId
                    };

                    _context.InvestmentTransactions.Add(invTransaction);
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new CsvImportError
                    {
                        ErrorType = "Row",
                        ErrorMessage = $"Kunde inte importera rad: {ex.Message}"
                    });
                    result.ErrorCount++;
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
                ErrorType = "Import",
                ErrorMessage = $"Ett fel uppstod vid import: {ex.Message}"
            });
            result.ErrorCount++;
        }

        return result;
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}

