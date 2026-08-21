using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class HouseholdServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly HouseholdService _householdService;

    public HouseholdServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _householdService = new HouseholdService(_context);
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

    #region Household CRUD Tests

    [TestMethod]
    public async Task UpdateHouseholdAsync_UpdatesHouseholdSuccessfully()
    {
        // Arrange
        var household = new Household 
        { 
            Name = "Original Name",
            Description = "Original Description"
        };
        var created = await _householdService.CreateHouseholdAsync(household);

        // Act
        created.Name = "Updated Name";
        created.Description = "Updated Description";
        var updated = await _householdService.UpdateHouseholdAsync(created);

        // Assert
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Name", updated.Name);
        Assert.AreEqual("Updated Description", updated.Description);
        Assert.AreEqual(created.HouseholdId, updated.HouseholdId);

        // Verify the update persisted
        var retrieved = await _householdService.GetHouseholdByIdAsync(created.HouseholdId);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Updated Name", retrieved.Name);
        Assert.AreEqual("Updated Description", retrieved.Description);
    }

    #endregion

    #region Activity Tests

    [TestMethod]
    public async Task CreateActivityAsync_CreatesActivitySuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Cleaned the kitchen",
            Description = "Deep cleaning",
            Type = HouseholdActivityType.Cleaning
        };

        // Act
        var result = await _householdService.CreateActivityAsync(activity);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Cleaned the kitchen", result.Title);
        Assert.AreEqual(HouseholdActivityType.Cleaning, result.Type);
        Assert.AreNotEqual(default, result.CreatedDate);
        Assert.AreNotEqual(default, result.CompletedDate);
    }

    [TestMethod]
    public async Task GetActivitiesAsync_ReturnsActivitiesOrderedByDate()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity1 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Activity 1",
            CompletedDate = DateTime.Now.AddDays(-2)
        };
        var activity2 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Activity 2",
            CompletedDate = DateTime.Now.AddDays(-1)
        };

        await _householdService.CreateActivityAsync(activity1);
        await _householdService.CreateActivityAsync(activity2);

        // Act
        var result = await _householdService.GetActivitiesAsync(household.HouseholdId);

        // Assert
        var activities = result.ToList();
        Assert.AreEqual(2, activities.Count);
        Assert.AreEqual("Activity 2", activities[0].Title); // Most recent first
        Assert.AreEqual("Activity 1", activities[1].Title);
    }

    [TestMethod]
    public async Task GetActivitiesAsync_FiltersWithDateRange()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity1 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Old Activity",
            CompletedDate = DateTime.Now.AddMonths(-2)
        };
        var activity2 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Recent Activity",
            CompletedDate = DateTime.Now
        };

        await _householdService.CreateActivityAsync(activity1);
        await _householdService.CreateActivityAsync(activity2);

        // Act
        var result = await _householdService.GetActivitiesAsync(
            household.HouseholdId,
            startDate: DateTime.Now.AddDays(-1)
        );

        // Assert
        var activities = result.ToList();
        Assert.AreEqual(1, activities.Count());
        Assert.AreEqual("Recent Activity", activities[0].Title);
    }

    [TestMethod]
    public async Task DeleteActivityAsync_RemovesActivity()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity"
        };
        var created = await _householdService.CreateActivityAsync(activity);

        // Act
        var result = await _householdService.DeleteActivityAsync(created.HouseholdActivityId);

        // Assert
        Assert.IsTrue(result);
        var activities = await _householdService.GetActivitiesAsync(household.HouseholdId);
        Assert.AreEqual(0, activities.Count());
    }

    #endregion

    #region Task Tests

    [TestMethod]
    public async Task CreateTaskAsync_CreatesTaskSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Buy groceries",
            Description = "Milk, eggs, bread",
            Priority = HouseholdTaskPriority.High,
            DueDate = DateTime.Now.AddDays(1)
        };

        // Act
        var result = await _householdService.CreateTaskAsync(task);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Buy groceries", result.Title);
        Assert.AreEqual(HouseholdTaskPriority.High, result.Priority);
        Assert.IsFalse(result.IsCompleted);
        Assert.IsNull(result.CompletedDate);
        Assert.AreNotEqual(default, result.CreatedDate);
    }

    [TestMethod]
    public async Task GetTasksAsync_ReturnsTasksOrderedByPriority()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Low priority task",
            Priority = HouseholdTaskPriority.Low
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "High priority task",
            Priority = HouseholdTaskPriority.High
        };

        await _householdService.CreateTaskAsync(task1);
        await _householdService.CreateTaskAsync(task2);

        // Act
        var result = await _householdService.GetTasksAsync(household.HouseholdId);

        // Assert
        var tasks = result.ToList();
        Assert.AreEqual(2, tasks.Count);
        Assert.AreEqual("High priority task", tasks[0].Title); // Higher priority first
    }

    [TestMethod]
    public async Task GetTasksAsync_ExcludesCompletedTasksWhenRequested()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Incomplete task"
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Completed task"
        };

        await _householdService.CreateTaskAsync(task1);
        var created = await _householdService.CreateTaskAsync(task2);
        await _householdService.MarkTaskCompleteAsync(created.HouseholdTaskId);

        // Act
        var result = await _householdService.GetTasksAsync(household.HouseholdId, includeCompleted: false);

        // Assert
        var tasks = result.ToList();
        Assert.AreEqual(1, tasks.Count());
        Assert.AreEqual("Incomplete task", tasks[0].Title);
    }

    [TestMethod]
    public async Task MarkTaskCompleteAsync_MarksTaskAsCompleted()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        var member = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "John Doe"
        };
        _context.HouseholdMembers.Add(member);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task"
        };
        var created = await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.MarkTaskCompleteAsync(
            created.HouseholdTaskId,
            member.HouseholdMemberId
        );

        // Assert
        Assert.IsTrue(result);
        var updated = await _context.HouseholdTasks.FindAsync(created.HouseholdTaskId);
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.IsCompleted);
        Assert.IsNotNull(updated.CompletedDate);
        Assert.AreEqual(member.HouseholdMemberId, updated.CompletedByMemberId);
    }

    [TestMethod]
    public async Task MarkTaskIncompleteAsync_MarksTaskAsIncomplete()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task"
        };
        var created = await _householdService.CreateTaskAsync(task);
        await _householdService.MarkTaskCompleteAsync(created.HouseholdTaskId);

        // Act
        var result = await _householdService.MarkTaskIncompleteAsync(created.HouseholdTaskId);

        // Assert
        Assert.IsTrue(result);
        var updated = await _context.HouseholdTasks.FindAsync(created.HouseholdTaskId);
        Assert.IsNotNull(updated);
        Assert.IsFalse(updated.IsCompleted);
        Assert.IsNull(updated.CompletedDate);
        Assert.IsNull(updated.CompletedByMemberId);
    }

    [TestMethod]
    public async Task SearchTasksAsync_FindsTasksByTitle()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Buy groceries"
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Clean house"
        };

        await _householdService.CreateTaskAsync(task1);
        await _householdService.CreateTaskAsync(task2);

        // Act
        var result = await _householdService.SearchTasksAsync(household.HouseholdId, "groceries");

        // Assert
        var tasks = result.ToList();
        Assert.AreEqual(1, tasks.Count());
        Assert.AreEqual("Buy groceries", tasks[0].Title);
    }

    [TestMethod]
    public async Task SearchTasksAsync_FindsTasksByDescription()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Shopping",
            Description = "Buy milk and eggs"
        };

        await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.SearchTasksAsync(household.HouseholdId, "milk");

        // Assert
        var tasks = result.ToList();
        Assert.AreEqual(1, tasks.Count());
        Assert.AreEqual("Shopping", tasks[0].Title);
    }

    [TestMethod]
    public async Task DeleteTaskAsync_RemovesTask()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task"
        };
        var created = await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.DeleteTaskAsync(created.HouseholdTaskId);

        // Assert
        Assert.IsTrue(result);
        var tasks = await _householdService.GetTasksAsync(household.HouseholdId);
        Assert.AreEqual(0, tasks.Count());
    }

    #endregion
    
    #region Activity Image Tests

    [TestMethod]
    public async Task AddActivityImageAsync_AddsImageSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity",
            Type = HouseholdActivityType.General
        };
        var createdActivity = await _householdService.CreateActivityAsync(activity);

        // Act
        var image = await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId,
            "test-image.jpg",
            "image/jpeg",
            1024000,
            "Test caption"
        );

        // Assert
        Assert.IsNotNull(image);
        Assert.AreEqual("test-image.jpg", image.ImagePath);
        Assert.AreEqual("image/jpeg", image.MimeType);
        Assert.AreEqual(1024000, image.FileSize);
        Assert.AreEqual("Test caption", image.Caption);
        Assert.AreEqual(1, image.DisplayOrder);
    }

    [TestMethod]
    public async Task AddActivityImageAsync_SetsCorrectDisplayOrder()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity",
            Type = HouseholdActivityType.General
        };
        var createdActivity = await _householdService.CreateActivityAsync(activity);

        // Act
        var image1 = await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId,
            "image1.jpg",
            "image/jpeg",
            1024000
        );
        var image2 = await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId,
            "image2.jpg",
            "image/jpeg",
            1024000
        );
        var image3 = await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId,
            "image3.jpg",
            "image/jpeg",
            1024000
        );

        // Assert
        Assert.AreEqual(1, image1.DisplayOrder);
        Assert.AreEqual(2, image2.DisplayOrder);
        Assert.AreEqual(3, image3.DisplayOrder);
    }

    [TestMethod]
    public async Task GetActivityImagesAsync_ReturnsImagesInOrder()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity",
            Type = HouseholdActivityType.General
        };
        var createdActivity = await _householdService.CreateActivityAsync(activity);

        await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId, "image3.jpg", "image/jpeg", 1024000
        );
        await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId, "image1.jpg", "image/jpeg", 1024000
        );
        await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId, "image2.jpg", "image/jpeg", 1024000
        );

        // Act
        var images = await _householdService.GetActivityImagesAsync(createdActivity.HouseholdActivityId);

        // Assert
        var imageList = images.ToList();
        Assert.AreEqual(3, imageList.Count);
        Assert.AreEqual("image3.jpg", imageList[0].ImagePath);
        Assert.AreEqual("image1.jpg", imageList[1].ImagePath);
        Assert.AreEqual("image2.jpg", imageList[2].ImagePath);
    }

    [TestMethod]
    public async Task DeleteActivityImageAsync_DeletesImageSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity",
            Type = HouseholdActivityType.General
        };
        var createdActivity = await _householdService.CreateActivityAsync(activity);
        var image = await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId,
            "test-image.jpg",
            "image/jpeg",
            1024000
        );

        // Act
        var result = await _householdService.DeleteActivityImageAsync(image.HouseholdActivityImageId);

        // Assert
        Assert.IsTrue(result);
        var images = await _householdService.GetActivityImagesAsync(createdActivity.HouseholdActivityId);
        Assert.AreEqual(0, images.Count());
    }

    [TestMethod]
    public async Task UpdateImageOrderAsync_UpdatesOrderSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity",
            Type = HouseholdActivityType.General
        };
        var createdActivity = await _householdService.CreateActivityAsync(activity);
        var image = await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId,
            "test-image.jpg",
            "image/jpeg",
            1024000
        );

        // Act
        var result = await _householdService.UpdateImageOrderAsync(image.HouseholdActivityImageId, 5);

        // Assert
        Assert.IsTrue(result);
        var updatedImage = await _context.HouseholdActivityImages.FindAsync(image.HouseholdActivityImageId);
        Assert.IsNotNull(updatedImage);
        Assert.AreEqual(5, updatedImage.DisplayOrder);
    }

    [TestMethod]
    public async Task GetActivitiesAsync_IncludesImages()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity",
            Type = HouseholdActivityType.General
        };
        var createdActivity = await _householdService.CreateActivityAsync(activity);
        
        await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId, "image1.jpg", "image/jpeg", 1024000
        );
        await _householdService.AddActivityImageAsync(
            createdActivity.HouseholdActivityId, "image2.jpg", "image/jpeg", 1024000
        );

        // Act
        var activities = await _householdService.GetActivitiesAsync(household.HouseholdId);

        // Assert
        var activityList = activities.ToList();
        Assert.AreEqual(1, activityList.Count());
        Assert.AreEqual(2, activityList[0].Images.Count);
    }

    #endregion

    #region Shared Budget Tests

    [TestMethod]
    public async Task CreateSharedBudgetAsync_CreatesSharedBudgetSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        
        var member1 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 1",
            IsActive = true
        };
        var member2 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 2",
            IsActive = true
        };
        _context.HouseholdMembers.Add(member1);
        _context.HouseholdMembers.Add(member2);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            Description = "Test shared budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 60m },
            { member2.HouseholdMemberId, 40m }
        };

        // Act
        var result = await _householdService.CreateSharedBudgetAsync(budget, contributions);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Budget", result.Name);
        Assert.AreEqual(household.HouseholdId, result.HouseholdId);
        Assert.AreEqual("test-user-id", result.UserId);
        
        // Verify budget shares were created
        var shares = await _context.HouseholdBudgetShares
            .Where(s => s.BudgetId == result.BudgetId)
            .ToListAsync();
        Assert.AreEqual(2, shares.Count);
        Assert.IsTrue(shares.Any(s => s.HouseholdMemberId == member1.HouseholdMemberId && s.SharePercentage == 60m));
        Assert.IsTrue(shares.Any(s => s.HouseholdMemberId == member2.HouseholdMemberId && s.SharePercentage == 40m));
    }

    [TestMethod]
    public async Task CreateSharedBudgetAsync_ThrowsWhenContributionsDontSumTo100()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        
        var member1 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 1",
            IsActive = true
        };
        var member2 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 2",
            IsActive = true
        };
        _context.HouseholdMembers.Add(member1);
        _context.HouseholdMembers.Add(member2);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 60m },
            { member2.HouseholdMemberId, 30m } // Total is 90%, not 100%
        };

        // Act & Assert
        InvalidOperationException? exception = null;
        try
        {
            await _householdService.CreateSharedBudgetAsync(budget, contributions);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception);
        StringAssert.Contains(exception.Message, "must sum to 100%");
    }

    [TestMethod]
    public async Task CreateSharedBudgetAsync_ThrowsWhenHouseholdIdIsNull()
    {
        // Arrange
        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = null, // No household ID
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>();

        // Act & Assert
        InvalidOperationException? exception = null;
        try
        {
            await _householdService.CreateSharedBudgetAsync(budget, contributions);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception);
        StringAssert.Contains(exception.Message, "must be associated with a household");
    }

    [TestMethod]
    public async Task GetHouseholdBudgetsAsync_ReturnsHouseholdBudgets()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        
        var member = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 1",
            IsActive = true
        };
        _context.HouseholdMembers.Add(member);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member.HouseholdMemberId, 100m }
        };

        await _householdService.CreateSharedBudgetAsync(budget, contributions);

        // Act
        var result = await _householdService.GetHouseholdBudgetsAsync(household.HouseholdId);

        // Assert
        var budgets = result.ToList();
        Assert.AreEqual(1, budgets.Count());
        Assert.AreEqual("Test Budget", budgets[0].Name);
        Assert.AreNotEqual(0, budgets[0].HouseholdBudgetShares.Count());
    }

    #endregion

    #region Recurring Task Tests

    [TestMethod]
    public async Task CreateNextRecurrenceAsync_CreatesNewTaskWithCorrectDueDate()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var recurringTask = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Weekly cleaning",
            Description = "Clean the house",
            Category = HouseholdActivityType.Cleaning,
            DueDate = new DateTime(2025, 1, 1),
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Weekly,
            RecurrenceInterval = 1,
            Status = HouseholdTaskStatus.Done,
            IsCompleted = true
        };

        var created = await _householdService.CreateTaskAsync(recurringTask);

        // Act
        var nextTask = await _householdService.CreateNextRecurrenceAsync(created.HouseholdTaskId);

        // Assert
        Assert.IsNotNull(nextTask);
        Assert.AreEqual("Weekly cleaning", nextTask.Title);
        Assert.AreEqual(new DateTime(2025, 1, 8), nextTask.DueDate); // 7 days later
        Assert.AreEqual(HouseholdTaskStatus.ToDo, nextTask.Status);
        Assert.IsFalse(nextTask.IsCompleted);
        Assert.IsTrue(nextTask.IsRecurring);
        Assert.AreEqual(created.HouseholdTaskId, nextTask.ParentTaskId);
    }

    [TestMethod]
    public async Task CreateNextRecurrenceAsync_MonthlyRecurrence_CalculatesCorrectDate()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var recurringTask = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Monthly budget review",
            DueDate = new DateTime(2025, 1, 15),
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Monthly,
            RecurrenceInterval = 1,
            Status = HouseholdTaskStatus.Done,
            IsCompleted = true
        };

        var created = await _householdService.CreateTaskAsync(recurringTask);

        // Act
        var nextTask = await _householdService.CreateNextRecurrenceAsync(created.HouseholdTaskId);

        // Assert
        Assert.IsNotNull(nextTask);
        Assert.AreEqual(new DateTime(2025, 2, 15), nextTask.DueDate);
    }

    [TestMethod]
    public async Task UpdateTaskStatusAsync_CompletingRecurringTask_CreatesNextOccurrence()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var recurringTask = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Daily task",
            DueDate = DateTime.Today,
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Daily,
            RecurrenceInterval = 1
        };

        var created = await _householdService.CreateTaskAsync(recurringTask);

        // Act
        await _householdService.UpdateTaskStatusAsync(created.HouseholdTaskId, HouseholdTaskStatus.Done);

        // Assert
        var allTasks = await _householdService.GetTasksAsync(household.HouseholdId);
        var tasksList = allTasks.ToList();
        Assert.AreEqual(2, tasksList.Count); // Original + next occurrence
        Assert.IsTrue(tasksList.Any(t => t.DueDate == DateTime.Today.AddDays(1)));
    }

    #endregion

    #region Kanban Board Tests

    [TestMethod]
    public async Task GetTasksByStatusAsync_ReturnsOnlyTasksWithSpecifiedStatus()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "To do task",
            Status = HouseholdTaskStatus.ToDo
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "In progress task",
            Status = HouseholdTaskStatus.InProgress
        };
        var task3 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Done task",
            Status = HouseholdTaskStatus.Done
        };

        await _householdService.CreateTaskAsync(task1);
        await _householdService.CreateTaskAsync(task2);
        await _householdService.CreateTaskAsync(task3);

        // Act
        var inProgressTasks = await _householdService.GetTasksByStatusAsync(
            household.HouseholdId, 
            HouseholdTaskStatus.InProgress
        );

        // Assert
        var tasks = inProgressTasks.ToList();
        Assert.AreEqual(1, tasks.Count());
        Assert.AreEqual("In progress task", tasks[0].Title);
    }

    [TestMethod]
    public async Task GetTasksGroupedByStatusAsync_ReturnsTasksGroupedByStatus()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        await _householdService.CreateTaskAsync(new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Todo 1",
            Status = HouseholdTaskStatus.ToDo,
            Category = HouseholdActivityType.Cleaning
        });
        await _householdService.CreateTaskAsync(new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Todo 2",
            Status = HouseholdTaskStatus.ToDo,
            Category = HouseholdActivityType.Cleaning
        });
        await _householdService.CreateTaskAsync(new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "In Progress",
            Status = HouseholdTaskStatus.InProgress,
            Category = HouseholdActivityType.Cleaning
        });

        // Act
        var grouped = await _householdService.GetTasksGroupedByStatusAsync(
            household.HouseholdId, 
            HouseholdActivityType.Cleaning
        );

        // Assert
        Assert.AreEqual(2, grouped.Count);
        Assert.AreEqual(2, grouped[HouseholdTaskStatus.ToDo].Count());
        Assert.AreEqual(1, grouped[HouseholdTaskStatus.InProgress].Count());
    }

    [TestMethod]
    public async Task UpdateTaskStatusAsync_UpdatesStatusCorrectly()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task",
            Status = HouseholdTaskStatus.ToDo
        };

        var created = await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.UpdateTaskStatusAsync(
            created.HouseholdTaskId, 
            HouseholdTaskStatus.InProgress
        );

        // Assert
        Assert.IsTrue(result);
        var updated = await _context.HouseholdTasks.FindAsync(created.HouseholdTaskId);
        Assert.AreEqual(HouseholdTaskStatus.InProgress, updated!.Status);
    }

    [TestMethod]
    public async Task UpdateTaskStatusAsync_MovingToDone_MarksAsCompleted()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task",
            Status = HouseholdTaskStatus.InProgress
        };

        var created = await _householdService.CreateTaskAsync(task);

        // Act
        await _householdService.UpdateTaskStatusAsync(created.HouseholdTaskId, HouseholdTaskStatus.Done);

        // Assert
        var updated = await _context.HouseholdTasks.FindAsync(created.HouseholdTaskId);
        Assert.IsTrue(updated!.IsCompleted);
        Assert.IsNotNull(updated.CompletedDate);
    }

    #endregion
}
