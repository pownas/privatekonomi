using System.Globalization;
using System.Text;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.Parsers;

public class NordnetSwedbankHoldingsParser : IInvestmentCsvParser
{
    public string BankName { get; } = "Nordnet/Swedbank";
    public string FormatType { get; } = "Holdings";

    public bool CanParse(string csvContent)
    {
        var header = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var lower = header.ToLowerInvariant();
        return (lower.Contains("isin") || lower.Contains("värdepapper") || lower.Contains("instrument"))
            && (lower.Contains("antal") || lower.Contains("volym") || lower.Contains("andelar"))
            && (lower.Contains("konto") || lower.Contains("marknadsvärde") || lower.Contains("värde"));
    }

    public async Task<List<Investment>> ParseAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return [];
        }

        var separator = lines[0].Contains(';') ? ';' : lines[0].Contains('\t') ? '\t' : ',';
        var headers = SplitLine(lines[0], separator);
        var result = new List<Investment>();

        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            var values = SplitLine(lines[lineIndex], separator);
            if (values.Count == 0)
            {
                continue;
            }

            var quantity = ParseDecimal(GetValue(headers, values, "antal", "volym", "andelar", "quantity"));
            var currentPrice = ParseDecimal(GetValue(headers, values, "kurs", "aktuellt värde", "pris", "senast"));
            var marketValue = ParseDecimal(GetValue(headers, values, "marknadsvärde", "värde", "total value"));
            var purchasePrice = ParseDecimal(GetValue(headers, values, "gav", "anskaffningsvärde", "inköpspris", "average"));

            quantity ??= ParseDecimal(GetExactValue(headers, values, "Antal", "Volym", "Andelar"));
            marketValue ??= ParseDecimal(GetExactValue(headers, values, "Marknadsvärde", "Värde"));
            purchasePrice ??= ParseDecimal(GetExactValue(headers, values, "GAV", "GAV (SEK)", "Inköpspris", "Anskaffningsvärde"));

            if (marketValue.HasValue && quantity.HasValue && quantity.Value > 0 && !currentPrice.HasValue)
            {
                currentPrice = marketValue.Value / quantity.Value;
            }
            var name = GetValue(headers, values, "namn", "värdepapper", "instrument", "instrumentnamn");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var accountNumber = GetValue(headers, values, "konto", "kontonummer", "account");
            var isin = GetValue(headers, values, "isin");
            var currency = GetValue(headers, values, "valuta", "currency") ?? "SEK";
            var typeText = GetValue(headers, values, "typ", "asset type")?.ToLowerInvariant() ?? "";
            var type = typeText.Contains("fond") ? "Fond" : typeText.Contains("aktie") ? "Aktie" : "Investering";

            result.Add(new Investment
            {
                Name = name,
                Type = type,
                Quantity = quantity ?? 0,
                PurchasePrice = purchasePrice ?? 0,
                CurrentPrice = currentPrice ?? marketValue ?? 0,
                PurchaseDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                ValidFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                AccountNumber = accountNumber,
                ISIN = isin,
                Currency = currency,
                AccountType = InferAccountType(accountNumber)
            });
        }

        return result;
    }

    private static string? InferAccountType(string? accountNumber)
    {
        return accountNumber?.Contains("ISK", StringComparison.OrdinalIgnoreCase) == true
            ? "ISK"
            : accountNumber?.Contains("KF", StringComparison.OrdinalIgnoreCase) == true
                ? "KF"
                : "AF";
    }

    private static List<string> SplitLine(string line, char separator)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (character == separator && !inQuotes)
            {
                values.Add(current.ToString().Trim().Trim('"'));
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        values.Add(current.ToString().Trim().Trim('"'));
        return values;
    }

    private static string? GetValue(List<string> headers, List<string> values, params string[] aliases)
    {
        for (var i = 0; i < headers.Count && i < values.Count; i++)
        {
            var header = headers[i].Trim().Trim('"').ToLowerInvariant();
            if (aliases.Any(alias => header.Contains(alias)))
            {
                return values[i].Trim();
            }
        }

        return null;
    }

    private static string? GetExactValue(List<string> headers, List<string> values, params string[] aliases)
    {
        for (var i = 0; i < headers.Count && i < values.Count; i++)
        {
            var header = headers[i].Trim().Trim('"');
            if (aliases.Any(alias => string.Equals(header, alias, StringComparison.OrdinalIgnoreCase)))
            {
                return values[i].Trim();
            }
        }

        return null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace(" ", "").Replace("%", "").Replace("SEK", "")
            .Replace("USD", "").Replace("EUR", "").Replace("NOK", "").Replace("DKK", "").Trim();
        normalized = normalized.TrimStart('+');
        normalized = normalized.Replace("\u00A0", "");

        if (normalized.Contains(',') && normalized.Contains('.'))
        {
            normalized = normalized.Replace(".", "").Replace(",", ".");
        }
        else if (normalized.Contains(','))
        {
            normalized = normalized.Replace(",", ".");
        }

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }
        return null;
    }
}
