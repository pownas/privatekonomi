using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Implementation of notification preference service
/// </summary>
public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly PrivatekonomyContext _context;

    public NotificationPreferenceService(PrivatekonomyContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationPreference>> GetUserPreferencesAsync(string userId)
    {
        return await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task<NotificationPreference?> GetPreferenceAsync(string userId, SystemNotificationType type)
    {
        return await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type);
    }

    public async Task<NotificationPreference> SavePreferenceAsync(NotificationPreference preference)
    {
        preference.UpdatedAt = DateTime.UtcNow;

        var existing = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.NotificationPreferenceId == preference.NotificationPreferenceId);

        if (existing != null)
        {
            existing.EnabledChannels = preference.EnabledChannels;
            existing.MinimumPriority = preference.MinimumPriority;
            existing.IsEnabled = preference.IsEnabled;
            existing.DigestMode = preference.DigestMode;
            existing.DigestIntervalHours = preference.DigestIntervalHours;
            existing.UpdatedAt = preference.UpdatedAt;
        }
        else
        {
            preference.CreatedAt = DateTime.UtcNow;
            _context.NotificationPreferences.Add(preference);
        }

        await _context.SaveChangesAsync();
        return existing ?? preference;
    }

    public async Task<List<DoNotDisturbSchedule>> GetDndSchedulesAsync(string userId)
    {
        return await _context.DoNotDisturbSchedules
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task<DoNotDisturbSchedule> SaveDndScheduleAsync(DoNotDisturbSchedule schedule)
    {
        schedule.UpdatedAt = DateTime.UtcNow;

        var existing = await _context.DoNotDisturbSchedules
            .FirstOrDefaultAsync(s => s.DoNotDisturbScheduleId == schedule.DoNotDisturbScheduleId);

        if (existing != null)
        {
            existing.DayOfWeek = schedule.DayOfWeek;
            existing.StartTime = schedule.StartTime;
            existing.EndTime = schedule.EndTime;
            existing.IsEnabled = schedule.IsEnabled;
            existing.AllowCritical = schedule.AllowCritical;
            existing.UpdatedAt = schedule.UpdatedAt;
        }
        else
        {
            schedule.CreatedAt = DateTime.UtcNow;
            _context.DoNotDisturbSchedules.Add(schedule);
        }

        await _context.SaveChangesAsync();
        return existing ?? schedule;
    }

    public async Task DeleteDndScheduleAsync(int scheduleId, string userId)
    {
        var schedule = await _context.DoNotDisturbSchedules
            .FirstOrDefaultAsync(s => s.DoNotDisturbScheduleId == scheduleId && s.UserId == userId);

        if (schedule != null)
        {
            _context.DoNotDisturbSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<NotificationIntegration>> GetIntegrationsAsync(string userId)
    {
        return await _context.NotificationIntegrations
            .Where(i => i.UserId == userId)
            .ToListAsync();
    }

    public async Task<NotificationIntegration> SaveIntegrationAsync(NotificationIntegration integration)
    {
        integration.UpdatedAt = DateTime.UtcNow;

        var existing = await _context.NotificationIntegrations
            .FirstOrDefaultAsync(i => i.NotificationIntegrationId == integration.NotificationIntegrationId);

        if (existing != null)
        {
            existing.Channel = integration.Channel;
            existing.Configuration = integration.Configuration;
            existing.IsEnabled = integration.IsEnabled;
            existing.UpdatedAt = integration.UpdatedAt;
        }
        else
        {
            integration.CreatedAt = DateTime.UtcNow;
            _context.NotificationIntegrations.Add(integration);
        }

        await _context.SaveChangesAsync();
        return existing ?? integration;
    }

    public async Task DeleteIntegrationAsync(int integrationId, string userId)
    {
        var integration = await _context.NotificationIntegrations
            .FirstOrDefaultAsync(i => i.NotificationIntegrationId == integrationId && i.UserId == userId);

        if (integration != null)
        {
            _context.NotificationIntegrations.Remove(integration);
            await _context.SaveChangesAsync();
        }
    }

    public async Task InitializeDefaultPreferencesAsync(string userId)
    {
        // Check if user already has preferences
        var existingPreferences = await _context.NotificationPreferences
            .AnyAsync(p => p.UserId == userId);

        if (existingPreferences)
        {
            return; // Already initialized
        }

        // Create default preferences for all notification types
        var defaultPreferences = new List<NotificationPreference>();

        // Critical notifications - InApp + Email
        var criticalTypes = new[]
        {
            SystemNotificationType.BillOverdue,
            SystemNotificationType.BankSyncFailed,
            SystemNotificationType.SignificantLoss,
            SystemNotificationType.LowBalance
        };

        foreach (var type in criticalTypes)
        {
            defaultPreferences.Add(new NotificationPreference
            {
                UserId = userId,
                NotificationType = type,
                EnabledChannels = NotificationChannels.InApp | NotificationChannels.Email,
                MinimumPriority = NotificationPriority.High,
                IsEnabled = true,
                DigestMode = false
            });
        }

        // Important notifications - InApp only, can be digest
        var importantTypes = new[]
        {
            SystemNotificationType.BudgetExceeded,
            SystemNotificationType.BudgetWarning,
            SystemNotificationType.UpcomingBill,
            SystemNotificationType.BillDue,
            SystemNotificationType.GoalAchieved,
            SystemNotificationType.InvestmentChange,
            SystemNotificationType.UnusualTransaction,
            SystemNotificationType.HouseholdActivity,
            SystemNotificationType.SubscriptionPriceIncrease
        };

        foreach (var type in importantTypes)
        {
            defaultPreferences.Add(new NotificationPreference
            {
                UserId = userId,
                NotificationType = type,
                EnabledChannels = NotificationChannels.InApp,
                MinimumPriority = NotificationPriority.Normal,
                IsEnabled = true,
                DigestMode = false
            });
        }

        // Low priority notifications - InApp only, digest by default
        var lowPriorityTypes = new[]
        {
            SystemNotificationType.GoalMilestone,
            SystemNotificationType.BankSyncSuccess,
            SystemNotificationType.HouseholdInvitation,
            SystemNotificationType.SubscriptionRenewal
        };

        foreach (var type in lowPriorityTypes)
        {
            defaultPreferences.Add(new NotificationPreference
            {
                UserId = userId,
                NotificationType = type,
                EnabledChannels = NotificationChannels.InApp,
                MinimumPriority = NotificationPriority.Low,
                IsEnabled = true,
                DigestMode = true,
                DigestIntervalHours = 24
            });
        }

        _context.NotificationPreferences.AddRange(defaultPreferences);

        // Create default DND schedule (22:00 - 08:00 every day)
        var dndSchedule = new DoNotDisturbSchedule
        {
            UserId = userId,
            DayOfWeek = 7, // All days
            StartTime = "22:00",
            EndTime = "08:00",
            IsEnabled = true,
            AllowCritical = true
        };

        _context.DoNotDisturbSchedules.Add(dndSchedule);

        await _context.SaveChangesAsync();
    }
}
