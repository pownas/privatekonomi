using System.Globalization;
using System.Text;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.Parsers;

public class SwedbankParser : ICsvParser
{
    private const int MaxHeaderSearchLines = 5;
    
    public string BankName => "Swedbank";

    public bool CanParse(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return false;

        // Find header row (might not be on line 0 due to metadata lines)
        var headerIndex = FindHeaderRow(lines);
        if (headerIndex == -1) return false;
        
        var header = lines[headerIndex].ToLower();
        
        // Check for new CSN format (Swedish column names, tab-separated)
        if (header.Contains("radnummer") && header.Contains("bokföringsdag") && 
            header.Contains("belopp") && header.Contains("beskrivning"))
        {
            return true;
        }
        
        // Check for old format (English column names, semicolon-separated)
        return header.Contains("row type") && header.Contains("debit/credit") && 
               (header.Contains("client account") || header.Contains("details"));
    }

    public async Task<ParseResult> ParseAsync(Stream csvStream)
    {
        string content;
        // Try reading as UTF-8 first
        using (var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
        {
            content = await reader.ReadToEndAsync();
        }
        // If we see replacement characters, try Windows-1252
        if (content.Contains('\uFFFD') || content.Contains('?'))
        {
            try
            {
                csvStream.Position = 0;
                using var reader1252 = new StreamReader(csvStream, Encoding.GetEncoding("Windows-1252"), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                content = await reader1252.ReadToEndAsync();
            }
            catch { /* fallback failed, keep original content */ }
        }

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return new ParseResult
            {
                Warnings = { new ParseWarning { RowNumber = 0, WarningType = "EmptyFile", Message = "Filen innehåller inga rader att läsa in." } }
            };
        }

        // Find header row (might not be on line 0 due to metadata lines)
        var headerIndex = FindHeaderRow(lines);
        if (headerIndex == -1)
        {
            throw new InvalidOperationException("Kunde inte hitta rubriker i Swedbank CSV-filen. Kan den vara sparad i felaktig encoding? Behöver vara UTF-8 eller Windows-1252.");
        }

        var header = NormalizeHeader(lines[headerIndex]);

        // Detect format and parse accordingly
        if (IsSwedishCsnHeader(header))
        {
            // New CSN format with Swedish column names (tab-separated)
            return await ParseCsnFormatAsync(lines, headerIndex);
        }
        else
        {
            // Old format with English column names (semicolon-separated)
            return await ParseOldFormatAsync(lines, headerIndex);
        }
    }

    private async Task<ParseResult> ParseCsnFormatAsync(string[] lines, int headerIndex)
    {
        var transactions = new List<Transaction>();
        var warnings = new List<ParseWarning>();
        
        // Detect separator (comma, tab, or semicolon) from header
        var separator = DetectSeparator(lines[headerIndex]);
        
        // Parse header - handle quoted fields
        var header = ParseCsvLine(lines[headerIndex], separator).ToArray();
        var dateIndex = FindColumnIndex(header, new[] { "bokföringsdag", "bokforingsdag", "transaktionsdag" });
        var amountIndex = FindColumnIndex(header, new[] { "belopp" });
        var descriptionIndex = FindColumnIndex(header, new[] { "beskrivning" });
        var referenceIndex = FindColumnIndex(header, new[] { "referens" });
        var currencyIndex = FindColumnIndex(header, new[] { "valuta" });
        var clearingIndex = FindColumnIndex(header, new[] { "clearingnummer" });
        var accountIndex = FindColumnIndex(header, new[] { "kontonummer" });

        if (dateIndex == -1 || amountIndex == -1)
        {
            var missing = new List<string>();
            if (dateIndex == -1) missing.Add("'Bokföringsdag'");
            if (amountIndex == -1) missing.Add("'Belopp'");
            throw new InvalidOperationException(
                $"Kunde inte hitta nödvändiga kolumner i Swedbank CSV-filen. " +
                $"Saknar: {string.Join(", ", missing)}. " +
                $"Hittade kolumner: {string.Join(", ", header.Select(h => h.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)))}. " +
                "Kan filen vara sparad i felaktig encoding? Behöver vara UTF-8.");
        }

        // Parse data rows (start from headerIndex + 1)
        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var rowNumber = i + 1;
            try
            {
                var columns = ParseCsvLine(rawLine, separator).ToArray();
                if (columns.Length <= Math.Max(dateIndex, amountIndex))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "TooFewColumns",
                        Message = $"Rad {rowNumber} hoppades över – för få kolumner (hittade {columns.Length}, behöver minst {Math.Max(dateIndex, amountIndex) + 1}).",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Check currency if available (only process SEK for now)
                if (currencyIndex != -1 && columns.Length > currencyIndex)
                {
                    var currency = columns[currencyIndex].Trim();
                    if (!string.IsNullOrEmpty(currency) && !string.Equals(currency, "SEK", StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add(new ParseWarning
                        {
                            RowNumber = rowNumber,
                            WarningType = "UnsupportedCurrency",
                            Message = $"Rad {rowNumber} hoppades över – valutan '{currency}' stöds inte (endast SEK).",
                            RawData = ParserHelpers.Truncate(rawLine)
                        });
                        continue;
                    }
                }

                var dateStr = columns[dateIndex].Trim();
                var amountStr = columns[amountIndex].Trim().Replace(",", ".");

                // Build description from available fields
                var description = string.Empty;
                if (descriptionIndex != -1 && columns.Length > descriptionIndex)
                {
                    description = columns[descriptionIndex].Trim();
                }
                if (string.IsNullOrWhiteSpace(description) && referenceIndex != -1 && columns.Length > referenceIndex)
                {
                    description = columns[referenceIndex].Trim();
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "MissingDescription",
                        Message = $"Rad {rowNumber} hoppades över – varken 'Beskrivning' eller 'Referens' har ett värde.",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Parse date (YYYY-MM-DD format for CSN)
                if (!TryParseDate(dateStr, out var date))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "InvalidDate",
                        Message = $"Rad {rowNumber} hoppades över – kunde inte tolka datum '{dateStr}' (förväntat format: ÅÅÅÅ-MM-DD).",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Parse amount
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "InvalidAmount",
                        Message = $"Rad {rowNumber} hoppades över – kunde inte tolka belopp '{columns[amountIndex].Trim()}'.",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Determine if income based on sign (negative = expense, positive = income)
                var isIncome = amount > 0;

                // Extract account info
                var clearingNumber = clearingIndex != -1 && columns.Length > clearingIndex
                    ? columns[clearingIndex].Trim()
                    : null;
                var accountNumber = accountIndex != -1 && columns.Length > accountIndex
                    ? columns[accountIndex].Trim()
                    : null;

                var transaction = new Transaction
                {
                    Date = date,
                    Amount = Math.Abs(amount),
                    IsIncome = isIncome,
                    Description = description.Length > 500 ? description.Substring(0, 500) : description,
                    ClearingNumber = string.IsNullOrWhiteSpace(clearingNumber) ? null : clearingNumber,
                    AccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber
                };

                transactions.Add(transaction);
            }
            catch (Exception ex)
            {
                warnings.Add(new ParseWarning
                {
                    RowNumber = rowNumber,
                    WarningType = "ParseError",
                    Message = $"Rad {rowNumber} hoppades över – oväntat fel: {ex.Message}",
                    RawData = ParserHelpers.Truncate(rawLine)
                });
            }
        }

        return await Task.FromResult(new ParseResult { Transactions = transactions, Warnings = warnings });
    }

    private async Task<ParseResult> ParseOldFormatAsync(string[] lines, int headerIndex)
    {
        var transactions = new List<Transaction>();
        var warnings = new List<ParseWarning>();
        
        // Swedbank uses semicolon separator and quotes
        var separator = ';';
        
        // Parse header
        var header = ParseCsvLine(lines[headerIndex], separator).ToArray();
        var rowTypeIndex = FindColumnIndex(header, new[] { "row type" });
        var dateIndex = FindColumnIndex(header, new[] { "date" });
        var amountIndex = FindColumnIndex(header, new[] { "amount" });
        var debitCreditIndex = FindColumnIndex(header, new[] { "debit/credit" });
        var detailsIndex = FindColumnIndex(header, new[] { "details" });
        var beneficiaryIndex = FindColumnIndex(header, new[] { "beneficiary/payer" });
        var currencyIndex = FindColumnIndex(header, new[] { "currency" });
        var clientAccountIndex = FindColumnIndex(header, new[] { "client account" });

        if (rowTypeIndex == -1 || dateIndex == -1 || amountIndex == -1 || debitCreditIndex == -1 || detailsIndex == -1)
        {
            var missing = new List<string>();
            if (rowTypeIndex == -1) missing.Add("'Row Type'");
            if (dateIndex == -1) missing.Add("'Date'");
            if (amountIndex == -1) missing.Add("'Amount'");
            if (debitCreditIndex == -1) missing.Add("'Debit/Credit'");
            if (detailsIndex == -1) missing.Add("'Details'");
            throw new InvalidOperationException(
                $"Kunde inte hitta nödvändiga kolumner i Swedbank CSV-filen. " +
                $"Saknar: {string.Join(", ", missing)}. " +
                $"Hittade kolumner: {string.Join(", ", header.Select(h => h.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)))}. " +
                "Kan filen vara sparad i felaktig encoding? Behöver vara UTF-8.");
        }

        // Parse data rows (start from headerIndex + 1)
        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var rowNumber = i + 1;
            try
            {
                var columns = ParseCsvLine(rawLine, separator).ToArray();
                if (columns.Length <= Math.Max(rowTypeIndex, Math.Max(dateIndex, Math.Max(amountIndex, debitCreditIndex))))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "TooFewColumns",
                        Message = $"Rad {rowNumber} hoppades över – för få kolumner.",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                var rowType = columns[rowTypeIndex].Trim();
                
                // Skip non-transaction rows (10=opening balance, 82=turnover, 86=closing balance)
                if (rowType != "20")
                    continue;

                // Check currency if available (only process SEK for now)
                if (currencyIndex != -1 && columns.Length > currencyIndex)
                {
                    var currency = columns[currencyIndex].Trim();
                    if (!string.IsNullOrEmpty(currency) && currency != "SEK")
                    {
                        warnings.Add(new ParseWarning
                        {
                            RowNumber = rowNumber,
                            WarningType = "UnsupportedCurrency",
                            Message = $"Rad {rowNumber} hoppades över – valutan '{currency}' stöds inte (endast SEK).",
                            RawData = ParserHelpers.Truncate(rawLine)
                        });
                        continue;
                    }
                }

                var dateStr = columns[dateIndex].Trim();
                var amountStr = columns[amountIndex].Trim().Replace(",", ".");
                var debitCredit = columns[debitCreditIndex].Trim().ToUpper();
                var details = columns[detailsIndex].Trim();
                var beneficiary = beneficiaryIndex != -1 && columns.Length > beneficiaryIndex 
                    ? columns[beneficiaryIndex].Trim() 
                    : string.Empty;

                // Parse date (DD.MM.YYYY format)
                if (!TryParseDate(dateStr, out var date))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "InvalidDate",
                        Message = $"Rad {rowNumber} hoppades över – kunde inte tolka datum '{dateStr}' (förväntat format: DD.MM.ÅÅÅÅ).",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Parse amount
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "InvalidAmount",
                        Message = $"Rad {rowNumber} hoppades över – kunde inte tolka belopp '{columns[amountIndex].Trim()}'.",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Determine if income based on Debit/Credit flag
                var isIncome = debitCredit == "K"; // K = Kredit (income), D = Debet (expense)

                // Build description from beneficiary and details
                var description = BuildDescription(beneficiary, details);
                if (string.IsNullOrWhiteSpace(description))
                {
                    warnings.Add(new ParseWarning
                    {
                        RowNumber = rowNumber,
                        WarningType = "MissingDescription",
                        Message = $"Rad {rowNumber} hoppades över – varken 'Beneficiary/Payer' eller 'Details' har ett värde.",
                        RawData = ParserHelpers.Truncate(rawLine)
                    });
                    continue;
                }

                // Extract account number from Client Account column
                var accountNumber = clientAccountIndex != -1 && columns.Length > clientAccountIndex
                    ? columns[clientAccountIndex].Trim()
                    : null;

                var transaction = new Transaction
                {
                    Date = date,
                    Amount = Math.Abs(amount),
                    IsIncome = isIncome,
                    Description = description.Length > 500 ? description.Substring(0, 500) : description,
                    AccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber
                };

                transactions.Add(transaction);
            }
            catch (Exception ex)
            {
                warnings.Add(new ParseWarning
                {
                    RowNumber = rowNumber,
                    WarningType = "ParseError",
                    Message = $"Rad {rowNumber} hoppades över – oväntat fel: {ex.Message}",
                    RawData = ParserHelpers.Truncate(rawLine)
                });
            }
        }

        return await Task.FromResult(new ParseResult { Transactions = transactions, Warnings = warnings });
    }

    private string BuildDescription(string beneficiary, string details)
    {
        if (!string.IsNullOrWhiteSpace(beneficiary) && !string.IsNullOrWhiteSpace(details))
        {
            return $"{beneficiary} - {details}";
        }
        else if (!string.IsNullOrWhiteSpace(beneficiary))
        {
            return beneficiary;
        }
        else
        {
            return details;
        }
    }

    private List<string> ParseCsvLine(string line, char separator)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        var insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == separator && !insideQuotes)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());
        return result;
    }

    private int FindColumnIndex(string[] header, string[] possibleNames)
    {
        for (int i = 0; i < header.Length; i++)
        {
            var columnName = NormalizeHeader(header[i]).Trim();
            foreach (var name in possibleNames)
            {
                var searchName = NormalizeHeader(name).Trim();
                if (columnName.Contains(searchName))
                    return i;
            }
        }
        return -1;
    }

    private bool TryParseDate(string dateStr, out DateTime date)
    {
        var formats = new[]
        {
            "dd.MM.yyyy",
            "yyyy-MM-dd",
            "dd-MM-yyyy",
            "yyyy/MM/dd"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
        }

        date = DateTime.MinValue;
        return false;
    }

    private char DetectSeparator(string headerLine)
    {
        // Count occurrences of potential separators (excluding those inside quotes)
        var commaCount = 0;
        var tabCount = 0;
        var semicolonCount = 0;
        var insideQuotes = false;

        for (int i = 0; i < headerLine.Length; i++)
        {
            var c = headerLine[i];
            
            if (c == '"')
            {
                // Check for escaped quote (two consecutive quotes)
                if (i + 1 < headerLine.Length && headerLine[i + 1] == '"')
                {
                    i++; // Skip the next quote as it's escaped
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (!insideQuotes)
            {
                if (c == ',') commaCount++;
                else if (c == '\t') tabCount++;
                else if (c == ';') semicolonCount++;
            }
        }

        // Return the most common separator
        if (commaCount >= tabCount && commaCount >= semicolonCount)
            return ',';
        else if (tabCount >= semicolonCount)
            return '\t';
        else
            return ';';
    }

    /// <summary>
    /// Finds the header row in the CSV file. Swedbank exports sometimes include metadata lines
    /// before the actual header row with column names. This method searches the first few lines
    /// to locate the header.
    /// </summary>
    /// <param name="lines">All lines from the CSV file</param>
    /// <returns>Index of the header row, or -1 if not found within the first MaxHeaderSearchLines (5) lines</returns>
    private int FindHeaderRow(string[] lines)
    {
        for (int i = 0; i < Math.Min(lines.Length, MaxHeaderSearchLines); i++)
        {
            var line = NormalizeHeader(lines[i]).Trim().TrimStart('\uFEFF');

            // Check for Swedish CSN format headers (tolerant for encoding issues)
            if (IsSwedishCsnHeader(line))
            {
                return i;
            }

            // Check for English format headers
            if (line.Contains("row type") && line.Contains("debit/credit") &&
                (line.Contains("client account") || line.Contains("details")))
            {
                return i;
            }
        }

        return -1; // Header not found
    }

    private static bool IsSwedishCsnHeader(string normalizedHeaderLine)
    {
        // NormalizeHeader replaces å/ä/ö with a/a/o, so "bokföringsdag" becomes "bokforingsdag".
        return normalizedHeaderLine.Contains("radnummer") &&
               (normalizedHeaderLine.Contains("bokforingsdag") ||
                normalizedHeaderLine.Contains("bokföringsdag") ||
                normalizedHeaderLine.Contains("bokf?ringsdag") ||
                normalizedHeaderLine.Contains("bokf\0ringsdag")) &&
               normalizedHeaderLine.Contains("belopp") &&
               normalizedHeaderLine.Contains("beskrivning");
    }

    // Normalize header for encoding issues (replace common encoding errors)
    private static string NormalizeHeader(string header)
    {
#pragma warning disable CA1307 // Specify StringComparison for clarity
        return header.ToLowerInvariant()
            .Replace('å', 'a').Replace('ä', 'a').Replace('ö', 'o')
            .Replace('\u00e5', 'a').Replace('\u00e4', 'a').Replace('\u00f6', 'o')
            .Replace('\uFFFD', '?') // replacement char
            // Common encoding artefacts for "för".
            .Replace("f?r", "for")
            .Replace("f\0r", "for")
            .Replace("f 0r", "for");
#pragma warning restore CA1307 // Specify StringComparison for clarity
    }
}
