using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class ReminderServiceTests : IDisposable
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<ReminderService>> _mockLogger;
    private readonly PrivatekonomyContext _context;
    private readonly ReminderService _service;
    private readonly string _testUserId = "test-user-123";

    public ReminderServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<ReminderService>>();
        _service = new ReminderService(_context, _mockNotificationService.Object, _mockLogger.Object);
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
    public async Task CreateReminderAsync_ShouldCreateReminder()
    {
        // Arrange
        var reminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Test Reminder",
            Description = "Test Description",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Priority = ReminderPriority.Normal
        };

        // Act
        var result = await _service.CreateReminderAsync(reminder);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ReminderId > 0);
        Assert.AreEqual(ReminderStatus.Active, result.Status);
        Assert.AreEqual(_testUserId, result.UserId);
        Assert.AreEqual("Test Reminder", result.Title);
    }

    [TestMethod]
    public async Task GetUserRemindersAsync_ShouldReturnUserReminders()
    {
        // Arrange
        var reminder1 = new Reminder
        {
            UserId = _testUserId,
            Title = "Reminder 1",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };
        
        var reminder2 = new Reminder
        {
            UserId = _testUserId,
            Title = "Reminder 2",
            ReminderDate = DateTime.UtcNow.AddDays(2),
            Status = ReminderStatus.Active
        };
        
        var reminder3 = new Reminder
        {
            UserId = "other-user",
            Title = "Other User Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };

        await _service.CreateReminderAsync(reminder1);
        await _service.CreateReminderAsync(reminder2);
        await _service.CreateReminderAsync(reminder3);

        // Act
        var result = await _service.GetUserRemindersAsync(_testUserId);

        // Assert
        Assert.AreEqual(2, result.Count);
        foreach (var r in result) { Assert.AreEqual(_testUserId, r.UserId); }
    }

    [TestMethod]
    public async Task SnoozeReminderAsync_ShouldSnoozeReminder()
    {
        // Arrange
        var reminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Test Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };
        var created = await _service.CreateReminderAsync(reminder);

        // Act
        await _service.SnoozeReminderAsync(created.ReminderId, _testUserId, 60); // 60 minutes

        // Assert
        var result = await _service.GetReminderByIdAsync(created.ReminderId, _testUserId);
        Assert.IsNotNull(result);
        Assert.AreEqual(ReminderStatus.Snoozed, result.Status);
        Assert.IsNotNull(result.SnoozeUntil);
        Assert.AreEqual(1, result.SnoozeCount);
    }

    [TestMethod]
    public async Task MarkAsCompletedAsync_ShouldCompleteReminder()
    {
        // Arrange
        var reminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Test Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };
        var created = await _service.CreateReminderAsync(reminder);

        // Act
        await _service.MarkAsCompletedAsync(created.ReminderId, _testUserId);

        // Assert
        var result = await _service.GetReminderByIdAsync(created.ReminderId, _testUserId);
        Assert.IsNotNull(result);
        Assert.AreEqual(ReminderStatus.Completed, result.Status);
        Assert.IsNotNull(result.CompletedDate);
        
        // Verify notification was sent
        _mockNotificationService.Verify(
            x => x.SendNotificationAsync(
                _testUserId,
                SystemNotificationType.GoalAchieved,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationPriority.Low,
                null,
                null),
            Times.Once);
    }

    [TestMethod]
    public async Task GetActiveRemindersAsync_ShouldReturnOnlyActiveAndNonSnoozedReminders()
    {
        // Arrange
        var activeReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Active Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        var snoozedReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Snoozed Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Snoozed,
            SnoozeUntil = DateTime.UtcNow.AddDays(2), // Still snoozed
            CreatedAt = DateTime.UtcNow
        };
        
        var completedReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Completed Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        // Add directly to context to preserve status
        _context.Reminders.AddRange(activeReminder, snoozedReminder, completedReminder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveRemindersAsync(_testUserId);

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Active Reminder", result[0].Title);
    }

    [TestMethod]
    public async Task GetDueRemindersAsync_ShouldReturnOnlyDueReminders()
    {
        // Arrange
        var dueReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Due Reminder",
            ReminderDate = DateTime.UtcNow.AddMinutes(-10), // Past due
            Status = ReminderStatus.Active
        };
        
        var futureReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Future Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };

        await _service.CreateReminderAsync(dueReminder);
        await _service.CreateReminderAsync(futureReminder);

        // Act
        var result = await _service.GetDueRemindersAsync(_testUserId);

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Due Reminder", result[0].Title);
    }

    [TestMethod]
    public async Task ShouldEscalateReminderAsync_ShouldReturnTrueWhenThresholdExceeded()
    {
        // Arrange
        var settings = new ReminderSettings
        {
            UserId = _testUserId,
            EnableEscalation = true,
            SnoozeThresholdForEscalation = 3
        };
        _context.ReminderSettings.Add(settings);
        await _context.SaveChangesAsync();

        var reminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Test Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Snoozed,
            SnoozeCount = 3
        };
        await _service.CreateReminderAsync(reminder);

        // Act
        var result = await _service.ShouldEscalateReminderAsync(reminder.ReminderId);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetUserSettingsAsync_ShouldCreateDefaultSettingsIfNotExist()
    {
        // Act
        var result = await _service.GetUserSettingsAsync(_testUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_testUserId, result.UserId);
        Assert.IsTrue(result.EnableEscalation);
        Assert.AreEqual(3, result.SnoozeThresholdForEscalation);
    }

    [TestMethod]
    public async Task GetStatisticsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var activeReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Active",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        var snoozedReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Snoozed",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Snoozed,
            CreatedAt = DateTime.UtcNow
        };
        
        var completedReminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Completed",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        // Add directly to context to preserve status
        _context.Reminders.AddRange(activeReminder, snoozedReminder, completedReminder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStatisticsAsync(_testUserId);

        // Assert
        Assert.AreEqual(1, result.TotalActive);
        Assert.AreEqual(1, result.TotalSnoozed);
        Assert.AreEqual(1, result.TotalCompleted);
    }

    [TestMethod]
    public async Task DeleteReminderAsync_ShouldDeleteReminder()
    {
        // Arrange
        var reminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Test Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };
        var created = await _service.CreateReminderAsync(reminder);

        // Act
        await _service.DeleteReminderAsync(created.ReminderId, _testUserId);

        // Assert
        var result = await _service.GetReminderByIdAsync(created.ReminderId, _testUserId);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task DismissReminderAsync_ShouldDismissReminder()
    {
        // Arrange
        var reminder = new Reminder
        {
            UserId = _testUserId,
            Title = "Test Reminder",
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Status = ReminderStatus.Active
        };
        var created = await _service.CreateReminderAsync(reminder);

        // Act
        await _service.DismissReminderAsync(created.ReminderId, _testUserId);

        // Assert
        var result = await _service.GetReminderByIdAsync(created.ReminderId, _testUserId);
        Assert.IsNotNull(result);
        Assert.AreEqual(ReminderStatus.Dismissed, result.Status);
    }
}
