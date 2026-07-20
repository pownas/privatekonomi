using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class DashboardLayoutServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly DashboardLayoutService _service;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private const string TestUserId = "test-user-123";

    public DashboardLayoutServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PrivatekonomyContext(options);

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(TestUserId);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        _service = new DashboardLayoutService(_context, _mockCurrentUserService.Object);
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
    public async Task GetUserLayoutsAsync_ReturnsUserLayouts()
    {
        // Arrange
        var layout1 = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Hem",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var layout2 = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Investeringar",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var layout3 = new DashboardLayout
        {
            UserId = "other-user",
            Name = "Other",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DashboardLayouts.AddRange(layout1, layout2, layout3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserLayoutsAsync(TestUserId);

        // Assert
        Assert.AreEqual(2, result.Count());
        foreach (var l in result) { Assert.AreEqual(TestUserId, l.UserId); }
    }

    [TestMethod]
    public async Task GetDefaultLayoutAsync_ReturnsDefaultLayout()
    {
        // Arrange
        var defaultLayout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Hem",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var otherLayout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Budget",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DashboardLayouts.AddRange(defaultLayout, otherLayout);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDefaultLayoutAsync(TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefault);
        Assert.AreEqual("Hem", result.Name);
    }

    [TestMethod]
    public async Task GetDefaultLayoutAsync_CreatesDefaultWhenNoneExists()
    {
        // Act
        var result = await _service.GetDefaultLayoutAsync(TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefault);
        Assert.AreEqual("Hem", result.Name);
        Assert.AreNotEqual(0, result.Widgets.Count());
    }

    [TestMethod]
    public async Task CreateLayoutAsync_CreatesNewLayout()
    {
        // Arrange
        var newLayout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Test Layout",
            IsDefault = false,
            Widgets = new List<WidgetConfiguration>
            {
                new WidgetConfiguration
                {
                    Type = WidgetType.NetWorth,
                    Row = 0,
                    Column = 0,
                    Width = 12,
                    Height = 2
                }
            }
        };

        // Act
        var result = await _service.CreateLayoutAsync(newLayout);

        // Assert
        Assert.AreNotEqual(0, result.LayoutId);
        Assert.AreEqual("Test Layout", result.Name);
        Assert.AreEqual(1, result.Widgets.Count());
        Assert.AreNotEqual(DateTime.MinValue, result.CreatedAt);
        Assert.AreNotEqual(DateTime.MinValue, result.UpdatedAt);
    }

    [TestMethod]
    public async Task CreateLayoutAsync_SetsCurrentUserIdWhenAuthenticated()
    {
        // Arrange
        var newLayout = new DashboardLayout
        {
            UserId = "", // Empty, should be set by service
            Name = "Test Layout",
            IsDefault = false
        };

        // Act
        var result = await _service.CreateLayoutAsync(newLayout);

        // Assert
        Assert.AreEqual(TestUserId, result.UserId);
    }

    [TestMethod]
    public async Task CreateLayoutAsync_UnsetsOtherDefaultsWhenCreatingDefault()
    {
        // Arrange
        var existingDefault = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Old Default",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DashboardLayouts.Add(existingDefault);
        await _context.SaveChangesAsync();

        var newDefault = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "New Default",
            IsDefault = true
        };

        // Act
        await _service.CreateLayoutAsync(newDefault);

        // Assert
        var oldDefault = await _context.DashboardLayouts.FindAsync(existingDefault.LayoutId);
        Assert.IsNotNull(oldDefault);
        Assert.IsFalse(oldDefault.IsDefault);
    }

    [TestMethod]
    public async Task UpdateLayoutAsync_UpdatesExistingLayout()
    {
        // Arrange
        var layout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Original Name",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DashboardLayouts.Add(layout);
        await _context.SaveChangesAsync();

        layout.Name = "Updated Name";

        // Act
        var result = await _service.UpdateLayoutAsync(layout);

        // Assert
        Assert.AreEqual("Updated Name", result.Name);
        var updated = await _context.DashboardLayouts.FindAsync(layout.LayoutId);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Name", updated.Name);
    }

    [TestMethod]
    public async Task DeleteLayoutAsync_DeletesLayout()
    {
        // Arrange
        var layout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "To Delete",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DashboardLayouts.Add(layout);
        await _context.SaveChangesAsync();
        var layoutId = layout.LayoutId;

        // Act
        await _service.DeleteLayoutAsync(layoutId);

        // Assert
        var deleted = await _context.DashboardLayouts.FindAsync(layoutId);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task DeleteLayoutAsync_SetsAnotherAsDefaultWhenDeletingDefault()
    {
        // Arrange
        var defaultLayout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Default",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var otherLayout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Other",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DashboardLayouts.AddRange(defaultLayout, otherLayout);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteLayoutAsync(defaultLayout.LayoutId);

        // Assert
        var updated = await _context.DashboardLayouts.FindAsync(otherLayout.LayoutId);
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.IsDefault);
    }

    [TestMethod]
    public async Task SetDefaultLayoutAsync_SetsLayoutAsDefault()
    {
        // Arrange
        var layout1 = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Layout 1",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var layout2 = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Layout 2",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DashboardLayouts.AddRange(layout1, layout2);
        await _context.SaveChangesAsync();

        // Act
        await _service.SetDefaultLayoutAsync(layout2.LayoutId, TestUserId);

        // Assert
        var updated1 = await _context.DashboardLayouts.FindAsync(layout1.LayoutId);
        var updated2 = await _context.DashboardLayouts.FindAsync(layout2.LayoutId);
        Assert.IsNotNull(updated1);
        Assert.IsNotNull(updated2);
        Assert.IsFalse(updated1.IsDefault);
        Assert.IsTrue(updated2.IsDefault);
    }

    [TestMethod]
    public async Task CreateDefaultLayoutForUserAsync_CreatesLayoutWithWidgets()
    {
        // Act
        var result = await _service.CreateDefaultLayoutForUserAsync(TestUserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TestUserId, result.UserId);
        Assert.AreEqual("Hem", result.Name);
        Assert.IsTrue(result.IsDefault);
        Assert.AreNotEqual(0, result.Widgets.Count());
        Assert.IsTrue(result.Widgets.Count >= 5); // Should have at least 5 default widgets
    }

    [TestMethod]
    public async Task GetLayoutByIdAsync_ReturnsCorrectLayout()
    {
        // Arrange
        var layout = new DashboardLayout
        {
            UserId = TestUserId,
            Name = "Test Layout",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Widgets = new List<WidgetConfiguration>
            {
                new WidgetConfiguration
                {
                    Type = WidgetType.CashFlow,
                    Row = 0,
                    Column = 0,
                    Width = 6,
                    Height = 2
                }
            }
        };
        _context.DashboardLayouts.Add(layout);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLayoutByIdAsync(layout.LayoutId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(layout.LayoutId, result.LayoutId);
        Assert.AreEqual("Test Layout", result.Name);
        Assert.AreEqual(1, result.Widgets.Count());
    }
}
