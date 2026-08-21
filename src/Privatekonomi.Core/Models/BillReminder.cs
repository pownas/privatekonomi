using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a reminder for a bill
/// </summary>
public class BillReminder
{
    public int BillReminderId { get; set; }
    
    public int BillId { get; set; }
    public Bill? Bill { get; set; }
    
    /// <summary>
    /// Date to send reminder
    /// </summary>
    [Required]
    public DateTime ReminderDate { get; set; }
    
    /// <summary>
    /// Whether reminder was sent
    /// </summary>
    public bool IsSent { get; set; }
    
    /// <summary>
    /// Date reminder was sent
    /// </summary>
    public DateTime? SentDate { get; set; }
    
    /// <summary>
    /// Reminder method: Email, Notification, SMS
    /// </summary>
    [MaxLength(50)]
    public string ReminderMethod { get; set; } = "Notification";
    
    [MaxLength(500)]
    public string? Message { get; set; }
    
    /// <summary>
    /// Snooze until this date/time
    /// </summary>
    public DateTime? SnoozeUntil { get; set; }
    
    /// <summary>
    /// Number of times this reminder has been snoozed
    /// </summary>
    public int SnoozeCount { get; set; }
    
    /// <summary>
    /// Whether reminder has been marked as completed
    /// </summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>
    /// Date reminder was completed
    /// </summary>
    public DateTime? CompletedDate { get; set; }
    
    /// <summary>
    /// Escalation level for critical reminders (0 = none, 1-3 = escalation levels)
    /// </summary>
    public int EscalationLevel { get; set; }
    
    /// <summary>
    /// Last time a follow-up notification was sent
    /// </summary>
    public DateTime? LastFollowUpDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
