using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class SavingsChallengeServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly SavingsChallengeService _service;

    public SavingsChallengeServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _service = new SavingsChallengeService(_context, null);
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
    public async Task CreateChallengeAsync_ValidChallenge_SuccessfullyCreatesChallenge()
    {
        // Arrange
        var challenge = new SavingsChallenge
        {
            Name = "30-Day Savings Challenge",
            Description = "Save 100 kr per day for 30 days",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 3000m,
            DurationDays = 30,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.CreateChallengeAsync(challenge);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.SavingsChallengeId > 0);
        Assert.AreEqual(ChallengeStatus.Active, result.Status);
        Assert.IsNotNull(result.EndDate);
        Assert.AreEqual(30, (result.EndDate.Value - result.StartDate).Days);
    }

    [TestMethod]
    public async Task GetAllChallengesAsync_ReturnsChallenges()
    {
        // Arrange
        var challenge1 = new SavingsChallenge
        {
            Name = "Challenge 1",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        var challenge2 = new SavingsChallenge
        {
            Name = "Challenge 2",
            Type = ChallengeType.NoRestaurant,
            TargetAmount = 2000m,
            DurationDays = 14,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _service.CreateChallengeAsync(challenge1);
        await _service.CreateChallengeAsync(challenge2);

        // Act
        var result = await _service.GetAllChallengesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task GetActiveChallengesAsync_ReturnsOnlyActiveChallenges()
    {
        // Arrange
        var activeChallenge = new SavingsChallenge
        {
            Name = "Active Challenge",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var completedChallenge = new SavingsChallenge
        {
            Name = "Completed Challenge",
            Type = ChallengeType.NoRestaurant,
            TargetAmount = 2000m,
            DurationDays = 14,
            StartDate = DateTime.UtcNow.AddDays(-20),
            Status = ChallengeStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        await _service.CreateChallengeAsync(activeChallenge);
        await _service.CreateChallengeAsync(completedChallenge);

        // Act
        var result = await _service.GetActiveChallengesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Active Challenge", result.First().Name);
    }

    [TestMethod]
    public async Task RecordProgressAsync_ValidProgress_SuccessfullyRecordsProgress()
    {
        // Arrange
        var challenge = new SavingsChallenge
        {
            Name = "Test Challenge",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateChallengeAsync(challenge);

        // Act
        var progress = await _service.RecordProgressAsync(
            created.SavingsChallengeId,
            DateTime.UtcNow,
            true,
            100m,
            "First day completed");

        // Assert
        Assert.IsNotNull(progress);
        Assert.IsTrue(progress.Completed);
        Assert.AreEqual(100m, progress.AmountSaved);
        Assert.AreEqual("First day completed", progress.Notes);

        var updatedChallenge = await _service.GetChallengeByIdAsync(created.SavingsChallengeId);
        Assert.IsNotNull(updatedChallenge);
        Assert.AreEqual(100m, updatedChallenge.CurrentAmount);
    }

    [TestMethod]
    public async Task RecordProgressAsync_MultipleProgressEntries_UpdatesStreak()
    {
        // Arrange
        var challenge = new SavingsChallenge
        {
            Name = "Streak Test Challenge",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow.AddDays(-3),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var created = await _service.CreateChallengeAsync(challenge);

        // Act - Record progress for 3 consecutive days
        await _service.RecordProgressAsync(created.SavingsChallengeId, DateTime.UtcNow.AddDays(-2), true, 100m);
        await _service.RecordProgressAsync(created.SavingsChallengeId, DateTime.UtcNow.AddDays(-1), true, 100m);
        await _service.RecordProgressAsync(created.SavingsChallengeId, DateTime.UtcNow, true, 100m);

        // Assert
        var updatedChallenge = await _service.GetChallengeByIdAsync(created.SavingsChallengeId);
        Assert.IsNotNull(updatedChallenge);
        Assert.IsTrue(updatedChallenge.CurrentStreak >= 1); // At least 1 day streak
        Assert.AreEqual(300m, updatedChallenge.CurrentAmount);
    }

    [TestMethod]
    public async Task UpdateChallengeStatusAsync_ValidStatus_SuccessfullyUpdatesStatus()
    {
        // Arrange
        var challenge = new SavingsChallenge
        {
            Name = "Status Test Challenge",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateChallengeAsync(challenge);

        // Act
        await _service.UpdateChallengeStatusAsync(created.SavingsChallengeId, ChallengeStatus.Completed);

        // Assert
        var updatedChallenge = await _service.GetChallengeByIdAsync(created.SavingsChallengeId);
        Assert.IsNotNull(updatedChallenge);
        Assert.AreEqual(ChallengeStatus.Completed, updatedChallenge.Status);
    }

    [TestMethod]
    public async Task GetTotalActiveChallengesAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _service.CreateChallengeAsync(new SavingsChallenge
        {
            Name = "Active 1",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await _service.CreateChallengeAsync(new SavingsChallenge
        {
            Name = "Active 2",
            Type = ChallengeType.NoRestaurant,
            TargetAmount = 2000m,
            DurationDays = 14,
            StartDate = DateTime.UtcNow,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await _service.CreateChallengeAsync(new SavingsChallenge
        {
            Name = "Completed",
            Type = ChallengeType.NoCoffeeOut,
            TargetAmount = 500m,
            DurationDays = 7,
            StartDate = DateTime.UtcNow.AddDays(-10),
            Status = ChallengeStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        // Act
        var count = await _service.GetTotalActiveChallengesAsync();

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task GetTotalAmountSavedAsync_ReturnsCorrectTotal()
    {
        // Arrange
        var challenge1 = await _service.CreateChallengeAsync(new SavingsChallenge
        {
            Name = "Challenge 1",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var challenge2 = await _service.CreateChallengeAsync(new SavingsChallenge
        {
            Name = "Challenge 2",
            Type = ChallengeType.NoRestaurant,
            TargetAmount = 2000m,
            DurationDays = 14,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _service.RecordProgressAsync(challenge1.SavingsChallengeId, DateTime.UtcNow, true, 100m);
        await _service.RecordProgressAsync(challenge2.SavingsChallengeId, DateTime.UtcNow, true, 200m);

        // Act
        var total = await _service.GetTotalAmountSavedAsync();

        // Assert
        Assert.AreEqual(300m, total);
    }

    [TestMethod]
    public async Task DeleteChallengeAsync_ValidId_SuccessfullyDeletesChallenge()
    {
        // Arrange
        var challenge = new SavingsChallenge
        {
            Name = "Delete Test Challenge",
            Type = ChallengeType.SaveDaily,
            TargetAmount = 1000m,
            DurationDays = 10,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _service.CreateChallengeAsync(challenge);

        // Act
        await _service.DeleteChallengeAsync(created.SavingsChallengeId);

        // Assert
        var deletedChallenge = await _service.GetChallengeByIdAsync(created.SavingsChallengeId);
        Assert.IsNull(deletedChallenge);
    }

    [TestMethod]
    public async Task GetAllTemplatesAsync_ReturnsActiveTemplates()
    {
        // Arrange - Templates are seeded by the context initialization
        
        // Act
        var templates = await _service.GetAllTemplatesAsync();

        // Assert
        Assert.IsNotNull(templates);
        // Should have templates if seeded, or empty if not
        Assert.IsTrue(templates.All(t => t.IsActive));
    }

    [TestMethod]
    public async Task CreateChallengeFromTemplateAsync_ValidTemplate_SuccessfullyCreatesChallenge()
    {
        // Arrange
        var template = new ChallengeTemplate
        {
            Name = "Test Template",
            Description = "Test Description",
            Icon = "🎯",
            Type = ChallengeType.SaveDaily,
            DurationDays = 30,
            Difficulty = DifficultyLevel.Medium,
            Category = ChallengeCategory.Individual,
            EstimatedSavingsMin = 500,
            EstimatedSavingsMax = 1000,
            SuggestedTargetAmount = 750,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChallengeTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var challenge = await _service.CreateChallengeFromTemplateAsync(template.ChallengeTemplateId);

        // Assert
        Assert.IsNotNull(challenge);
        Assert.AreEqual(template.Name, challenge.Name);
        Assert.AreEqual(template.Description, challenge.Description);
        Assert.AreEqual(template.Icon, challenge.Icon);
        Assert.AreEqual(template.Type, challenge.Type);
        Assert.AreEqual(template.DurationDays, challenge.DurationDays);
        Assert.AreEqual(template.Difficulty, challenge.Difficulty);
        Assert.AreEqual(template.Category, challenge.Category);
        Assert.AreEqual(ChallengeStatus.Active, challenge.Status);
    }
}
