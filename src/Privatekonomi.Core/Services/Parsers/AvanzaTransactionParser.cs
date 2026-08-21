using System.Globalization;
using System.Text;

namespace Privatekonomi.Core.Services.Parsers;

/// <summary>
/// Structured row parsed from an Avanza transaction history CSV export.
/// Rows with a non-empty <see cref="ISIN"/> represent investment activity (buy/sell/dividend).
/// </summary>
public class AvanzaTransactionRow
{
    public DateTime Date { get; set; }
    /// <summary>Typ av transaktion, e.g. "Köp", "Sälj", "Utdelning", "Insättning".</summary>
    public string TransactionType { get; set; } = string.Empty;
    public string? SecurityName { get; set; }
    public string? ISIN { get; set; }
    public string? AccountNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerShare { get; set; }
    /// <summary>Signed total amount (negative for buys/withdrawals).</summary>
    public decimal TotalAmount { get; set; }
    public decimal? Fees { get; set; }
    public string Currency { get; set; } = "SEK";
}

/// <summary>
/// Parser for Avanza transaction history CSV exports
/// (exported from Avanza "Min ekonomi → Transaktioner → Exportera till CSV").
/// Rows are returned as <see cref="AvanzaTransactionRow"/> objects; use
/// <see cref="InvestmentService.ImportFromCsvAsync"/> to persist them as investment records.
/// </summary>
public class AvanzaTransactionParser
{
    public string BankName => "Avanza";

    /// <summary>
    /// Returns true when the CSV content looks like an Avanza transaction history export.
    /// The header must contain "datum", "typ av transaktion", and "belopp".
    /// </summary>
    public bool CanParse(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent)) return false;

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return false;

        var header = lines[0].ToLowerInvariant();
        return header.Contains("datum", StringComparison.OrdinalIgnoreCase) &&
               header.Contains("typ av transaktion", StringComparison.OrdinalIgnoreCase) &&
               header.Contains("belopp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses the CSV stream and returns one <see cref="AvanzaTransactionRow"/> per data row.
    /// Rows with missing mandatory fields are silently skipped.
    /// </summary>
    public async Task<List<AvanzaTransactionRow>> ParseTransactionsAsync(Stream csvStream)
    {
        var rows = new List<AvanzaTransactionRow>();

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync();

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return rows;

        var separator = DetectSeparator(lines[0]);
        var header = SplitLine(lines[0], separator);

        var dateIndex        = FindColumnIndex(header, "datum");
        var typeIndex        = FindColumnIndex(header, "typ av transaktion");
        var securityIndex    = FindColumnIndex(header, "värdepapper/beskrivning", "värdepapper", "beskrivning");
        var quantityIndex    = FindColumnIndex(header, "antal");
        var priceIndex       = FindColumnIndex(header, "kurs");
        var amountIndex      = FindColumnIndex(header, "belopp");
        var feesIndex        = FindColumnIndex(header, "courtage");
        var currencyIndex    = FindColumnIndex(header, "valuta");
        var isinIndex        = FindColumnIndex(header, "isin");
        var accountIndex     = FindColumnIndex(header, "konto");

        if (dateIndex == -1 || typeIndex == -1 || amountIndex == -1)
            throw new InvalidOperationException(
                "Kunde inte hitta nödvändiga kolumner (Datum, Typ av transaktion, Belopp) i CSV-filen.");

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var cols = SplitLine(lines[i], separator);
                int maxRequired = Math.Max(dateIndex, Math.Max(typeIndex, amountIndex));
                if (cols.Length <= maxRequired) continue;

                var dateStr = cols[dateIndex].Trim().Trim('"');
                var transactionType = cols[typeIndex].Trim().Trim('"');
                var amountStr = cols[amountIndex].Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(transactionType))
                    continue;

                if (!TryParseDate(dateStr, out var date)) continue;

                if (!TryParseDecimal(amountStr, out var totalAmount)) continue;

                var row = new AvanzaTransactionRow
                {
                    Date = date,
                    TransactionType = transactionType,
                    TotalAmount = totalAmount,
                    Currency = GetColumn(cols, currencyIndex) is { Length: > 0 } c ? c : "SEK",
                    ISIN = GetColumn(cols, isinIndex),
                    SecurityName = GetColumn(cols, securityIndex),
                    AccountNumber = GetColumn(cols, accountIndex),
                    Fees = TryParseDecimal(GetColumn(cols, feesIndex) ?? "", out var fees) && fees != 0 ? fees : null,
                    Quantity = TryParseDecimal(GetColumn(cols, quantityIndex) ?? "", out var qty) ? qty : 0m,
                    PricePerShare = TryParseDecimal(GetColumn(cols, priceIndex) ?? "", out var price) ? price : 0m,
                };

                rows.Add(row);
            }
            catch
            {
                // Skip invalid rows
            }
        }

        return rows;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string? GetColumn(string[] cols, int index)
    {
        if (index < 0 || index >= cols.Length) return null;
        var v = cols[index].Trim().Trim('"');
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static char DetectSeparator(string line)
    {
        if (line.Contains(';')) return ';';
        if (line.Contains('\t')) return '\t';
        return ',';
    }

    private static string[] SplitLine(string line, char separator)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == separator && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static int FindColumnIndex(string[] header, params string[] possibleNames)
    {
        for (int i = 0; i < header.Length; i++)
        {
            var col = header[i].Trim().Trim('"').ToLowerInvariant();
            foreach (var name in possibleNames)
            {
                if (col.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        var normalised = raw
            .Replace("\u00a0", "")  // non-breaking space
            .Replace(" ", "")
            .Replace(".", "")       // Swedish thousand separator
            .Replace(",", ".");     // decimal comma → dot
        return decimal.TryParse(normalised, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseDate(string s, out DateTime date)
    {
        string[] formats = { "yyyy-MM-dd", "dd.MM.yyyy", "dd-MM-yyyy", "yyyy/MM/dd" };
        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(s, fmt, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                return true;
        }
        date = DateTime.MinValue;
        return false;
    }
}
