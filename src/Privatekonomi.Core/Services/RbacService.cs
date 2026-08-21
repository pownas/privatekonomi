using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Implementation of role-based access control service
/// </summary>
public class RbacService : IRbacService
{
    private readonly PrivatekonomyContext _context;
    private readonly IAuditLogService _auditService;

    public RbacService(PrivatekonomyContext context, IAuditLogService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // ==================== Role Management ====================

    public async Task<HouseholdRole?> GetUserRoleInHouseholdAsync(string userId, int householdId)
    {
        return await _context.HouseholdRoles
            .Include(r => r.HouseholdMember)
            .Where(r => r.HouseholdMember!.UserId == userId &&
                       r.HouseholdMember.HouseholdId == householdId &&
                       r.IsActive)
            .OrderByDescending(r => r.AssignedDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasRoleAsync(string userId, int householdId, HouseholdRoleType roleType)
    {
        var role = await GetUserRoleInHouseholdAsync(userId, householdId);
        return role?.RoleType == roleType;
    }

    public async Task<bool> HasMinimumRoleAsync(string userId, int householdId, HouseholdRoleType minimumRole)
    {
        var role = await GetUserRoleInHouseholdAsync(userId, householdId);
        if (role == null) return false;

        // Role hierarchy: Admin(1) > FullAccess(2) > Editor(3) > ViewOnly(4) > Limited(5) > Child(6)
        return (int)role.RoleType <= (int)minimumRole;
    }

    public async Task<HouseholdRole> AssignRoleAsync(string assignerUserId, int householdMemberId, HouseholdRoleType roleType)
    {
        var member = await _context.HouseholdMembers
            .Include(m => m.Roles)
            .FirstOrDefaultAsync(m => m.HouseholdMemberId == householdMemberId);

        if (member == null)
            throw new ArgumentException("Household member not found");

        // Validate assignment
        var validation = await ValidateRoleAssignmentAsync(assignerUserId, member.HouseholdId, householdMemberId, roleType);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Role assignment validation failed: {string.Join(", ", validation.Errors)}");

        // If assigning Admin role, revoke existing admin
        if (roleType == HouseholdRoleType.Admin)
        {
            var existingAdmin = await _context.HouseholdRoles
                .Include(r => r.HouseholdMember)
                .FirstOrDefaultAsync(r => r.HouseholdMember!.HouseholdId == member.HouseholdId &&
                                         r.RoleType == HouseholdRoleType.Admin &&
                                         r.IsActive);

            if (existingAdmin != null && existingAdmin.HouseholdMemberId != householdMemberId)
            {
                existingAdmin.IsActive = false;
                existingAdmin.RevokedDate = DateTime.UtcNow;
                existingAdmin.RevokedBy = assignerUserId;
            }
        }

        // Deactivate existing roles for this member
        foreach (var existingRole in member.Roles.Where(r => r.IsActive))
        {
            existingRole.IsActive = false;
            existingRole.RevokedDate = DateTime.UtcNow;
            existingRole.RevokedBy = assignerUserId;
        }

        // Create new role
        var newRole = new HouseholdRole
        {
            HouseholdMemberId = householdMemberId,
            RoleType = roleType,
            AssignedDate = DateTime.UtcNow,
            AssignedBy = assignerUserId,
            IsActive = true
        };

        _context.HouseholdRoles.Add(newRole);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            "RoleAssigned",
            "HouseholdRole",
            newRole.HouseholdRoleId,
            $"Assigned {roleType} role to member {member.Name}",
            assignerUserId);

        return newRole;
    }

    public async Task<bool> RemoveRoleAsync(string userId, int roleId, string? reason = null)
    {
        var role = await _context.HouseholdRoles.FindAsync(roleId);
        if (role == null) return false;

        // Cannot remove the last admin role
        if (role.RoleType == HouseholdRoleType.Admin)
        {
            var member = await _context.HouseholdMembers.FindAsync(role.HouseholdMemberId);
            if (member != null)
            {
                var adminCount = await _context.HouseholdRoles
                    .Include(r => r.HouseholdMember)
                    .CountAsync(r => r.HouseholdMember!.HouseholdId == member.HouseholdId &&
                                   r.RoleType == HouseholdRoleType.Admin &&
                                   r.IsActive);

                if (adminCount <= 1)
                    throw new InvalidOperationException("Cannot remove the last admin role. Transfer admin role first.");
            }
        }

        role.IsActive = false;
        role.RevokedDate = DateTime.UtcNow;
        role.RevokedBy = userId;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            "RoleRevoked",
            "HouseholdRole",
            roleId,
            $"Revoked {role.RoleType} role. Reason: {reason ?? "Not specified"}",
            userId);

        return true;
    }

    public async Task<HouseholdRole> TransferAdminRoleAsync(string currentAdminUserId, string newAdminUserId, int householdId)
    {
        // Validate current admin
        var currentAdminRole = await GetUserRoleInHouseholdAsync(currentAdminUserId, householdId);
        if (currentAdminRole?.RoleType != HouseholdRoleType.Admin)
            throw new UnauthorizedAccessException("Only the current admin can transfer the admin role");

        // Get new admin member
        var newAdminMember = await _context.HouseholdMembers
            .FirstOrDefaultAsync(m => m.UserId == newAdminUserId && m.HouseholdId == householdId);

        if (newAdminMember == null)
            throw new ArgumentException("Target user is not a member of this household");

        // Assign admin role to new admin (this will automatically revoke old admin)
        var newRole = await AssignRoleAsync(currentAdminUserId, newAdminMember.HouseholdMemberId, HouseholdRoleType.Admin);

        // Assign Limited role to former admin
        var currentAdminMember = await _context.HouseholdMembers
            .FirstOrDefaultAsync(m => m.UserId == currentAdminUserId && m.HouseholdId == householdId);

        if (currentAdminMember != null)
        {
            await AssignRoleAsync(newAdminUserId, currentAdminMember.HouseholdMemberId, HouseholdRoleType.Limited);
        }

        // Special audit log for admin transfer
        await _auditService.LogAsync(
            "AdminTransferred",
            "Household",
            householdId,
            $"Admin role transferred from {currentAdminUserId} to {newAdminUserId}",
            currentAdminUserId);

        return newRole;
    }

    // ==================== Permission Checks ====================

    public async Task<bool> HasPermissionAsync(string userId, int householdId, string permissionKey)
    {
        var role = await GetUserRoleInHouseholdAsync(userId, householdId);
        if (role == null) return false;

        var permission = await _context.RolePermissions
            .FirstOrDefaultAsync(p => p.RoleType == role.RoleType && p.PermissionKey == permissionKey);

        return permission?.IsAllowed ?? GetDefaultPermission(role.RoleType, permissionKey);
    }

    public async Task<bool> CanPerformActionAsync(string userId, int householdId, string action, decimal? amount = null)
    {
        var result = await CheckPermissionAsync(userId, householdId, action, new { amount });
        
        if (!result.IsAllowed)
            return false;
            
        // If no approval required, action is allowed
        if (!result.RequiresApproval)
            return true;
            
        // If approval required, check amount is within limit
        var withinLimit = !amount.HasValue || amount.Value <= (result.AmountLimit ?? decimal.MaxValue);
        return withinLimit;
    }

    public async Task<PermissionCheckResult> CheckPermissionAsync(string userId, int householdId, string permissionKey, object? context = null)
    {
        var role = await GetUserRoleInHouseholdAsync(userId, householdId);
        
        if (role == null)
        {
            return new PermissionCheckResult
            {
                IsAllowed = false,
                DenialReason = "User does not have a role in this household"
            };
        }

        var permission = await _context.RolePermissions
            .FirstOrDefaultAsync(p => p.RoleType == role.RoleType && p.PermissionKey == permissionKey);

        var isAllowed = permission?.IsAllowed ?? GetDefaultPermission(role.RoleType, permissionKey);
        var amountLimit = permission?.AmountLimit ?? GetDefaultAmountLimit(role.RoleType, permissionKey);

        return new PermissionCheckResult
        {
            IsAllowed = isAllowed,
            AmountLimit = amountLimit,
            RequiresApproval = amountLimit.HasValue,
            DenialReason = isAllowed ? null : $"Role {role.RoleType} does not have permission: {permissionKey}"
        };
    }

    // ==================== Delegation ====================

    public async Task<HouseholdRole> DelegateRoleAsync(
        string delegatorUserId,
        string targetUserId,
        int householdId,
        HouseholdRoleType roleType,
        DateTime? endDate = null)
    {
        // Validate delegator has permission
        var delegatorRole = await GetUserRoleInHouseholdAsync(delegatorUserId, householdId);
        if (delegatorRole == null)
            throw new UnauthorizedAccessException("You do not have a role in this household");

        if (!CanDelegate(delegatorRole.RoleType, roleType))
            throw new UnauthorizedAccessException($"Role {delegatorRole.RoleType} cannot delegate {roleType}");

        // Validate delegation period
        var maxPeriod = GetMaxDelegationPeriod(delegatorRole.RoleType, roleType);
        var actualEndDate = endDate ?? DateTime.UtcNow.AddDays(maxPeriod);

        if (actualEndDate > DateTime.UtcNow.AddDays(maxPeriod))
            throw new ArgumentException($"Maximum delegation period is {maxPeriod} days");

        // Get or create target member
        var targetMember = await _context.HouseholdMembers
            .FirstOrDefaultAsync(m => m.UserId == targetUserId && m.HouseholdId == householdId);

        if (targetMember == null)
            throw new ArgumentException("Target user is not a member of this household");

        // Check if delegator's role is itself delegated (no delegation chains)
        if (delegatorRole.IsDelegated)
            throw new InvalidOperationException("Delegated roles cannot be delegated further");

        // Create delegated role
        var requiresApproval = RequiresApprovalForDelegation(delegatorRole.RoleType, roleType);

        var delegatedRole = new HouseholdRole
        {
            HouseholdMemberId = targetMember.HouseholdMemberId,
            RoleType = roleType,
            IsDelegated = true,
            DelegatedBy = delegatorUserId,
            DelegationStartDate = DateTime.UtcNow,
            DelegationEndDate = actualEndDate,
            AssignedDate = DateTime.UtcNow,
            AssignedBy = delegatorUserId,
            IsActive = !requiresApproval  // Pending if requires approval
        };

        _context.HouseholdRoles.Add(delegatedRole);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            "RoleDelegated",
            "HouseholdRole",
            delegatedRole.HouseholdRoleId,
            $"Delegated {roleType} to {targetUserId} until {actualEndDate:yyyy-MM-dd}. Requires approval: {requiresApproval}",
            delegatorUserId);

        return delegatedRole;
    }

