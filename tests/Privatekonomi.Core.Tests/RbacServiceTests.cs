using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class RbacServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<IAuditLogService> _mockAuditService;
    private readonly RbacService _rbacService;
    private readonly string _testUserId = "test-user-1";
    private readonly string _testUser2Id = "test-user-2";
    private readonly int _testHouseholdId = 1;

    public RbacServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _mockAuditService = new Mock<IAuditLogService>();
        _rbacService = new RbacService(_context, _mockAuditService.Object);

        // Setup test data synchronously using GetAwaiter().GetResult() to avoid deadlocks
        SetupTestDataSync();
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

    private void SetupTestDataSync()
    {
        SetupTestData().GetAwaiter().GetResult();
    }

    private async Task SetupTestData()
    {
        // Create household
        var household = new Household
        {
            HouseholdId = _testHouseholdId,
            Name = "Test Household",
            CreatedDate = DateTime.UtcNow
        };
        _context.Households.Add(household);

        // Create household member for test user 1 (Admin)
        var member1 = new HouseholdMember
        {
            HouseholdMemberId = 1,
            HouseholdId = _testHouseholdId,
            UserId = _testUserId,
            Name = "Admin User",
            Email = "admin@test.com",
            IsActive = true,
            JoinedDate = DateTime.UtcNow
        };
        _context.HouseholdMembers.Add(member1);

        // Create household member for test user 2
        var member2 = new HouseholdMember
        {
            HouseholdMemberId = 2,
            HouseholdId = _testHouseholdId,
            UserId = _testUser2Id,
            Name = "Regular User",
            Email = "user@test.com",
            IsActive = true,
            JoinedDate = DateTime.UtcNow
        };
        _context.HouseholdMembers.Add(member2);

        // Assign Admin role to user 1
        var adminRole = new HouseholdRole
        {
            HouseholdRoleId = 1,
            HouseholdMemberId = 1,
            RoleType = HouseholdRoleType.Admin,
            AssignedDate = DateTime.UtcNow,
            AssignedBy = "system",
            IsActive = true
        };
        _context.HouseholdRoles.Add(adminRole);

        await _context.SaveChangesAsync();
    }

    // ==================== Role Management Tests ====================

    [TestMethod]
    public async Task GetUserRoleInHouseholdAsync_ReturnsAdminRole()
    {
        // Act
        var role = await _rbacService.GetUserRoleInHouseholdAsync(_testUserId, _testHouseholdId);

        // Assert
        Assert.IsNotNull(role);
        Assert.AreEqual(HouseholdRoleType.Admin, role.RoleType);
        Assert.IsTrue(role.IsActive);
    }

    [TestMethod]
    public async Task GetUserRoleInHouseholdAsync_ReturnsNullForNonMember()
    {
        // Act
        var role = await _rbacService.GetUserRoleInHouseholdAsync("non-existent-user", _testHouseholdId);

        // Assert
        Assert.IsNull(role);
    }

    [TestMethod]
    public async Task HasRoleAsync_ReturnsTrueForCorrectRole()
    {
        // Act
        var hasRole = await _rbacService.HasRoleAsync(_testUserId, _testHouseholdId, HouseholdRoleType.Admin);

        // Assert
        Assert.IsTrue(hasRole);
    }

    [TestMethod]
    public async Task HasRoleAsync_ReturnsFalseForIncorrectRole()
    {
        // Act
        var hasRole = await _rbacService.HasRoleAsync(_testUserId, _testHouseholdId, HouseholdRoleType.ViewOnly);

        // Assert
        Assert.IsFalse(hasRole);
    }

    [TestMethod]
    public async Task HasMinimumRoleAsync_ReturnsTrueForSameOrHigherRole()
    {
        // Admin should pass minimum role check for FullAccess, Editor, etc.
        // Act
        var hasMinRole = await _rbacService.HasMinimumRoleAsync(_testUserId, _testHouseholdId, HouseholdRoleType.FullAccess);

        // Assert
        Assert.IsTrue(hasMinRole);
    }

    [TestMethod]
    public async Task AssignRoleAsync_CreatesNewRoleSuccessfully()
    {
        // Arrange
        var targetMemberId = 2; // User 2

        // Act
        var newRole = await _rbacService.AssignRoleAsync(_testUserId, targetMemberId, HouseholdRoleType.Editor);

        // Assert
        Assert.IsNotNull(newRole);
        Assert.AreEqual(HouseholdRoleType.Editor, newRole.RoleType);
        Assert.IsTrue(newRole.IsActive);
        Assert.AreEqual(_testUserId, newRole.AssignedBy);
        
        // Verify audit log was called
        _mockAuditService.Verify(x => x.LogAsync(
            "RoleAssigned",
            "HouseholdRole",
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            _testUserId,
            null), Times.Once);
    }

    [TestMethod]
    public async Task AssignRoleAsync_ThrowsExceptionWhenNonAdminTriesToAssign()
    {
        // Arrange - First assign Editor role to user 2
        await _rbacService.AssignRoleAsync(_testUserId, 2, HouseholdRoleType.Editor);

        // Act & Assert
        bool exceptionThrown = false;
        try
        {
            await _rbacService.AssignRoleAsync(_testUser2Id, 1, HouseholdRoleType.FullAccess);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }
        
        Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
    }

    [TestMethod]
    public async Task AssignRoleAsync_RevokesExistingRoleWhenAssigningNew()
    {
        // Arrange
        var targetMemberId = 2;
        await _rbacService.AssignRoleAsync(_testUserId, targetMemberId, HouseholdRoleType.Editor);

        // Act - Assign a different role
        await _rbacService.AssignRoleAsync(_testUserId, targetMemberId, HouseholdRoleType.FullAccess);

        // Assert
        var roles = await _context.HouseholdRoles
            .Where(r => r.HouseholdMemberId == targetMemberId)
            .ToListAsync();

        var activeRoles = roles.Where(r => r.IsActive).ToList();
        var revokedRoles = roles.Where(r => !r.IsActive).ToList();

        Assert.AreEqual(1, activeRoles.Count());
        Assert.AreEqual(HouseholdRoleType.FullAccess, activeRoles[0].RoleType);
        Assert.AreEqual(1, revokedRoles.Count());
        Assert.AreEqual(HouseholdRoleType.Editor, revokedRoles[0].RoleType);
    }

    [TestMethod]
    public async Task TransferAdminRoleAsync_TransfersAdminRoleSuccessfully()
    {
        // Act
        var newAdminRole = await _rbacService.TransferAdminRoleAsync(_testUserId, _testUser2Id, _testHouseholdId);

        // Assert
        Assert.IsNotNull(newAdminRole);
        Assert.AreEqual(HouseholdRoleType.Admin, newAdminRole.RoleType);

        // Verify old admin now has Limited role
        var oldAdminRole = await _rbacService.GetUserRoleInHouseholdAsync(_testUserId, _testHouseholdId);
        Assert.IsNotNull(oldAdminRole);
        Assert.AreEqual(HouseholdRoleType.Limited, oldAdminRole.RoleType);

        // Verify new admin has Admin role
        var newRole = await _rbacService.GetUserRoleInHouseholdAsync(_testUser2Id, _testHouseholdId);
        Assert.IsNotNull(newRole);
        Assert.AreEqual(HouseholdRoleType.Admin, newRole.RoleType);
    }

    [TestMethod]
    public async Task RemoveRoleAsync_CannotRemoveLastAdminRole()
    {
        // Act & Assert
        bool exceptionThrown = false;
        try
        {
            await _rbacService.RemoveRoleAsync(_testUserId, 1);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }
        
        Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
    }

    // ==================== Permission Check Tests ====================

    [TestMethod]
    public async Task HasPermissionAsync_AdminHasAllPermissions()
    {
        // Act
        var hasTransactionPermission = await _rbacService.HasPermissionAsync(_testUserId, _testHouseholdId, "transaction.view.all");
        var hasBudgetPermission = await _rbacService.HasPermissionAsync(_testUserId, _testHouseholdId, "budget.edit.all");

        // Assert
        Assert.IsTrue(hasTransactionPermission);
        Assert.IsTrue(hasBudgetPermission);
    }

    [TestMethod]
    public async Task CanPerformActionAsync_RespectsAmountLimits()
    {
        // Arrange - Assign Editor role to user 2
        await _rbacService.AssignRoleAsync(_testUserId, 2, HouseholdRoleType.Editor);

        // Act
        var canCreateSmallTransaction = await _rbacService.CanPerformActionAsync(_testUser2Id, _testHouseholdId, "transaction.create", 1000m);
        var canCreateLargeTransaction = await _rbacService.CanPerformActionAsync(_testUser2Id, _testHouseholdId, "transaction.create", 10000m);

        // Assert
        Assert.IsTrue(canCreateSmallTransaction);
        Assert.IsFalse(canCreateLargeTransaction); // Editor limit is 5000 SEK
    }

    [TestMethod]
    public async Task CheckPermissionAsync_ReturnsCorrectAmountLimitForEditor()
    {
        // Arrange - Assign Editor role to user 2
        await _rbacService.AssignRoleAsync(_testUserId, 2, HouseholdRoleType.Editor);

        // Act
        var result = await _rbacService.CheckPermissionAsync(_testUser2Id, _testHouseholdId, "transaction.create");

        // Assert
        Assert.IsTrue(result.IsAllowed);
        Assert.AreEqual(5000m, result.AmountLimit);
        Assert.IsTrue(result.RequiresApproval);
    }

    // ==================== Delegation Tests ====================

    [TestMethod]
    public async Task DelegateRoleAsync_AdminCanDelegateFullAccess()
    {
        // Act
        var delegatedRole = await _rbacService.DelegateRoleAsync(
            _testUserId,
            _testUser2Id,
            _testHouseholdId,
            HouseholdRoleType.FullAccess,
            DateTime.UtcNow.AddDays(30));

        // Assert
        Assert.IsNotNull(delegatedRole);
        Assert.AreEqual(HouseholdRoleType.FullAccess, delegatedRole.RoleType);
        Assert.IsTrue(delegatedRole.IsDelegated);
        Assert.AreEqual(_testUserId, delegatedRole.DelegatedBy);
        Assert.IsNotNull(delegatedRole.DelegationEndDate);
    }

    [TestMethod]
    public async Task DelegateRoleAsync_ThrowsExceptionWhenExceedingMaxPeriod()
    {
        // Act & Assert
        bool exceptionThrown = false;
        try
        {
            await _rbacService.DelegateRoleAsync(
                _testUserId,
                _testUser2Id,
                _testHouseholdId,
                HouseholdRoleType.FullAccess,
                DateTime.UtcNow.AddDays(365)); // Max is 90 days for FullAccess
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }
        
        Assert.IsTrue(exceptionThrown, "Expected ArgumentException was not thrown");
    }

    [TestMethod]
    public async Task DelegateRoleAsync_DelegatedRoleCannotDelegateFurther()
    {
        // Arrange - Create a delegation
        await _rbacService.DelegateRoleAsync(
            _testUserId,
            _testUser2Id,
            _testHouseholdId,
            HouseholdRoleType.FullAccess,
            DateTime.UtcNow.AddDays(30));

        // Create a third user
        var member3 = new HouseholdMember
        {
            HouseholdMemberId = 3,
            HouseholdId = _testHouseholdId,
            UserId = "test-user-3",
            Name = "Third User",
            IsActive = true,
            JoinedDate = DateTime.UtcNow
        };
        _context.HouseholdMembers.Add(member3);
        await _context.SaveChangesAsync();

        // Act & Assert - User 2 (with delegated role) tries to delegate to user 3
        bool exceptionThrown = false;
        try
        {
            await _rbacService.DelegateRoleAsync(
                _testUser2Id,
                "test-user-3",
                _testHouseholdId,
                HouseholdRoleType.Editor,
                DateTime.UtcNow.AddDays(7));
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }
        
        Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
    }

    [TestMethod]
    public async Task RevokeDelegationAsync_RevokesSuccessfully()
    {
        // Arrange - Create a delegation
        var delegation = await _rbacService.DelegateRoleAsync(
            _testUserId,
            _testUser2Id,
            _testHouseholdId,
            HouseholdRoleType.Editor,
            DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _rbacService.RevokeDelegationAsync(_testUserId, delegation.HouseholdRoleId);

        // Assert
        Assert.IsTrue(result);

        var revokedDelegation = await _context.HouseholdRoles.FindAsync(delegation.HouseholdRoleId);
        Assert.IsNotNull(revokedDelegation);
        Assert.IsFalse(revokedDelegation.IsActive);
        Assert.IsNotNull(revokedDelegation.RevokedDate);
    }

    [TestMethod]
    public async Task GetActiveDelegationsAsync_ReturnsOnlyActiveDelegations()
    {
        // Arrange - Create two delegations, revoke one
        var delegation1 = await _rbacService.DelegateRoleAsync(
            _testUserId,
            _testUser2Id,
            _testHouseholdId,
            HouseholdRoleType.Editor,
            DateTime.UtcNow.AddDays(30));

        var member3 = new HouseholdMember
        {
            HouseholdMemberId = 3,
            HouseholdId = _testHouseholdId,
            UserId = "test-user-3",
            Name = "Third User",
            IsActive = true,
            JoinedDate = DateTime.UtcNow
        };
        _context.HouseholdMembers.Add(member3);
        await _context.SaveChangesAsync();

        var delegation2 = await _rbacService.DelegateRoleAsync(
            _testUserId,
            "test-user-3",
            _testHouseholdId,
            HouseholdRoleType.ViewOnly,
            DateTime.UtcNow.AddDays(15));

        await _rbacService.RevokeDelegationAsync(_testUserId, delegation1.HouseholdRoleId);

        // Act
        var activeDelegations = await _rbacService.GetActiveDelegationsAsync(_testHouseholdId);

        // Assert
        Assert.AreEqual(1, activeDelegations.Count());
        Assert.AreEqual(delegation2.HouseholdRoleId, activeDelegations.First().HouseholdRoleId);
    }

    // ==================== Validation Tests ====================

    [TestMethod]
    public async Task ValidateRoleAssignmentAsync_FailsWhenNonAdminTriesToAssign()
    {
        // Arrange - Assign Editor role to user 2
        await _rbacService.AssignRoleAsync(_testUserId, 2, HouseholdRoleType.Editor);

        // Act
        var validation = await _rbacService.ValidateRoleAssignmentAsync(_testUser2Id, _testHouseholdId, 1, HouseholdRoleType.FullAccess);

        // Assert
        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Errors.ToList(), "Only Admin can assign roles");
    }

    [TestMethod]
    public async Task ValidateRoleAssignmentAsync_WarnsWhenReplacingAdmin()
    {
        // Act
        var validation = await _rbacService.ValidateRoleAssignmentAsync(_testUserId, _testHouseholdId, 2, HouseholdRoleType.Admin);

        // Assert
        Assert.IsTrue(validation.IsValid);
        var hasWarning = validation.Warnings.Any(w => w.Contains("replace the existing admin"));
        Assert.IsTrue(hasWarning, "Expected warning about replacing admin was not found");
    }

    [TestMethod]
    public async Task ValidateRoleAssignmentAsync_RequiresDateOfBirthForChildRole()
    {
        // Act
        var validation = await _rbacService.ValidateRoleAssignmentAsync(_testUserId, _testHouseholdId, 2, HouseholdRoleType.Child);

        // Assert
        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Errors.ToList(), "Date of birth required for Child role");
    }

    [TestMethod]
    public async Task ValidateHouseholdRolesAsync_PassesWithOneAdmin()
    {
        // Act
        var isValid = await _rbacService.ValidateHouseholdRolesAsync(_testHouseholdId);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task ValidateHouseholdRolesAsync_FailsWithNoAdmin()
    {
        // Arrange - Remove admin role
        var adminRole = await _context.HouseholdRoles.FindAsync(1);
        if (adminRole != null)
        {
            adminRole.IsActive = false;
            await _context.SaveChangesAsync();
        }

        // Act
        var isValid = await _rbacService.ValidateHouseholdRolesAsync(_testHouseholdId);

        // Assert
        Assert.IsFalse(isValid);
    }

    // ==================== Utility Tests ====================

    [TestMethod]
    public void CanDelegate_AdminCanDelegateAllExceptAdmin()
    {
        // Assert
        Assert.IsFalse(_rbacService.CanDelegate(HouseholdRoleType.Admin, HouseholdRoleType.Admin));
        Assert.IsTrue(_rbacService.CanDelegate(HouseholdRoleType.Admin, HouseholdRoleType.FullAccess));
        Assert.IsTrue(_rbacService.CanDelegate(HouseholdRoleType.Admin, HouseholdRoleType.Editor));
        Assert.IsTrue(_rbacService.CanDelegate(HouseholdRoleType.Admin, HouseholdRoleType.ViewOnly));
    }

    [TestMethod]
    public void CanDelegate_FullAccessCanDelegateLimitedRoles()
    {
        // Assert
        Assert.IsTrue(_rbacService.CanDelegate(HouseholdRoleType.FullAccess, HouseholdRoleType.Editor));
        Assert.IsTrue(_rbacService.CanDelegate(HouseholdRoleType.FullAccess, HouseholdRoleType.ViewOnly));
        Assert.IsTrue(_rbacService.CanDelegate(HouseholdRoleType.FullAccess, HouseholdRoleType.Limited));
        Assert.IsFalse(_rbacService.CanDelegate(HouseholdRoleType.FullAccess, HouseholdRoleType.FullAccess));
    }

    [TestMethod]
    public void CanDelegate_OtherRolesCannotDelegate()
    {
        // Assert
        Assert.IsFalse(_rbacService.CanDelegate(HouseholdRoleType.Editor, HouseholdRoleType.ViewOnly));
        Assert.IsFalse(_rbacService.CanDelegate(HouseholdRoleType.ViewOnly, HouseholdRoleType.Limited));
        Assert.IsFalse(_rbacService.CanDelegate(HouseholdRoleType.Limited, HouseholdRoleType.Child));
    }

    [TestMethod]
    public void GetMaxDelegationPeriod_ReturnsCorrectPeriods()
    {
        // Assert
        Assert.AreEqual(90, _rbacService.GetMaxDelegationPeriod(HouseholdRoleType.Admin, HouseholdRoleType.FullAccess));
        Assert.AreEqual(365, _rbacService.GetMaxDelegationPeriod(HouseholdRoleType.Admin, HouseholdRoleType.ViewOnly));
        Assert.AreEqual(30, _rbacService.GetMaxDelegationPeriod(HouseholdRoleType.FullAccess, HouseholdRoleType.Editor));
    }

    [TestMethod]
    public void RequiresApprovalForDelegation_ReturnsCorrectValue()
    {
        // Assert
        Assert.IsFalse(_rbacService.RequiresApprovalForDelegation(HouseholdRoleType.Admin, HouseholdRoleType.FullAccess));
        Assert.IsTrue(_rbacService.RequiresApprovalForDelegation(HouseholdRoleType.FullAccess, HouseholdRoleType.Editor));
        Assert.IsTrue(_rbacService.RequiresApprovalForDelegation(HouseholdRoleType.FullAccess, HouseholdRoleType.Limited));
        Assert.IsFalse(_rbacService.RequiresApprovalForDelegation(HouseholdRoleType.FullAccess, HouseholdRoleType.ViewOnly));
    }
}
