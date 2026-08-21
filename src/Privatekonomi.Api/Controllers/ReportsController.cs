using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IBankSourceService _bankSourceService;
    private readonly IReportService _reportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ITransactionService transactionService,
        IBankSourceService bankSourceService,
        IReportService reportService,
        ICurrentUserService currentUserService,
        ILogger<ReportsController> logger)
    {
        _transactionService = transactionService;
        _bankSourceService = bankSourceService;
        _reportService = reportService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Hämta nettoprövning över tid
    /// </summary>
    [HttpGet("networth")]
    public async Task<ActionResult<NetWorthReport>> GetNetWorth(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var startDateValue = startDate ?? DateTime.Now.AddYears(-1);
            var endDateValue = endDate ?? DateTime.Now;

            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(startDateValue, endDateValue);
            
            // Calculate net worth over time
            var groupedByMonth = transactions
                .OrderBy(t => t.Date)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new NetWorthDataPoint
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Income = g.Where(t => t.IsIncome).Sum(t => t.Amount),
                    Expense = g.Where(t => !t.IsIncome).Sum(t => t.Amount),
                    NetWorth = g.Sum(t => t.IsIncome ? t.Amount : -t.Amount)
                })
                .ToList();

            // Calculate cumulative net worth
            decimal cumulative = 0;
            foreach (var point in groupedByMonth)
            {
                cumulative += point.NetWorth;
                point.CumulativeNetWorth = cumulative;
            }

            return Ok(new NetWorthReport
            {
                StartDate = startDateValue,
                EndDate = endDateValue,
                DataPoints = groupedByMonth,
                CurrentNetWorth = cumulative
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating net worth");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Månatlig sammanfattning (inkomst, utgift, top-kategorier)
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<MonthlySummary>> GetSummary(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        try
        {
            var targetYear = year ?? DateTime.Now.Year;
            var targetMonth = month ?? DateTime.Now.Month;

            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(startDate, endDate);

            var income = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var expense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);

            // Get top categories by expense
            var topCategories = transactions
                .Where(t => !t.IsIncome)
                .SelectMany(t => t.TransactionCategories.Select(tc => new
                {
                    CategoryId = tc.CategoryId,
                    CategoryName = tc.Category?.Name ?? "Unknown",
                    Amount = t.Amount * tc.Percentage / 100
                }))
                .GroupBy(x => new { x.CategoryId, x.CategoryName })
                .Select(g => new CategorySummary
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalAmount = g.Sum(x => x.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.TotalAmount)
                .Take(10)
                .ToList();

            return Ok(new MonthlySummary
            {
                Year = targetYear,
                Month = targetMonth,
                Income = income,
                Expense = expense,
                Net = income - expense,
                TransactionCount = transactions.Count(),
                TopCategories = topCategories
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly summary");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta månadsrapport för ett specifikt år och månad (format: YYYY-MM)
    /// </summary>
    /// <param name="month">Månad i format YYYY-MM (t.ex. 2025-01)</param>
    /// <param name="householdId">Valfritt hushålls-ID</param>
    /// <returns>Detaljerad månadsrapport med sammanfattning av ekonomi</returns>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyReportData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MonthlyReportData>> GetMonthlyReport(
        [FromQuery] string month,
        [FromQuery] int? householdId = null)
    {
        try
        {
            // Parse month parameter (format: YYYY-MM)
            if (string.IsNullOrEmpty(month) || !TryParseYearMonth(month, out int year, out int monthNum))
            {
                return BadRequest(new { error = "Ogiltigt månadsformat. Använd YYYY-MM (t.ex. 2025-01)" });
            }

            // Validate month is not in the future
            var requestedMonth = new DateTime(year, monthNum, 1);
            if (requestedMonth > DateTime.Today)
            {
                return BadRequest(new { error = "Kan inte generera rapport för framtida månad" });
            }

            // Get userId from authentication context (null if not authenticated, allowing anonymous access for demo)
            var userId = _currentUserService.UserId;

            var reportData = await _reportService.GenerateMonthlyReportAsync(year, monthNum, userId, householdId);

            return Ok(reportData);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid month parameter: {Month}", month);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly report for {Month}", month);
            return StatusCode(500, new { error = "Ett fel uppstod vid generering av månadsrapporten" });
        }
    }

    /// <summary>
    /// Hämta lista över tillgängliga månadsrapporter
    /// </summary>
    /// <param name="limit">Max antal rapporter att returnera (standard: 12)</param>
    /// <param name="householdId">Valfritt hushålls-ID</param>
    /// <returns>Lista över månadsrapporter</returns>
    [HttpGet("monthly/list")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyReportSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MonthlyReportSummary>>> GetMonthlyReportsList(
        [FromQuery] int limit = 12,
        [FromQuery] int? householdId = null)
    {
        try
        {
            // Get userId from authentication context (null if not authenticated, allowing anonymous access for demo)
            var userId = _currentUserService.UserId;

            var reports = await _reportService.GetMonthlyReportsAsync(userId, householdId, limit);

            var summaries = reports.Select(r => new MonthlyReportSummary
            {
                Year = r.ReportMonth.Year,
                Month = r.ReportMonth.Month,
                MonthName = GetSwedishMonthName(r.ReportMonth.Month),
                TotalIncome = r.TotalIncome,
                TotalExpenses = r.TotalExpenses,
                NetFlow = r.NetFlow,
                GeneratedAt = r.GeneratedAt,
                Status = r.Status.ToString()
            });

            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly reports list");
            return StatusCode(500, new { error = "Ett fel uppstod vid hämtning av rapportlista" });
        }
    }

    private static bool TryParseYearMonth(string input, out int year, out int month)
    {
        year = 0;
        month = 0;

        if (string.IsNullOrEmpty(input))
            return false;

        var parts = input.Split('-');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out year) || !int.TryParse(parts[1], out month))
            return false;

        if (month < 1 || month > 12)
            return false;

        if (year < 2000 || year > 2100)
            return false;

        return true;
    }

    private static string GetSwedishMonthName(int month)
    {
        string[] months = { "januari", "februari", "mars", "april", "maj", "juni",
                          "juli", "augusti", "september", "oktober", "november", "december" };
        return month >= 1 && month <= 12 ? months[month - 1] : "";
    }
}

public class NetWorthReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<NetWorthDataPoint> DataPoints { get; set; } = new();
    public decimal CurrentNetWorth { get; set; }
}

public class NetWorthDataPoint
{
    public DateTime Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal NetWorth { get; set; }
    public decimal CumulativeNetWorth { get; set; }
}

public class MonthlySummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
    public int TransactionCount { get; set; }
    public List<CategorySummary> TopCategories { get; set; } = new();
}

public class CategorySummary
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>
/// Summary of a monthly report for list display
/// </summary>
public class MonthlyReportSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetFlow { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
