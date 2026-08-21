using System.Globalization;
using System.Text;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.Parsers;

public class AvanzaHoldingsPerAccountParser : IInvestmentCsvParser
{
    public string BankName => "Avanza";
    public string FormatType => "Per konto";

    public bool CanParse(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return false;

        var header = lines[0].ToLower(CultureInfo.InvariantCulture);
        return header.Contains("kontonummer") && 
               header.Contains("namn") && 
               header.Contains("volym") && 
               header.Contains("marknadsvärde");
    }

    public async Task<List<Investment>> ParseAsync(Stream csvStream)
    {
        var investments = new List<Investment>();
        
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return investments;

        // Detect separator (tab, pipe for table format, or semicolon/comma for CSV)
        var separator = lines[0].Contains('|') ? '|' : 
                       lines[0].Contains('\t') ? '\t' : 
                       lines[0].Contains(';') ? ';' : ',';
        
        // Parse header
        var header = lines[0].Split(separator);
        var accountIndex = FindColumnIndex(header, new[] { "kontonummer", "account number" });
        var nameIndex = FindColumnIndex(header, new[] { "namn", "name" });
        var shortNameIndex = FindColumnIndex(header, new[] { "kortnamn", "short name", "ticker" });
        var volumeIndex = FindColumnIndex(header, new[] { "volym", "volume", "quantity" });
        var marketValueIndex = FindColumnIndex(header, new[] { "marknadsvärde", "market value" });
        var gavSekIndex = FindColumnIndex(header, new[] { "gav (sek)", "gav" });
        var currencyIndex = FindColumnIndex(header, new[] { "valuta", "currency" });
        var countryIndex = FindColumnIndex(header, new[] { "land", "country" });
        var isinIndex = FindColumnIndex(header, new[] { "isin" });
        var marketIndex = FindColumnIndexExact(header, new[] { "marknad", "market", "exchange" });
        var typeIndex = FindColumnIndex(header, new[] { "typ", "type" });

        if (nameIndex == -1 || volumeIndex == -1 || marketValueIndex == -1)
        {
            throw new InvalidOperationException("Kunde inte hitta nödvändiga kolumner i CSV-filen");
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var columns = lines[i].Split(separator);
                
                // Skip header separators in markdown tables
                if (columns.All(c => c.Trim().All(ch => ch == '-' || ch == ' ' || ch == '|')))
                    continue;
                    
                if (columns.Length <= Math.Max(nameIndex, Math.Max(volumeIndex, marketValueIndex)))
                    continue;

                var name = GetColumnValue(columns, nameIndex);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var volumeStr = GetColumnValue(columns, volumeIndex).Replace(",", ".").Replace(" ", "").Replace(" ", "");
                var marketValueStr = GetColumnValue(columns, marketValueIndex).Replace(",", ".").Replace(" ", "").Replace(" ", "");
                var gavStr = gavSekIndex >= 0 ? GetColumnValue(columns, gavSekIndex).Replace(",", ".").Replace(" ", "").Replace(" ", "") : "";

                if (!decimal.TryParse(volumeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                    continue;
                    
                if (!decimal.TryParse(marketValueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var marketValue))
                    continue;

                decimal gav = 0;
                if (!string.IsNullOrEmpty(gavStr))
                {
                    decimal.TryParse(gavStr, NumberStyles.Any, CultureInfo.CurrentCulture, out gav);
                }

                var currentPrice = volume > 0 ? marketValue / volume : 0;
                var type = MapAvanzaTypeToInvestmentType(GetColumnValue(columns, typeIndex));

                var investment = new Investment
                {
                    Name = name,
                    ShortName = GetColumnValue(columns, shortNameIndex),
                    Type = type,
                    Quantity = volume,
                    PurchasePrice = gav,
                    CurrentPrice = currentPrice,
                    AccountNumber = GetColumnValue(columns, accountIndex),
                    ISIN = GetColumnValue(columns, isinIndex),
                    Currency = GetColumnValue(columns, currencyIndex),
                    Country = GetColumnValue(columns, countryIndex),
                    Market = GetColumnValue(columns, marketIndex),
                    PurchaseDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                investments.Add(investment);
            }
            catch
            {
                // Skip invalid rows
                continue;
            }
        }

        return investments;
    }

    private int FindColumnIndex(string[] header, string[] possibleNames)
    {
        for (int i = 0; i < header.Length; i++)
        {
            var columnName = header[i].Trim();
            foreach (var name in possibleNames)
            {
                if (columnName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private int FindColumnIndexExact(string[] header, string[] possibleNames)
    {
        for (int i = 0; i < header.Length; i++)
        {
            var columnName = header[i].Trim().Replace(" ", "").Replace("(", "").Replace(")", "");
            foreach (var name in possibleNames)
            {
                var searchName = name.Replace(" ", "").Replace("(", "").Replace(")", "");
                if (string.Equals(columnName, searchName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private string GetColumnValue(string[] columns, int index)
    {
        if (index < 0 || index >= columns.Length)
            return string.Empty;
        return columns[index].Trim();
    }

    private string MapAvanzaTypeToInvestmentType(string avanzaType)
    {
        return avanzaType.ToUpper(CultureInfo.InvariantCulture) switch
        {
            "STOCK" => "Aktie",
            "FUND" => "Fond",
            "EXCHANGE_TRADED_FUND" => "Fond",
            "CERTIFICATE" => "Certifikat",
            _ => "Övrigt"
        };
    }
}
