namespace Privatekonomi.Core.Models;

public class HouseholdMember
{
    public int HouseholdMemberId { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShareBalanceSheetConsent { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LeftDate { get; set; }
    
    // Link to user account (optional - member may exist without user account)
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // RBAC - Date of Birth for age-based restrictions
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// Calculates age in years (null if DateOfBirth not set)
    /// </summary>
    public int? Age 
    { 
        get
        {
            if (!DateOfBirth.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
    
    public Household? Household { get; set; }
    public ICollection<ExpenseShare> ExpenseShares { get; set; } = new List<ExpenseShare>();
    
    // RBAC - Role assignments
    public ICollection<HouseholdRole> Roles { get; set; } = new List<HouseholdRole>();
}
