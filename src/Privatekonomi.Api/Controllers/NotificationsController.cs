using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly ILogger<NotificationsController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        INotificationPreferenceService preferenceService,
        ILogger<NotificationsController> logger,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _preferenceService = preferenceService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Hämta användarens notifikationer
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta antal olästa notifikationer
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Markera notifikation som läst
    /// </summary>
    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _notificationService.MarkAsReadAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Markera alla notifikationer som lästa
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort notifikation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _notificationService.DeleteNotificationAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta användarens notifikationspreferenser
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<IEnumerable<NotificationPreference>>> GetPreferences()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var preferences = await _preferenceService.GetUserPreferencesAsync(userId);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification preferences");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera notifikationspreferens
    /// </summary>
    [HttpPut("preferences/{id}")]
    public async Task<ActionResult<NotificationPreference>> UpdatePreference(int id, NotificationPreference preference)
    {
        if (id != preference.NotificationPreferenceId)
        {
            return BadRequest();
        }

        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            preference.UserId = userId; // Ensure correct user
            var updated = await _preferenceService.SavePreferenceAsync(preference);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preference");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta Do Not Disturb-scheman
    /// </summary>
    [HttpGet("dnd-schedules")]
    public async Task<ActionResult<IEnumerable<DoNotDisturbSchedule>>> GetDndSchedules()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var schedules = await _preferenceService.GetDndSchedulesAsync(userId);
            return Ok(schedules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving DND schedules");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa eller uppdatera Do Not Disturb-schema
    /// </summary>
    [HttpPost("dnd-schedules")]
    public async Task<ActionResult<DoNotDisturbSchedule>> SaveDndSchedule(DoNotDisturbSchedule schedule)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            schedule.UserId = userId; // Ensure correct user
            var saved = await _preferenceService.SaveDndScheduleAsync(schedule);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving DND schedule");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort Do Not Disturb-schema
    /// </summary>
    [HttpDelete("dnd-schedules/{id}")]
    public async Task<IActionResult> DeleteDndSchedule(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _preferenceService.DeleteDndScheduleAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting DND schedule");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta notifikationsintegrationer
    /// </summary>
    [HttpGet("integrations")]
    public async Task<ActionResult<IEnumerable<NotificationIntegration>>> GetIntegrations()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var integrations = await _preferenceService.GetIntegrationsAsync(userId);
            return Ok(integrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integrations");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa eller uppdatera integration
    /// </summary>
    [HttpPost("integrations")]
    public async Task<ActionResult<NotificationIntegration>> SaveIntegration(NotificationIntegration integration)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            integration.UserId = userId; // Ensure correct user
            var saved = await _preferenceService.SaveIntegrationAsync(integration);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving integration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort integration
    /// </summary>
    [HttpDelete("integrations/{id}")]
    public async Task<IActionResult> DeleteIntegration(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _preferenceService.DeleteIntegrationAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting integration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Initiera standardpreferenser för användare
    /// </summary>
    [HttpPost("initialize-defaults")]
    public async Task<IActionResult> InitializeDefaults()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _preferenceService.InitializeDefaultPreferencesAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default preferences");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Snooze en notifikation
    /// </summary>
    [HttpPost("{id}/snooze")]
    public async Task<IActionResult> SnoozeNotification(int id, [FromBody] SnoozeRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _notificationService.SnoozeNotificationAsync(id, userId, request.Duration);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid snooze operation for notification {NotificationId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error snoozing notification {NotificationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Markera påminnelse som slutförd
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteReminder(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            await _notificationService.MarkReminderAsCompletedAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid complete operation for notification {NotificationId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing reminder {NotificationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta aktiva notifikationer (exkluderar snoozade)
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetActiveNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var notifications = await _notificationService.GetActiveNotificationsAsync(userId, unreadOnly);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active notifications");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for snoozing a notification
/// </summary>
public class SnoozeRequest
{
    public SnoozeDuration Duration { get; set; }
}
