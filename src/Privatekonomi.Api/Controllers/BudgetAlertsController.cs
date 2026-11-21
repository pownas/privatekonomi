using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

/// <summary>
/// API controller for managing budget alerts and warnings
/// </summary>
[LegacyApiDisabled]
[ApiController]
[Route("api/budget-alerts")]
public class BudgetAlertsController : ControllerBase
{
    private readonly IBudgetAlertService _budgetAlertService;
    private readonly ILogger<BudgetAlertsController> _logger;

    public BudgetAlertsController(
        IBudgetAlertService budgetAlertService,
        ILogger<BudgetAlertsController> logger)
    {
        _budgetAlertService = budgetAlertService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active alerts for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetAlert>>> GetActiveAlerts()
    {
        var alerts = await _budgetAlertService.GetActiveAlertsAsync();
        return Ok(alerts);
    }

    /// <summary>
    /// Get active alerts for a specific budget
    /// </summary>
    [HttpGet("budget/{budgetId}")]
    public async Task<ActionResult<IEnumerable<BudgetAlert>>> GetAlertsForBudget(int budgetId)
    {
        var alerts = await _budgetAlertService.GetActiveAlertsForBudgetAsync(budgetId);
        return Ok(alerts);
    }

    /// <summary>
    /// Get active alerts for a specific budget category
    /// </summary>
    [HttpGet("budget/{budgetId}/category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<BudgetAlert>>> GetAlertsForCategory(int budgetId, int categoryId)
    {
        var alerts = await _budgetAlertService.GetActiveAlertsForCategoryAsync(budgetId, categoryId);
        return Ok(alerts);
    }

    /// <summary>
    /// Check all active budgets and create alerts for threshold violations
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> CheckAllBudgets()
    {
        await _budgetAlertService.CheckAllBudgetsAsync();
        return Ok(new { message = "Budget check completed" });
    }

    /// <summary>
    /// Check a specific budget and create alerts if needed
    /// </summary>
    [HttpPost("check/{budgetId}")]
    public async Task<ActionResult<IEnumerable<BudgetAlert>>> CheckBudget(int budgetId)
    {
        var alerts = await _budgetAlertService.CheckBudgetAsync(budgetId);
        return Ok(alerts);
    }

    /// <summary>
    /// Acknowledge an alert (mark as read)
    /// </summary>
    [HttpPost("{alertId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int alertId)
    {
        await _budgetAlertService.AcknowledgeAlertAsync(alertId);
        return NoContent();
    }

    /// <summary>
    /// Calculate budget usage percentage for a category
    /// </summary>
    [HttpGet("budget/{budgetId}/category/{categoryId}/usage")]
    public async Task<ActionResult<BudgetUsageDto>> GetCategoryUsage(int budgetId, int categoryId)
    {
        var percentage = await _budgetAlertService.CalculateBudgetUsagePercentageAsync(budgetId, categoryId);
        var dailyRate = await _budgetAlertService.CalculateDailyRateAsync(budgetId, categoryId);
        var daysUntilExceeded = await _budgetAlertService.CalculateDaysUntilExceededAsync(budgetId, categoryId);

        return Ok(new BudgetUsageDto
        {
            BudgetId = budgetId,
            CategoryId = categoryId,
            UsagePercentage = percentage,
            DailyRate = dailyRate,
            DaysUntilExceeded = daysUntilExceeded
        });
    }

    /// <summary>
    /// Get or create budget alert settings for current user
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<BudgetAlertSettings>> GetSettings()
    {
        var settings = await _budgetAlertService.GetOrCreateSettingsAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Update budget alert settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<BudgetAlertSettings>> UpdateSettings([FromBody] BudgetAlertSettings settings)
    {
        try
        {
            var updatedSettings = await _budgetAlertService.UpdateSettingsAsync(settings);
            return Ok(updatedSettings);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }

    /// <summary>
    /// Freeze a budget or budget category
    /// </summary>
    [HttpPost("freeze")]
    public async Task<ActionResult<BudgetFreeze>> FreezeBudget([FromBody] FreezeBudgetRequest request)
    {
        var freeze = await _budgetAlertService.FreezeBudgetAsync(
            request.BudgetId, 
            request.CategoryId, 
            request.Reason);
        return Ok(freeze);
    }

    /// <summary>
    /// Unfreeze a budget or budget category
    /// </summary>
    [HttpPost("freeze/{freezeId}/unfreeze")]
    public async Task<IActionResult> UnfreezeBudget(int freezeId)
    {
        try
        {
            await _budgetAlertService.UnfreezeBudgetAsync(freezeId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Check if a budget or category is frozen
    /// </summary>
    [HttpGet("freeze/budget/{budgetId}")]
    public async Task<ActionResult<bool>> IsBudgetFrozen(int budgetId, [FromQuery] int? categoryId = null)
    {
        var isFrozen = await _budgetAlertService.IsBudgetFrozenAsync(budgetId, categoryId);
        return Ok(isFrozen);
    }

    /// <summary>
    /// Get all active budget freezes
    /// </summary>
    [HttpGet("freeze")]
    public async Task<ActionResult<IEnumerable<BudgetFreeze>>> GetActiveFreezes()
    {
        var freezes = await _budgetAlertService.GetActiveFreezesAsync();
        return Ok(freezes);
    }
}

/// <summary>
/// DTO for budget usage information
/// </summary>
public class BudgetUsageDto
{
    public int BudgetId { get; set; }
    public int CategoryId { get; set; }
    public decimal UsagePercentage { get; set; }
    public decimal DailyRate { get; set; }
    public int? DaysUntilExceeded { get; set; }
}

/// <summary>
/// Request model for freezing a budget
/// </summary>
public class FreezeBudgetRequest
{
    public int BudgetId { get; set; }
    public int? CategoryId { get; set; }
    public string? Reason { get; set; }
}
