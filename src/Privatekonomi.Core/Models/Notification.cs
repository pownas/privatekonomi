using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a notification sent to a user
/// </summary>
public class Notification
{
    public int NotificationId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Required]
    public SystemNotificationType Type { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional data in JSON format for notification-specific details
    /// </summary>
    public string? Data { get; set; }
    
    /// <summary>
    /// Channel through which notification was sent
    /// </summary>
    public NotificationChannel Channel { get; set; }
    
    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// Whether notification has been read
    /// </summary>
    public bool IsRead { get; set; }
    
    /// <summary>
    /// When notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }
    
    /// <summary>
    /// When notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When notification was sent
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// If sending failed, error message
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Link to navigate to when notification is clicked
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }
    
    /// <summary>
    /// Snooze until this date/time
    /// </summary>
    public DateTime? SnoozeUntil { get; set; }
    
    /// <summary>
    /// Number of times this notification has been snoozed
    /// </summary>
    public int SnoozeCount { get; set; }
    
    /// <summary>
    /// Related BillReminder ID if this is a bill reminder notification
    /// </summary>
    public int? BillReminderId { get; set; }
}

/// <summary>
/// System notification types for budget, transactions, and system events
/// </summary>
public enum SystemNotificationType
{
    // Budget-related
    BudgetExceeded = 1,
    BudgetWarning = 2,
    
    // Account balance
    LowBalance = 10,
    
    // Bills and payments
    UpcomingBill = 20,
    BillDue = 21,
    BillOverdue = 22,
    
    // Goals
    GoalAchieved = 30,
    GoalMilestone = 31,
    
    // Investments
    InvestmentChange = 40,
    SignificantGain = 41,
    SignificantLoss = 42,
    
    // Transactions
    UnusualTransaction = 50,
    LargeTransaction = 51,
    
    // Bank sync
    BankSyncFailed = 60,
    BankSyncSuccess = 61,
    
    // Household
    HouseholdActivity = 70,
    HouseholdInvitation = 71,
    
    // Subscriptions
    SubscriptionPriceIncrease = 80,
    SubscriptionRenewal = 81,
    
    // System
    SystemMaintenance = 90,
    SystemAlert = 91
}

/// <summary>
/// Available notification channels
/// </summary>
public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    SMS = 3,
    Push = 4,
    Slack = 5,
    Teams = 6
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Snooze duration options
/// </summary>
public enum SnoozeDuration
{
    OneHour = 1,
    OneDay = 2,
    OneWeek = 3
}
