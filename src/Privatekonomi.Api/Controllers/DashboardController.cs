using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

/// <summary>
/// Dashboard API controller for aggregated ekonomisk översikt.
/// Provides endpoint for retrieving aggregated dashboard data.
/// </summary>
[LegacyApiDisabled]
[ApiController]
[Route("api/me")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get aggregated dashboard data including balance, budget status, upcoming bills, and insights.
    /// </summary>
    /// <param name="accountIds">Optional comma-separated list of account IDs to include in balance calculation</param>
    /// <returns>Aggregated dashboard data</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardData>> GetDashboard([FromQuery] string? accountIds = null)
    {
        try
        {
            int[]? accountIdArray = null;
            if (!string.IsNullOrEmpty(accountIds))
            {
                accountIdArray = accountIds.Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var result) ? result : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToArray();
            }

            var dashboardData = await _dashboardService.GetDashboardDataAsync(accountIdArray);
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve dashboard data");
        }
    }

    /// <summary>
    /// Get balance summary for selected accounts.
    /// </summary>
    /// <param name="accountIds">Optional comma-separated list of account IDs</param>
    /// <returns>Balance summary</returns>
    [HttpGet("dashboard/balance")]
    [ProducesResponseType(typeof(BalanceSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BalanceSummary>> GetBalanceSummary([FromQuery] string? accountIds = null)
    {
        try
        {
            int[]? accountIdArray = null;
            if (!string.IsNullOrEmpty(accountIds))
            {
                accountIdArray = accountIds.Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var result) ? result : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToArray();
            }

            var balance = await _dashboardService.GetBalanceSummaryAsync(accountIdArray);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance summary");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve balance summary");
        }
    }

    /// <summary>
    /// Get current month's budget status.
    /// </summary>
    /// <returns>Budget status summary</returns>
    [HttpGet("dashboard/budget-status")]
    [ProducesResponseType(typeof(BudgetStatusSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BudgetStatusSummary>> GetBudgetStatus()
    {
        try
        {
            var budgetStatus = await _dashboardService.GetBudgetStatusAsync();
            return Ok(budgetStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget status");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve budget status");
        }
    }

    /// <summary>
    /// Get list of upcoming bills.
    /// </summary>
    /// <param name="daysAhead">Number of days ahead to look for bills (default: 30)</param>
    /// <param name="limit">Maximum number of bills to return (default: 5)</param>
    /// <returns>List of upcoming bills</returns>
    [HttpGet("dashboard/upcoming-bills")]
    [ProducesResponseType(typeof(List<BillSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<BillSummary>>> GetUpcomingBills(
        [FromQuery] int daysAhead = 30,
        [FromQuery] int limit = 5)
    {
        try
        {
            var bills = await _dashboardService.GetUpcomingBillsAsync(daysAhead, limit);
            return Ok(bills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming bills");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve upcoming bills");
        }
    }

    /// <summary>
    /// Get recent insights and alerts.
    /// </summary>
    /// <param name="limit">Maximum number of insights to return (default: 5)</param>
    /// <returns>List of insights</returns>
    [HttpGet("dashboard/insights")]
    [ProducesResponseType(typeof(List<InsightItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<InsightItem>>> GetInsights([FromQuery] int limit = 5)
    {
        try
        {
            var insights = await _dashboardService.GetRecentInsightsAsync(limit);
            return Ok(insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insights");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve insights");
        }
    }
}
