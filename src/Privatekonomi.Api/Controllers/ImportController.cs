using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using System.Collections.Concurrent;
using System.Globalization;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly ICsvImportService _csvImportService;
    private readonly ILogger<ImportController> _logger;
    private static readonly ConcurrentDictionary<string, (byte[] Content, string FileName, long FileSize)> _tempFiles = new();

    public ImportController(ICsvImportService csvImportService, ILogger<ImportController> logger)
    {
        _csvImportService = csvImportService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file for preview (CSV or OFX format).
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PreviewResponse>> Upload([FromForm] FileUploadRequest request)
    {
        try
        {
            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { error = "Ingen fil vald" });
            }

            if (request.File.Length > 10 * 1024 * 1024) // 10 MB
            {
                return BadRequest(new { error = "Filen är för stor. Max storlek är 10 MB." });
            }

            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt" && extension != ".ofx" && extension != ".qfx")
            {
                return BadRequest(new { error = "Filtypen stöds inte. Endast .csv, .ofx och .qfx-filer accepteras." });
            }

            // Read file content
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            // Parse and preview
            memoryStream.Position = 0;
            
            // Determine bank name for OFX files
            var effectiveBankName = request.BankName;
            if ((extension == ".ofx" || extension == ".qfx") && !request.BankName.Contains("OFX", StringComparison.OrdinalIgnoreCase))
            {
                effectiveBankName = "OFX (Allmän)";
            }
            
            var result = await _csvImportService.PreviewCsvAsync(memoryStream, effectiveBankName);

            if (!result.Success)
            {
                return BadRequest(new { error = "Kunde inte läsa filen", errors = result.Errors });
            }

            // Store file temporarily
            var fileId = Guid.NewGuid().ToString();
            _tempFiles[fileId] = (fileContent, request.File.FileName, request.File.Length);

            // Clean up old temp files (older than 1 hour)
            CleanupOldTempFiles();

            var preview = result.Transactions.Take(5).Select(t => new
            {
                date = t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                amount = t.Amount,
                description = t.Description,
                isIncome = t.IsIncome
            }).ToList<object>();

            return Ok(new PreviewResponse
            {
                Success = true,
                FileId = fileId,
                Preview = preview,
                Summary = new SummaryResponse
                {
                    TotalRows = result.TotalRows,
                    ValidTransactions = result.ImportedCount,
                    Duplicates = result.DuplicateCount,
                    Errors = result.ErrorCount,
                    TotalAmount = result.Summary.TotalAmount,
                    IncomeAmount = result.Summary.IncomeAmount,
                    ExpenseAmount = result.Summary.ExpenseAmount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { error = $"Ett fel uppstod: {ex.Message}" });
        }
    }

    /// <summary>
    /// Confirm and execute the import.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<ImportResponse>> Confirm([FromBody] ConfirmRequest request)
    {
        try
        {
            if (!_tempFiles.TryGetValue(request.FileId, out var fileData))
            {
                return BadRequest(new { error = "Filen kunde inte hittas. Vänligen ladda upp filen igen." });
            }

            // Get authenticated user's ID from the HttpContext
            var userId = User.Identity?.IsAuthenticated == true 
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : null;

            // Import transactions with job tracking
            using var memoryStream = new MemoryStream(fileData.Content);
            var (result, importJob) = await _csvImportService.ImportWithJobAsync(
                memoryStream, 
                request.Bank, 
                fileData.FileName, 
                fileData.FileSize,
                userId: userId,
                request.SkipDuplicates);

            // Remove temp file
            _tempFiles.TryRemove(request.FileId, out _);

            if (!result.Success)
            {
                return BadRequest(new { error = "Import misslyckades", errors = result.Errors, jobId = importJob.ImportJobId });
            }

            return Ok(new ImportResponse
            {
                Success = true,
                Imported = result.ImportedCount,
                Duplicates = result.DuplicateCount,
                Errors = result.ErrorCount,
                ErrorDetails = result.Errors,
                JobId = importJob.ImportJobId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming import");
            return StatusCode(500, new { error = $"Ett fel uppstod: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get the status of an import job.
    /// </summary>
    [HttpGet("{id}/status")]
    public async Task<ActionResult<ImportJobStatusResponse>> GetImportStatus(int id)
    {
        try
        {
            var importJob = await _csvImportService.GetImportJobAsync(id);
            
            if (importJob == null)
            {
                return NotFound(new { error = "Import-jobb hittades inte" });
            }

            return Ok(new ImportJobStatusResponse
            {
                ImportJobId = importJob.ImportJobId,
                Status = importJob.Status,
                BankName = importJob.BankName,
                FileType = importJob.FileType,
                FileName = importJob.FileName,
                TotalRows = importJob.TotalRows,
                ImportedCount = importJob.ImportedCount,
                DuplicateCount = importJob.DuplicateCount,
                ErrorCount = importJob.ErrorCount,
                Source = importJob.Source,
                CreatedAt = importJob.CreatedAt,
                StartedAt = importJob.StartedAt,
                CompletedAt = importJob.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import status for job {JobId}", id);
            return StatusCode(500, new { error = $"Ett fel uppstod: {ex.Message}" });
        }
    }
    
    /// <summary>
    /// Get all supported banks for import.
    /// </summary>
    [HttpGet("banks")]
    public ActionResult<List<BankInfo>> GetSupportedBanks()
    {
        var banks = new List<BankInfo>
        {
            new() { Name = "ICA-banken", FileTypes = new[] { "CSV" }, Description = "ICA-banken CSV-export" },
            new() { Name = "Swedbank", FileTypes = new[] { "CSV" }, Description = "Swedbank CSV-export (båda formaten)" },
            new() { Name = "OFX (Allmän)", FileTypes = new[] { "OFX", "QFX" }, Description = "OFX/QFX-format (stöds av många banker)" }
        };
        
        return Ok(banks);
    }

    private void CleanupOldTempFiles()
    {
        // In a production system, you would track creation times and remove old files
        // For simplicity, we'll limit the number of temp files
        if (_tempFiles.Count > 100)
        {
            var oldestKeys = _tempFiles.Keys.Take(_tempFiles.Count - 100).ToList();
            foreach (var key in oldestKeys)
            {
                _tempFiles.TryRemove(key, out _);
            }
        }
    }
}

public class PreviewResponse
{
    public bool Success { get; set; }
    public string FileId { get; set; } = string.Empty;
    public List<object> Preview { get; set; } = new();
    public SummaryResponse Summary { get; set; } = new();
}

public class SummaryResponse
{
    public int TotalRows { get; set; }
    public int ValidTransactions { get; set; }
    public int Duplicates { get; set; }
    public int Errors { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
}

public class ConfirmRequest
{
    public string FileId { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public bool SkipDuplicates { get; set; } = true;
}

public class ImportResponse
{
    public bool Success { get; set; }
    public int Imported { get; set; }
    public int Duplicates { get; set; }
    public int Errors { get; set; }
    public List<CsvImportError> ErrorDetails { get; set; } = new();
    public int JobId { get; set; }
}

public class ImportJobStatusResponse
{
    public int ImportJobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BankInfo
{
    public string Name { get; set; } = string.Empty;
    public string[] FileTypes { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
}

public class FileUploadRequest
{
    /// <summary>
    /// The file to upload
    /// </summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// The name of the bank
    /// </summary>
    public string BankName { get; set; } = string.Empty;
}
