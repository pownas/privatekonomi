using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// User preferences for notification delivery
/// </summary>
public class NotificationPreference
{
    public int NotificationPreferenceId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Required]
    public SystemNotificationType NotificationType { get; set; }
    
    /// <summary>
    /// Enabled channels for this notification type (flags)
    /// </summary>
    public NotificationChannels EnabledChannels { get; set; } = NotificationChannels.InApp;
    
    /// <summary>
    /// Priority threshold - only send if notification priority meets or exceeds this
    /// </summary>
    public NotificationPriority MinimumPriority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// Whether notifications of this type are completely disabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Enable digest mode - group notifications instead of sending immediately
    /// </summary>
    public bool DigestMode { get; set; }
    
    /// <summary>
    /// How often to send digest (in hours)
    /// </summary>
    public int DigestIntervalHours { get; set; } = 24;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification channel flags for multi-selection
/// </summary>
[Flags]
public enum NotificationChannels
{
    None = 0,
    InApp = 1 << 0,      // 1
    Email = 1 << 1,      // 2
    SMS = 1 << 2,        // 4
    Push = 1 << 3,       // 8
    Slack = 1 << 4,      // 16
    Teams = 1 << 5       // 32
}

/// <summary>
/// Do Not Disturb schedule for a user
/// </summary>
public class DoNotDisturbSchedule
{
    public int DoNotDisturbScheduleId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Day of week (0 = Sunday, 6 = Saturday, 7 = All days)
    /// </summary>
    public int DayOfWeek { get; set; } = 7; // All days by default
    
    /// <summary>
    /// Start time (24-hour format, e.g., "22:00")
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string StartTime { get; set; } = "22:00";
    
    /// <summary>
    /// End time (24-hour format, e.g., "08:00")
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string EndTime { get; set; } = "08:00";
    
    /// <summary>
    /// Whether this DND schedule is active
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Allow critical notifications even during DND
    /// </summary>
    public bool AllowCritical { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configuration for external notification integrations
/// </summary>
public class NotificationIntegration
{
    public int NotificationIntegrationId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Required]
    public NotificationChannel Channel { get; set; }
    
    /// <summary>
    /// Integration-specific configuration (JSON format)
    /// E.g., Slack webhook URL, Teams channel ID, etc.
    /// </summary>
    [Required]
    public string Configuration { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this integration is active
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Last time this integration was used successfully
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Last error message if integration failed
    /// </summary>
    [MaxLength(500)]
    public string? LastErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
