using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Services;
using Privatekonomi.Core.Services.Persistence;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IDataImportService _importService;
    private readonly ILogger<ExportController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly StorageSettings _storageSettings;

    public ExportController(
        IExportService exportService, 
        IDataImportService importService,
        ILogger<ExportController> logger,
        IServiceProvider serviceProvider,
        IOptions<StorageSettings> storageSettings)
    {
        _exportService = exportService;
        _importService = importService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _storageSettings = storageSettings.Value;
    }

    /// <summary>
    /// Export transactions to CSV format
    /// </summary>
    /// <param name="fromDate">Optional start date (format: yyyy-MM-dd)</param>
    /// <param name="toDate">Optional end date (format: yyyy-MM-dd)</param>
    /// <returns>CSV file download</returns>
    [HttpGet("transactions/csv")]
    public async Task<IActionResult> ExportTransactionsToCsv([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var csvData = await _exportService.ExportTransactionsToCsvAsync(fromDate, toDate);
            var fileName = $"transaktioner_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to CSV");
            return StatusCode(500, new { error = "Ett fel uppstod vid export av transaktioner" });
        }
    }

    /// <summary>
    /// Export transactions to JSON format
    /// </summary>
    /// <param name="fromDate">Optional start date (format: yyyy-MM-dd)</param>
    /// <param name="toDate">Optional end date (format: yyyy-MM-dd)</param>
    /// <returns>JSON file download</returns>
    [HttpGet("transactions/json")]
    public async Task<IActionResult> ExportTransactionsToJson([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var jsonData = await _exportService.ExportTransactionsToJsonAsync(fromDate, toDate);
            var fileName = $"transaktioner_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            return File(jsonData, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to JSON");
            return StatusCode(500, new { error = "Ett fel uppstod vid export av transaktioner" });
        }
    }

    /// <summary>
    /// Export a specific budget to CSV format
    /// </summary>
    /// <param name="budgetId">Budget ID to export</param>
    /// <returns>CSV file download</returns>
    [HttpGet("budgets/{budgetId}/csv")]
    public async Task<IActionResult> ExportBudgetToCsv(int budgetId)
    {
        try
        {
            var csvData = await _exportService.ExportBudgetToCsvAsync(budgetId);
            var fileName = $"budget_{budgetId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            return File(csvData, "text/csv", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting budget to CSV");
            return StatusCode(500, new { error = "Ett fel uppstod vid export av budget" });
        }
    }

    /// <summary>
    /// Create a full backup of all data
    /// </summary>
    /// <returns>JSON backup file download</returns>
    [HttpGet("backup")]
    public async Task<IActionResult> ExportFullBackup()
    {
        try
        {
            var backupData = await _exportService.ExportFullBackupAsync();
            var fileName = $"privatekonomi_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            return File(backupData, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating full backup");
            return StatusCode(500, new { error = "Ett fel uppstod vid skapande av backup" });
        }
    }

    /// <summary>
    /// Import a full backup from JSON file
    /// </summary>
    /// <param name="request">The import request containing the file and options</param>
    /// <returns>Import result with statistics</returns>
    [HttpPost("backup/import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFullBackup([FromForm] BackupImportRequest request)
    {
        try
        {
            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { error = "Ingen fil vald" });
            }

            if (request.File.Length > 50 * 1024 * 1024) // 50 MB
            {
                return BadRequest(new { error = "Filen är för stor. Max storlek är 50 MB." });
            }

            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (extension != ".json")
            {
                return BadRequest(new { error = "Filtypen stöds inte. Endast .json-filer accepteras." });
            }

            // Read file content
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            // Import backup
            var result = await _importService.ImportFullBackupAsync(fileContent, request.MergeMode);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage, warnings = result.Warnings });
            }

            return Ok(new
            {
                success = true,
                message = request.MergeMode ? "Backup importerad och sammanslagen med befintlig data" : "Backup importerad framgångsrikt",
                importedCounts = result.ImportedCounts,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing backup");
            return StatusCode(500, new { error = $"Ett fel uppstod vid import av backup: {ex.Message}" });
        }
    }

    /// <summary>
    /// Request model for backup import
    /// </summary>
    public class BackupImportRequest
    {
        /// <summary>
        /// The backup JSON file to import
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// If true, merges with existing data; if false, replaces all data
        /// </summary>
        public bool MergeMode { get; set; }
    }

    /// <summary>
    /// Manually save data to persistent storage (for JsonFile provider)
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("save")]
    public async Task<IActionResult> ManualSave()
    {
        try
        {
            if (!_storageSettings.Provider.Equals("JsonFile", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Manuell sparning är endast tillgänglig för JsonFile-lagringsprovider" });
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = scope.ServiceProvider.GetService<IDataPersistenceService>();

            if (persistenceService == null)
            {
                return BadRequest(new { error = "Persisteringstjänsten är inte tillgänglig" });
            }

            await persistenceService.SaveAsync(context);
            _logger.LogInformation("Manual save completed successfully");

            return Ok(new
            {
                success = true,
                message = "Data sparades framgångsrikt",
                timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual save");
            return StatusCode(500, new { error = $"Ett fel uppstod vid sparning: {ex.Message}" });
        }
    }
}
