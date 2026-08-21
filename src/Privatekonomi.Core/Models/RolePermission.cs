namespace Privatekonomi.Core.Models;

/// <summary>
/// Defines permissions for each role type (for future fine-grained control)
/// </summary>
public class RolePrivilege
{
    public int RolePermissionId { get; set; }
    public HouseholdRoleType RoleType { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    
    /// <summary>
    /// Optional amount limit for financial permissions (null = no limit)
    /// </summary>
    public decimal? AmountLimit { get; set; }
}
