using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public interface ICsvImportService
{
    /// <summary>
    /// Tries to auto-detect which bank/format the file belongs to by inspecting file content.
    /// Returns the bank name (e.g. "Swedbank") or null if no parser recognises the file.
    /// </summary>
    string? DetectBank(byte[] fileBytes);

    Task<CsvImportResult> ImportCsvAsync(Stream csvStream, string bankName, bool skipDuplicates = true);

    /// <summary>
    /// Preview the CSV without saving. Pass <paramref name="userId"/> to scope duplicate detection
    /// to the current user's existing transactions.
    /// </summary>
    Task<CsvImportResult> PreviewCsvAsync(Stream csvStream, string bankName, string? userId = null);
    
    /// <summary>
    /// Import transactions and create an ImportJob record for tracking.
    /// </summary>
    Task<(CsvImportResult Result, ImportJob Job)> ImportWithJobAsync(
        Stream stream, 
        string bankName, 
        string fileName, 
        long fileSize, 
        string? userId = null,
        bool skipDuplicates = true);
    
    /// <summary>
    /// Get an import job by ID.
    /// </summary>
    Task<ImportJob?> GetImportJobAsync(int importJobId);
    
    /// <summary>
    /// Get all import jobs for a user.
    /// </summary>
    Task<List<ImportJob>> GetUserImportJobsAsync(string userId);
}
