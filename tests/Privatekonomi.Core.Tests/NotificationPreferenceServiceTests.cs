using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class NotificationPreferenceServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly NotificationPreferenceService _preferenceService;
    private readonly string _testUserId = "test-user-id";

    public NotificationPreferenceServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _preferenceService = new NotificationPreferenceService(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task GetUserPreferencesAsync_ReturnsEmptyListForNewUser()
    {
        // Act
        var preferences = await _preferenceService.GetUserPreferencesAsync(_testUserId);

        // Assert
        Assert.AreEqual(0, preferences.Count());
    }

    [TestMethod]
    public async Task SavePreferenceAsync_CreatesNewPreference()
    {
        // Arrange
        var preference = new NotificationPreference
        {
            UserId = _testUserId,
            NotificationType = SystemNotificationType.BudgetExceeded,
            EnabledChannels = NotificationChannels.InApp | NotificationChannels.Email,
            MinimumPriority = NotificationPriority.Normal,
            IsEnabled = true
        };

        // Act
        var saved = await _preferenceService.SavePreferenceAsync(preference);

        // Assert
        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.NotificationPreferenceId > 0);
        Assert.AreEqual(_testUserId, saved.UserId);
        Assert.AreEqual(SystemNotificationType.BudgetExceeded, saved.NotificationType);
    }

    [TestMethod]
    public async Task SavePreferenceAsync_UpdatesExistingPreference()
    {
        // Arrange
        var preference = new NotificationPreference
        {
            UserId = _testUserId,
            NotificationType = SystemNotificationType.BudgetExceeded,
            EnabledChannels = NotificationChannels.InApp,
            MinimumPriority = NotificationPriority.Normal,
            IsEnabled = true
        };

        var created = await _preferenceService.SavePreferenceAsync(preference);

        // Act - Update
        created.EnabledChannels = NotificationChannels.Email | NotificationChannels.SMS;
        created.IsEnabled = false;
        var updated = await _preferenceService.SavePreferenceAsync(created);

        // Assert
        Assert.AreEqual(created.NotificationPreferenceId, updated.NotificationPreferenceId);
        Assert.AreEqual(NotificationChannels.Email | NotificationChannels.SMS, updated.EnabledChannels);
        Assert.IsFalse(updated.IsEnabled);
    }

    [TestMethod]
    public async Task GetPreferenceAsync_ReturnsSpecificPreference()
    {
        // Arrange
        var preference = new NotificationPreference
        {
            UserId = _testUserId,
            NotificationType = SystemNotificationType.LowBalance,
            EnabledChannels = NotificationChannels.InApp | NotificationChannels.SMS,
            MinimumPriority = NotificationPriority.High,
            IsEnabled = true
        };

        await _preferenceService.SavePreferenceAsync(preference);

        // Act
        var retrieved = await _preferenceService.GetPreferenceAsync(_testUserId, SystemNotificationType.LowBalance);

        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(SystemNotificationType.LowBalance, retrieved.NotificationType);
        Assert.AreEqual(NotificationChannels.InApp | NotificationChannels.SMS, retrieved.EnabledChannels);
    }

    [TestMethod]
    public async Task GetPreferenceAsync_ReturnsNullForNonExistent()
    {
        // Act
        var preference = await _preferenceService.GetPreferenceAsync(_testUserId, SystemNotificationType.BudgetExceeded);

        // Assert
        Assert.IsNull(preference);
    }

    [TestMethod]
    public async Task SaveDndScheduleAsync_CreatesNewSchedule()
    {
        // Arrange
        var schedule = new DoNotDisturbSchedule
        {
            UserId = _testUserId,
            DayOfWeek = 7,
            StartTime = "22:00",
            EndTime = "08:00",
            IsEnabled = true,
            AllowCritical = true
        };

        // Act
        var saved = await _preferenceService.SaveDndScheduleAsync(schedule);

        // Assert
        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.DoNotDisturbScheduleId > 0);
        Assert.AreEqual("22:00", saved.StartTime);
        Assert.AreEqual("08:00", saved.EndTime);
    }

    [TestMethod]
    public async Task GetDndSchedulesAsync_ReturnsUserSchedules()
    {
        // Arrange
        var schedule1 = new DoNotDisturbSchedule
        {
            UserId = _testUserId,
            DayOfWeek = 7,
            StartTime = "22:00",
            EndTime = "08:00",
            IsEnabled = true
        };

        var schedule2 = new DoNotDisturbSchedule
        {
            UserId = _testUserId,
            DayOfWeek = 6,
            StartTime = "23:00",
            EndTime = "10:00",
            IsEnabled = true
        };

        await _preferenceService.SaveDndScheduleAsync(schedule1);
        await _preferenceService.SaveDndScheduleAsync(schedule2);

        // Act
        var schedules = await _preferenceService.GetDndSchedulesAsync(_testUserId);

        // Assert
        Assert.AreEqual(2, schedules.Count);
        foreach (var s in schedules) { Assert.AreEqual(_testUserId, s.UserId); }
    }

    [TestMethod]
    public async Task DeleteDndScheduleAsync_DeletesSchedule()
    {
        // Arrange
        var schedule = new DoNotDisturbSchedule
        {
            UserId = _testUserId,
            DayOfWeek = 7,
            StartTime = "22:00",
            EndTime = "08:00",
            IsEnabled = true
        };

        var saved = await _preferenceService.SaveDndScheduleAsync(schedule);

        // Act
        await _preferenceService.DeleteDndScheduleAsync(saved.DoNotDisturbScheduleId, _testUserId);

        // Assert
        var schedules = await _preferenceService.GetDndSchedulesAsync(_testUserId);
        Assert.AreEqual(0, schedules.Count());
    }

    [TestMethod]
    public async Task SaveIntegrationAsync_CreatesNewIntegration()
    {
        // Arrange
        var integration = new NotificationIntegration
        {
            UserId = _testUserId,
            Channel = NotificationChannel.Slack,
            Configuration = "{\"webhookUrl\": \"https://hooks.slack.com/services/xxx\"}",
            IsEnabled = true
        };

        // Act
        var saved = await _preferenceService.SaveIntegrationAsync(integration);

        // Assert
        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.NotificationIntegrationId > 0);
        Assert.AreEqual(NotificationChannel.Slack, saved.Channel);
    }

    [TestMethod]
    public async Task GetIntegrationsAsync_ReturnsUserIntegrations()
    {
        // Arrange
        var slackIntegration = new NotificationIntegration
        {
            UserId = _testUserId,
            Channel = NotificationChannel.Slack,
            Configuration = "{\"webhookUrl\": \"https://hooks.slack.com/services/xxx\"}",
            IsEnabled = true
        };

        var teamsIntegration = new NotificationIntegration
        {
            UserId = _testUserId,
            Channel = NotificationChannel.Teams,
            Configuration = "{\"webhookUrl\": \"https://outlook.office.com/webhook/xxx\"}",
            IsEnabled = true
        };

        await _preferenceService.SaveIntegrationAsync(slackIntegration);
        await _preferenceService.SaveIntegrationAsync(teamsIntegration);

        // Act
        var integrations = await _preferenceService.GetIntegrationsAsync(_testUserId);

        // Assert
        Assert.AreEqual(2, integrations.Count);
        Assert.IsTrue(integrations.Any(i => i.Channel == NotificationChannel.Slack));
        Assert.IsTrue(integrations.Any(i => i.Channel == NotificationChannel.Teams));
    }

    [TestMethod]
    public async Task DeleteIntegrationAsync_DeletesIntegration()
    {
        // Arrange
        var integration = new NotificationIntegration
        {
            UserId = _testUserId,
            Channel = NotificationChannel.Slack,
            Configuration = "{\"webhookUrl\": \"https://hooks.slack.com/services/xxx\"}",
            IsEnabled = true
        };

        var saved = await _preferenceService.SaveIntegrationAsync(integration);

        // Act
        await _preferenceService.DeleteIntegrationAsync(saved.NotificationIntegrationId, _testUserId);

        // Assert
        var integrations = await _preferenceService.GetIntegrationsAsync(_testUserId);
        Assert.AreEqual(0, integrations.Count());
    }

    [TestMethod]
    public async Task InitializeDefaultPreferencesAsync_CreatesDefaultPreferences()
    {
        // Act
        await _preferenceService.InitializeDefaultPreferencesAsync(_testUserId);

        // Assert
        var preferences = await _preferenceService.GetUserPreferencesAsync(_testUserId);
        var dndSchedules = await _preferenceService.GetDndSchedulesAsync(_testUserId);

        Assert.AreNotEqual(0, preferences.Count());
        Assert.AreEqual(1, dndSchedules.Count());
        
        // Check that critical notifications have email enabled
        var lowBalancePref = preferences.FirstOrDefault(p => p.NotificationType == SystemNotificationType.LowBalance);
        Assert.IsNotNull(lowBalancePref);
        Assert.IsTrue(lowBalancePref.EnabledChannels.HasFlag(NotificationChannels.Email));
    }

    [TestMethod]
    public async Task InitializeDefaultPreferencesAsync_DoesNotDuplicatePreferences()
    {
        // Act - Initialize twice
        await _preferenceService.InitializeDefaultPreferencesAsync(_testUserId);
        await _preferenceService.InitializeDefaultPreferencesAsync(_testUserId);

        // Assert
        var preferences = await _preferenceService.GetUserPreferencesAsync(_testUserId);
        
        // Should not have duplicates - group by notification type and check count
        var grouped = preferences.GroupBy(p => p.NotificationType);
        foreach (var g in grouped)
        {
            Assert.AreEqual(1, g.Count());
        }
    }

    [TestMethod]
    public async Task SavePreferenceAsync_WithDigestMode_SavesDigestSettings()
    {
        // Arrange
        var preference = new NotificationPreference
        {
            UserId = _testUserId,
            NotificationType = SystemNotificationType.GoalMilestone,
            EnabledChannels = NotificationChannels.InApp,
            MinimumPriority = NotificationPriority.Low,
            IsEnabled = true,
            DigestMode = true,
            DigestIntervalHours = 24
        };

        // Act
        var saved = await _preferenceService.SavePreferenceAsync(preference);

        // Assert
        Assert.IsTrue(saved.DigestMode);
        Assert.AreEqual(24, saved.DigestIntervalHours);
    }
}