    public async Task<bool> RevokeDelegationAsync(string userId, int delegationId, string? reason = null)
    {
        var delegation = await _context.HouseholdRoles.FindAsync(delegationId);
        
        if (delegation == null || !delegation.IsDelegated)
            return false;

        // Only the delegator or admin can revoke
        var member = await _context.HouseholdMembers.FindAsync(delegation.HouseholdMemberId);
        if (member != null)
        {
            var userRole = await GetUserRoleInHouseholdAsync(userId, member.HouseholdId);
            if (delegation.DelegatedBy != userId && userRole?.RoleType != HouseholdRoleType.Admin)
                throw new UnauthorizedAccessException("Only the delegator or admin can revoke a delegation");
        }

        delegation.IsActive = false;
        delegation.RevokedDate = DateTime.UtcNow;
        delegation.RevokedBy = userId;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            "DelegationRevoked",
            "HouseholdRole",
            delegationId,
            $"Delegation revoked. Reason: {reason ?? "Not specified"}",
            userId);

        return true;
    }

    public async Task<IEnumerable<HouseholdRole>> GetActiveDelegationsAsync(int householdId)
    {
        return await _context.HouseholdRoles
            .Include(r => r.HouseholdMember)
            .Where(r => r.HouseholdMember!.HouseholdId == householdId &&
                       r.IsDelegated &&
                       r.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<HouseholdRole>> GetExpiringDelegationsAsync(int days = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(days);
        return await _context.HouseholdRoles
            .Include(r => r.HouseholdMember)
            .Where(r => r.IsDelegated &&
                       r.IsActive &&
                       r.DelegationEndDate.HasValue &&
                       r.DelegationEndDate.Value <= cutoffDate)
            .ToListAsync();
    }

    // ==================== Validation ====================

    public async Task<RoleValidationResult> ValidateRoleAssignmentAsync(
        string userId,
        int householdId,
        int targetMemberId,
        HouseholdRoleType roleType)
    {
        var result = new RoleValidationResult { IsValid = true };

        // Check if assigner has permission
        var assignerRole = await GetUserRoleInHouseholdAsync(userId, householdId);
        if (assignerRole == null)
        {
            result.IsValid = false;
            result.Errors.Add("You do not have a role in this household");
            return result;
        }

        // Only Admin can assign roles
        if (assignerRole.RoleType != HouseholdRoleType.Admin)
        {
            result.IsValid = false;
            result.Errors.Add("Only Admin can assign roles");
            return result;
        }

        // Get target member
        var targetMember = await _context.HouseholdMembers
            .Include(m => m.Roles)
            .FirstOrDefaultAsync(m => m.HouseholdMemberId == targetMemberId);

        if (targetMember == null)
        {
            result.IsValid = false;
            result.Errors.Add("Target member not found");
            return result;
        }

        // Validate age for Child role
        if (roleType == HouseholdRoleType.Child)
        {
            if (!targetMember.DateOfBirth.HasValue)
            {
                result.IsValid = false;
                result.Errors.Add("Date of birth required for Child role");
            }
            else if (targetMember.Age >= 18)
            {
                result.IsValid = false;
                result.Errors.Add("Child role can only be assigned to members under 18");
            }
        }

        // Validate only one admin
        if (roleType == HouseholdRoleType.Admin)
        {
            var existingAdmin = await _context.HouseholdRoles
                .Include(r => r.HouseholdMember)
                .FirstOrDefaultAsync(r => r.HouseholdMember!.HouseholdId == householdId &&
                                         r.RoleType == HouseholdRoleType.Admin &&
                                         r.IsActive &&
                                         r.HouseholdMemberId != targetMemberId);

            if (existingAdmin != null)
            {
                result.Warnings.Add($"This will replace the existing admin. Consider using TransferAdminRole instead.");
            }
        }

        return result;
    }

    public async Task<bool> ValidateHouseholdRolesAsync(int householdId)
    {
        var activeAdmins = await _context.HouseholdRoles
            .Include(r => r.HouseholdMember)
            .CountAsync(r => r.HouseholdMember!.HouseholdId == householdId &&
                           r.RoleType == HouseholdRoleType.Admin &&
                           r.IsActive);

        if (activeAdmins == 0)
        {
            await _auditService.LogAsync(
                "NoAdminDetected",
                "Household",
                householdId,
                "Household has no active admin");
            return false;
        }

        if (activeAdmins > 1)
        {
            await _auditService.LogAsync(
                "MultipleAdminsDetected",
                "Household",
                householdId,
                $"Household has {activeAdmins} active admins");
            return false;
        }

        return true;
    }

    // ==================== Utility ====================

    public async Task<IEnumerable<RolePrivilege>> GetRolePermissionsAsync(HouseholdRoleType roleType)
    {
        return await _context.RolePermissions
            .Where(p => p.RoleType == roleType)
            .ToListAsync();
    }

    public bool CanDelegate(HouseholdRoleType delegatorRole, HouseholdRoleType targetRole)
    {
        // Admin can delegate any role except Admin itself
        if (delegatorRole == HouseholdRoleType.Admin)
            return targetRole != HouseholdRoleType.Admin;

        // FullAccess can delegate Editor, ViewOnly, Limited (with approval)
        if (delegatorRole == HouseholdRoleType.FullAccess)
            return targetRole is HouseholdRoleType.Editor or HouseholdRoleType.ViewOnly or HouseholdRoleType.Limited;

        // Others cannot delegate
        return false;
    }

    public int GetMaxDelegationPeriod(HouseholdRoleType delegatorRole, HouseholdRoleType targetRole)
    {
        return (delegatorRole, targetRole) switch
        {
            (HouseholdRoleType.Admin, HouseholdRoleType.FullAccess) => 90,
            (HouseholdRoleType.Admin, HouseholdRoleType.Editor) => 90,
            (HouseholdRoleType.Admin, HouseholdRoleType.ViewOnly) => 365,
            (HouseholdRoleType.Admin, HouseholdRoleType.Limited) => 90,
            (HouseholdRoleType.FullAccess, HouseholdRoleType.Editor) => 30,
            (HouseholdRoleType.FullAccess, HouseholdRoleType.ViewOnly) => 90,
            (HouseholdRoleType.FullAccess, HouseholdRoleType.Limited) => 30,
            _ => 30
        };
    }

    public bool RequiresApprovalForDelegation(HouseholdRoleType delegatorRole, HouseholdRoleType targetRole)
    {
        // FullAccess delegating Editor or Limited requires admin approval
        if (delegatorRole == HouseholdRoleType.FullAccess &&
            (targetRole == HouseholdRoleType.Editor || targetRole == HouseholdRoleType.Limited))
        {
            return true;
        }

        return false;
    }

    // ==================== Private Helper Methods ====================

    private bool GetDefaultPermission(HouseholdRoleType roleType, string permissionKey)
    {
        // Default permission matrix based on specification
        return (roleType, permissionKey) switch
        {
            // Admin has all permissions
            (HouseholdRoleType.Admin, _) => true,

            // FullAccess permissions
            (HouseholdRoleType.FullAccess, "transaction.view.all") => true,
            (HouseholdRoleType.FullAccess, "transaction.create") => true,
            (HouseholdRoleType.FullAccess, "transaction.edit.all") => true,
            (HouseholdRoleType.FullAccess, "transaction.delete.all") => true,
            (HouseholdRoleType.FullAccess, "budget.view.all") => true,
            (HouseholdRoleType.FullAccess, "budget.create") => true,
            (HouseholdRoleType.FullAccess, "budget.edit.all") => true,

            // Editor permissions
            (HouseholdRoleType.Editor, "transaction.view.all") => true,
            (HouseholdRoleType.Editor, "transaction.create") => true,
            (HouseholdRoleType.Editor, "transaction.edit.own") => true,
            (HouseholdRoleType.Editor, "budget.view.all") => true,
            (HouseholdRoleType.Editor, "budget.create") => true,
            (HouseholdRoleType.Editor, "budget.edit.own") => true,

            // ViewOnly permissions
            (HouseholdRoleType.ViewOnly, "transaction.view.all") => true,
            (HouseholdRoleType.ViewOnly, "budget.view.all") => true,
            (HouseholdRoleType.ViewOnly, "goal.view.all") => true,

            // Limited permissions
            (HouseholdRoleType.Limited, "transaction.view.own") => true,
            (HouseholdRoleType.Limited, "transaction.create") => true,
            (HouseholdRoleType.Limited, "transaction.edit.own") => true,
            (HouseholdRoleType.Limited, "budget.view.own") => true,
            (HouseholdRoleType.Limited, "budget.create") => true,

            // Child permissions
            (HouseholdRoleType.Child, "child_account.view") => true,

            _ => false
        };
    }

    private decimal? GetDefaultAmountLimit(HouseholdRoleType roleType, string permissionKey)
    {
        return (roleType, permissionKey) switch
        {
            // FullAccess limits
            (HouseholdRoleType.FullAccess, "transaction.approve") => 10000m,
            (HouseholdRoleType.FullAccess, "child_account.adjust_balance") => 1000m,

            // Editor limits
            (HouseholdRoleType.Editor, "transaction.create") => 5000m,
            (HouseholdRoleType.Editor, "shared_expense.create") => 5000m,

            // Limited limits
            (HouseholdRoleType.Limited, "transaction.create") => 2000m,

            // Child limits
            (HouseholdRoleType.Child, "child_account.withdrawal") => 500m,

            _ => null
        };
    }
}
