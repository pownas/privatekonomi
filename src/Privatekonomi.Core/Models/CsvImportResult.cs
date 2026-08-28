namespace Privatekonomi.Core.Models;

public class CsvImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public List<CsvImportError> Errors { get; set; } = new();
    public List<ParseWarning> Warnings { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
    public ImportSummary Summary { get; set; } = new();
}

public class CsvImportError
{
    public int RowNumber { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RawData { get; set; } = string.Empty;
}

/// <summary>
/// A non-fatal warning produced during CSV parsing, e.g. a row that was skipped
/// due to an unrecognised date format or a missing field value.
/// </summary>
public class ParseWarning
{
    /// <summary>1-based row number in the source file (0 = file-level warning).</summary>
    public int RowNumber { get; set; }
    public string WarningType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>Raw content of the skipped row, truncated to 200 chars.</summary>
    public string RawData { get; set; } = string.Empty;
}

public class ImportSummary
{
    public decimal TotalAmount { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
}
