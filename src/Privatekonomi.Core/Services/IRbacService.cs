using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for managing role-based access control (RBAC) in households
/// </summary>
public interface IRbacService
{
    // ==================== Role Management ====================
    
    /// <summary>
    /// Gets the active role for a user in a specific household
    /// </summary>
    Task<HouseholdRole?> GetUserRoleInHouseholdAsync(string userId, int householdId);
    
    /// <summary>
    /// Checks if a user has a specific role type in a household
    /// </summary>
    Task<bool> HasRoleAsync(string userId, int householdId, HouseholdRoleType roleType);
    
    /// <summary>
    /// Checks if a user has at least the minimum required role (role hierarchy)
    /// </summary>
    Task<bool> HasMinimumRoleAsync(string userId, int householdId, HouseholdRoleType minimumRole);
    
    /// <summary>
    /// Assigns a role to a household member
    /// </summary>
    Task<HouseholdRole> AssignRoleAsync(string assignerUserId, int householdMemberId, HouseholdRoleType roleType);
    
    /// <summary>
    /// Removes/revokes a role from a household member
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, int roleId, string? reason = null);
    
    /// <summary>
    /// Transfers admin role from current admin to another member
    /// </summary>
    Task<HouseholdRole> TransferAdminRoleAsync(string currentAdminUserId, string newAdminUserId, int householdId);
    
    // ==================== Permission Checks ====================
    
    /// <summary>
    /// Checks if user has a specific permission in a household
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, int householdId, string permissionKey);
    
    /// <summary>
    /// Checks if user can perform an action with optional amount limit validation
    /// </summary>
    Task<bool> CanPerformActionAsync(string userId, int householdId, string action, decimal? amount = null);
    
    /// <summary>
    /// Performs a detailed permission check with context
    /// </summary>
    Task<PermissionCheckResult> CheckPermissionAsync(string userId, int householdId, string permissionKey, object? context = null);
    
    // ==================== Delegation ====================
    
    /// <summary>
    /// Delegates a role to another user for a limited time
    /// </summary>
    Task<HouseholdRole> DelegateRoleAsync(
        string delegatorUserId, 
        string targetUserId, 
        int householdId, 
        HouseholdRoleType roleType, 
        DateTime? endDate = null);
    
    /// <summary>
    /// Revokes a delegated role
    /// </summary>
    Task<bool> RevokeDelegationAsync(string userId, int delegationId, string? reason = null);
    
    /// <summary>
    /// Gets all active delegations in a household
    /// </summary>
    Task<IEnumerable<HouseholdRole>> GetActiveDelegationsAsync(int householdId);
    
    /// <summary>
    /// Gets delegations that will expire soon (within days)
    /// </summary>
    Task<IEnumerable<HouseholdRole>> GetExpiringDelegationsAsync(int days = 7);
    
    // ==================== Validation ====================
    
    /// <summary>
    /// Validates if a role can be assigned to a member
    /// </summary>
    Task<RoleValidationResult> ValidateRoleAssignmentAsync(
        string userId, 
        int householdId, 
        int targetMemberId, 
        HouseholdRoleType roleType);
    
    /// <summary>
    /// Validates household role integrity (e.g., exactly one admin)
    /// </summary>
    Task<bool> ValidateHouseholdRolesAsync(int householdId);
    
    // ==================== Utility ====================
    
    /// <summary>
    /// Gets all role permissions for a role type
    /// </summary>
    Task<IEnumerable<RolePrivilege>> GetRolePermissionsAsync(HouseholdRoleType roleType);
    
    /// <summary>
    /// Checks if a delegation is allowed based on delegator's role
    /// </summary>
    bool CanDelegate(HouseholdRoleType delegatorRole, HouseholdRoleType targetRole);
    
    /// <summary>
    /// Gets maximum delegation period in days for a role combination
    /// </summary>
    int GetMaxDelegationPeriod(HouseholdRoleType delegatorRole, HouseholdRoleType targetRole);
    
    /// <summary>
    /// Checks if delegation requires approval
    /// </summary>
    bool RequiresApprovalForDelegation(HouseholdRoleType delegatorRole, HouseholdRoleType targetRole);
}

/// <summary>
/// Result of a permission check operation
/// </summary>
public class PermissionCheckResult
{
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }
    public decimal? AmountLimit { get; set; }
    public bool RequiresApproval { get; set; }
}

/// <summary>
/// Result of role assignment validation
/// </summary>
public class RoleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
