using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

/// <summary>
/// Integration test to verify the complete goal milestone workflow
/// </summary>
[TestClass]
public class GoalMilestoneIntegrationTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly GoalService _goalService;
    private readonly GoalMilestoneService _milestoneService;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public GoalMilestoneIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _notificationServiceMock = new Mock<INotificationService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);

        _milestoneService = new GoalMilestoneService(_context, _notificationServiceMock.Object, _currentUserServiceMock.Object);
        _goalService = new GoalService(_context, _currentUserServiceMock.Object, _milestoneService);
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
    public async Task CompleteWorkflow_CreateGoalAndTrackProgress_MilestonesAreCreatedAndReached()
    {
        // Arrange - Create a new goal
        var goal = new Goal
        {
            Name = "Nödfond",
            Description = "Spara för nödsituationer",
            TargetAmount = 50000m,
            CurrentAmount = 0m,
            Priority = 1,
            TargetDate = DateTime.UtcNow.AddMonths(12),
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            UserId = "test-user-id"
        };

        // Act - Create goal (should automatically create milestones)
        var createdGoal = await _goalService.CreateGoalAsync(goal);

        // Assert - Verify milestones were created
        var milestones = await _milestoneService.GetMilestonesByGoalIdAsync(createdGoal.GoalId);
        var milestoneList = milestones.ToList();
        
        Assert.AreEqual(4, milestoneList.Count);
        foreach (var m in milestoneList) { Assert.IsTrue(m.IsAutomatic); }
        foreach (var m in milestoneList) { Assert.IsFalse(m.IsReached); }

        // Act - Update progress to 30% (should reach 25% milestone)
        await _goalService.UpdateGoalProgressAsync(createdGoal.GoalId, 15000m);

        // Assert - Verify 25% milestone is reached
        var updatedMilestones = await _milestoneService.GetMilestonesByGoalIdAsync(createdGoal.GoalId);
        var milestone25 = updatedMilestones.First(m => m.Percentage == 25);
        Assert.IsTrue(milestone25.IsReached);
        Assert.IsNotNull(milestone25.ReachedAt);

        // Verify notification was sent
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<string>(),
                SystemNotificationType.GoalMilestone,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);

        // Act - Update progress to 60% (should reach 50% milestone)
        await _goalService.UpdateGoalProgressAsync(createdGoal.GoalId, 30000m);

        // Assert - Verify 25% and 50% milestones are reached
        var finalMilestones = await _milestoneService.GetMilestonesByGoalIdAsync(createdGoal.GoalId);
        var reachedMilestones = finalMilestones.Where(m => m.IsReached).ToList();
        
        Assert.AreEqual(2, reachedMilestones.Count);
        Assert.IsTrue(reachedMilestones.Any(m => m.Percentage == 25));
        Assert.IsTrue(reachedMilestones.Any(m => m.Percentage == 50));

        // Verify 2 notifications were sent (one for each reached milestone)
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<string>(),
                SystemNotificationType.GoalMilestone,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Exactly(2));

        // Act - Get milestone history
        var history = await _milestoneService.GetReachedMilestonesAsync(createdGoal.GoalId);
        
        // Assert - Verify history contains reached milestones
        Assert.AreEqual(2, history.Count());
        foreach (var m in history) { Assert.IsTrue(m.IsReached); }
        foreach (var m in history) { Assert.IsNotNull(m.ReachedAt); }
    }

    [TestMethod]
    public async Task CompleteWorkflow_WithCustomMilestone_BothAutomaticAndCustomWork()
    {
        // Arrange
        var goal = new Goal
        {
            Name = "Resa till Japan",
            TargetAmount = 40000m,
            CurrentAmount = 5000m,
            Priority = 2,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            UserId = "test-user-id"
        };

        // Act - Create goal
        var createdGoal = await _goalService.CreateGoalAsync(goal);

        // Add a custom milestone at 35%
        var customMilestone = new GoalMilestone
        {
            GoalId = createdGoal.GoalId,
            TargetAmount = 14000m,
            Percentage = 35,
            Description = "Köpt flygbiljetter!"
        };
        await _milestoneService.CreateCustomMilestoneAsync(customMilestone);

        // Assert - Verify we have both automatic and custom milestones
        var allMilestones = await _milestoneService.GetMilestonesByGoalIdAsync(createdGoal.GoalId);
        Assert.AreEqual(5, allMilestones.Count()); // 4 automatic + 1 custom
        Assert.AreEqual(1, allMilestones.Count(m => !m.IsAutomatic));

        // Act - Update progress to reach 25% and the custom milestone
        await _goalService.UpdateGoalProgressAsync(createdGoal.GoalId, 15000m);

        // Assert - Verify both 25% and custom 35% milestones are reached
        var reachedMilestones = await _milestoneService.GetReachedMilestonesAsync(createdGoal.GoalId);
        Assert.AreEqual(2, reachedMilestones.Count());
        Assert.IsTrue(reachedMilestones.Any(m => m.Percentage == 25 && m.IsAutomatic));
        Assert.IsTrue(reachedMilestones.Any(m => m.Percentage == 35 && !m.IsAutomatic));
    }

    [TestMethod]
    public async Task CompleteWorkflow_GoalCompletion_AllMilestonesReached()
    {
        // Arrange
        var goal = new Goal
        {
            Name = "Bil kontantinsats",
            TargetAmount = 100000m,
            CurrentAmount = 0m,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow,
            UserId = "test-user-id"
        };

        // Act - Create goal and complete it immediately
        var createdGoal = await _goalService.CreateGoalAsync(goal);
        await _goalService.UpdateGoalProgressAsync(createdGoal.GoalId, 100000m);

        // Assert - All milestones should be reached
        var milestones = await _milestoneService.GetMilestonesByGoalIdAsync(createdGoal.GoalId);
        foreach (var m in milestones) { Assert.IsTrue(m.IsReached); }
        foreach (var m in milestones) { Assert.IsNotNull(m.ReachedAt); }

        // Verify 4 notifications were sent (one for each milestone)
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<string>(),
                SystemNotificationType.GoalMilestone,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Exactly(4));

        // Verify the 100% milestone notification mentions completion
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                "test-user-id",
                SystemNotificationType.GoalMilestone,
                It.Is<string>(s => s.Contains("100%")),
                It.Is<string>(s => s.Contains("Bil kontantinsats")),
                NotificationPriority.Normal,
                null,
                It.IsAny<string>()),
            Times.Once);
    }
}
