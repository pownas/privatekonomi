using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// User preferences for reminder escalation and follow-up behavior
/// </summary>
public class ReminderSettings
{
    public int ReminderSettingsId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Enable automatic escalation for repeatedly snoozed reminders
    /// </summary>
    public bool EnableEscalation { get; set; } = true;
    
    /// <summary>
    /// Number of snoozes before escalation
    /// </summary>
    public int SnoozeThresholdForEscalation { get; set; } = 3;
    
    /// <summary>
    /// Enable email notifications for escalated reminders
    /// </summary>
    public bool EscalateToEmail { get; set; }
    
    /// <summary>
    /// Enable SMS notifications for escalated reminders
    /// </summary>
    public bool EscalateToSMS { get; set; }
    
    /// <summary>
    /// Enable push notifications for escalated reminders
    /// </summary>
    public bool EscalateToPush { get; set; } = true;
    
    /// <summary>
    /// Default snooze duration in minutes (60 = 1 hour)
    /// </summary>
    public int DefaultSnoozeDurationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Enable automatic follow-up for incomplete reminders
    /// </summary>
    public bool EnableFollowUp { get; set; } = true;
    
    /// <summary>
    /// Follow-up interval in hours
    /// </summary>
    public int FollowUpIntervalHours { get; set; } = 24;
    
    /// <summary>
    /// Maximum number of follow-ups before marking as expired
    /// </summary>
    public int MaxFollowUps { get; set; } = 3;
    
    /// <summary>
    /// Quiet hours start (hour of day, 0-23)
    /// </summary>
    public int QuietHoursStart { get; set; } = 22; // 10 PM
    
    /// <summary>
    /// Quiet hours end (hour of day, 0-23)
    /// </summary>
    public int QuietHoursEnd { get; set; } = 7; // 7 AM
    
    /// <summary>
    /// Respect quiet hours for reminders
    /// </summary>
    public bool RespectQuietHours { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
