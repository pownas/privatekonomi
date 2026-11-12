namespace Privatekonomi.Core.Models;

public class Household
{
    public int HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool ShareBalanceSheet { get; set; }
    
    public ICollection<HouseholdMember> Members { get; set; } = new List<HouseholdMember>();
    public ICollection<SharedExpense> SharedExpenses { get; set; } = new List<SharedExpense>();
    public ICollection<HouseholdActivity> Activities { get; set; } = new List<HouseholdActivity>();
    public ICollection<HouseholdTask> Tasks { get; set; } = new List<HouseholdTask>();
    public ICollection<DebtSettlement> DebtSettlements { get; set; } = new List<DebtSettlement>();
}
