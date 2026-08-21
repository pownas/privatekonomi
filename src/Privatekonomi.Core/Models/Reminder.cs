using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// Representerar en flexibel påminnelse för användaren
/// Flexible reminder entity for various types of reminders
/// </summary>
public class Reminder
{
    public int ReminderId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Status of the reminder
    /// </summary>
    [Required]
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    
    /// <summary>
    /// When the reminder should trigger
    /// </summary>
    [Required]
    public DateTime ReminderDate { get; set; }
    
    /// <summary>
    /// Snooze until this date/time
    /// </summary>
    public DateTime? SnoozeUntil { get; set; }
    
    /// <summary>
    /// Number of times this reminder has been snoozed
    /// </summary>
    public int SnoozeCount { get; set; }
    
    /// <summary>
    /// Date when reminder was completed
    /// </summary>
    public DateTime? CompletedDate { get; set; }
    
    /// <summary>
    /// Priority level of the reminder
    /// </summary>
    public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;
    
    /// <summary>
    /// Type of reminder (Bill, Transaction, Goal, Custom, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ReminderType { get; set; }
    
    /// <summary>
    /// Related entity ID (e.g., BillId, TransactionId, GoalId)
    /// </summary>
    public int? RelatedEntityId { get; set; }
    
    /// <summary>
    /// Related entity type (e.g., "Bill", "Transaction", "Goal")
    /// </summary>
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }
    
    /// <summary>
    /// Escalation level for critical reminders (0 = none, 1-3 = escalation levels)
    /// </summary>
    public int EscalationLevel { get; set; }
    
    /// <summary>
    /// Last time a follow-up notification was sent
    /// </summary>
    public DateTime? LastFollowUpDate { get; set; }
    
    /// <summary>
    /// Whether to send follow-up notifications
    /// </summary>
    public bool EnableFollowUp { get; set; }
    
    /// <summary>
    /// Follow-up interval in hours
    /// </summary>
    public int FollowUpIntervalHours { get; set; } = 24;
    
    /// <summary>
    /// Maximum number of follow-ups before escalation
    /// </summary>
    public int MaxFollowUps { get; set; } = 3;
    
    /// <summary>
    /// Action URL when user clicks on the reminder
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }
    
    /// <summary>
    /// Tags for categorization (comma-separated)
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }
    
    /// <summary>
    /// Additional data in JSON format
    /// </summary>
    public string? Metadata { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Status of a reminder
/// </summary>
public enum ReminderStatus
{
    Active = 1,
    Snoozed = 2,
    Completed = 3,
    Dismissed = 4,
    Expired = 5
}

/// <summary>
/// Priority levels for reminders
/// </summary>
public enum ReminderPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}
