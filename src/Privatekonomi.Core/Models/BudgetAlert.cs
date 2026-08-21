namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a budget alert triggered when spending reaches certain thresholds
/// </summary>
public class BudgetAlert
{
    public int BudgetAlertId { get; set; }
    
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;
    
    public int BudgetCategoryId { get; set; }
    public BudgetCategory BudgetCategory { get; set; } = null!;
    
    /// <summary>
    /// Threshold percentage that triggered this alert (e.g., 75, 90, 100)
    /// </summary>
    public decimal ThresholdPercentage { get; set; }
    
    /// <summary>
    /// Current spending percentage when alert was triggered
    /// </summary>
    public decimal CurrentPercentage { get; set; }
    
    /// <summary>
    /// Amount spent at the time of alert
    /// </summary>
    public decimal SpentAmount { get; set; }
    
    /// <summary>
    /// Planned amount for the budget category
    /// </summary>
    public decimal PlannedAmount { get; set; }
    
    /// <summary>
    /// When the alert was triggered
    /// </summary>
    public DateTime TriggeredAt { get; set; }
    
    /// <summary>
    /// Whether this alert is still active (not acknowledged)
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the alert was acknowledged by the user
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
    
    /// <summary>
    /// Forecasted number of days until budget is exceeded (null if not applicable)
    /// </summary>
    public int? ForecastDaysUntilExceeded { get; set; }
    
    /// <summary>
    /// Daily spending rate calculated at time of alert
    /// </summary>
    public decimal DailyRate { get; set; }
    
    /// <summary>
    /// User ID for alert ownership
    /// </summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}

/// <summary>
/// Settings for budget alert thresholds and behavior
/// </summary>
public class BudgetAlertSettings
{
    public int BudgetAlertSettingsId { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Enable alerts at 75% threshold
    /// </summary>
    public bool EnableAlert75 { get; set; } = true;
    
    /// <summary>
    /// Enable alerts at 90% threshold
    /// </summary>
    public bool EnableAlert90 { get; set; } = true;
    
    /// <summary>
    /// Enable alerts at 100% threshold
    /// </summary>
    public bool EnableAlert100 { get; set; } = true;
    
    /// <summary>
    /// Custom alert thresholds (comma-separated percentages)
    /// </summary>
    public string? CustomThresholds { get; set; }
    
    /// <summary>
    /// Enable forecast warnings
    /// </summary>
    public bool EnableForecastWarnings { get; set; } = true;
    
    /// <summary>
    /// Minimum days ahead for forecast warnings
    /// </summary>
    public int ForecastWarningDays { get; set; } = 7;
    
    /// <summary>
    /// Enable budget freeze when exceeded
    /// </summary>
    public bool EnableBudgetFreeze { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Tracks which budgets are frozen (blocked from new expenses)
/// </summary>
public class BudgetFreeze
{
    public int BudgetFreezeId { get; set; }
    
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;
    
    public int? BudgetCategoryId { get; set; }
    public BudgetCategory? BudgetCategory { get; set; }
    
    /// <summary>
    /// When the freeze was activated
    /// </summary>
    public DateTime FrozenAt { get; set; }
    
    /// <summary>
    /// When the freeze was lifted (null if still frozen)
    /// </summary>
    public DateTime? UnfrozenAt { get; set; }
    
    /// <summary>
    /// Reason for the freeze
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Whether this freeze is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// User ID who owns this budget
    /// </summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
