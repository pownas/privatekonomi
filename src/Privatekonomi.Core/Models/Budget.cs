namespace Privatekonomi.Core.Models;

public class Budget : ITemporalEntity
{
    public int BudgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BudgetPeriod Period { get; set; }
    public BudgetTemplateType? TemplateType { get; set; }
    public bool RolloverUnspent { get; set; }
    public int? HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Temporal tracking
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public Household? Household { get; set; }
    public ICollection<BudgetCategory> BudgetCategories { get; set; } = new List<BudgetCategory>();
    public ICollection<HouseholdBudgetShare> HouseholdBudgetShares { get; set; } = new List<HouseholdBudgetShare>();
}

public enum BudgetPeriod
{
    Monthly,
    Yearly
}

public enum BudgetTemplateType
{
    Custom = 0,
    ZeroBased = 1,
    FiftyThirtyTwenty = 2,
    Envelope = 3,
    SwedishFamily = 4,      // Swedish household budget for families (Länsförsäkringar style)
    SwedishSingle = 5       // Swedish household budget for singles (Länsförsäkringar style)
}
