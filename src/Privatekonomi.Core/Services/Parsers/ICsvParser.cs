using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.Parsers;

public interface ICsvParser
{
    string BankName { get; }

    /// <summary>
    /// Parses the CSV stream and returns both the parsed transactions and any
    /// per-row warnings for rows that were skipped or could not be fully parsed.
    /// </summary>
    Task<ParseResult> ParseAsync(Stream csvStream);

    bool CanParse(string csvContent);
}

/// <summary>
/// The result of parsing a CSV/OFX file: successfully parsed transactions plus
/// non-fatal warnings for skipped rows.
/// </summary>
public class ParseResult
{
    public List<Transaction> Transactions { get; init; } = new();
    public List<ParseWarning> Warnings { get; init; } = new();
}
