namespace Privatekonomi.Core.Models;

public class SavingsChallenge
{
    public int SavingsChallengeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChallengeType Type { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public int DurationDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Active;
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // New properties for enhanced gamification
    public string Icon { get; set; } = "🎯";
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Individual;
    public decimal? EstimatedSavingsMin { get; set; }
    public decimal? EstimatedSavingsMax { get; set; }
    public bool IsTemplate { get; set; } // True for predefined challenge templates
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Navigation property for progress tracking
    public ICollection<SavingsChallengeProgress> ProgressEntries { get; set; } = new List<SavingsChallengeProgress>();
    
    // Calculated properties
    public int DaysCompleted => ProgressEntries.Count(p => p.Completed);
    public decimal ProgressPercentage => DurationDays > 0 ? (decimal)DaysCompleted / DurationDays * 100 : 0;
    public bool IsCompleted => Status == ChallengeStatus.Completed;
    public int RemainingDays 
    { 
        get 
        {
            if (EndDate.HasValue)
            {
                var remaining = (EndDate.Value - DateTime.UtcNow).Days;
                return remaining > 0 ? remaining : 0;
            }
            return DurationDays - DaysCompleted;
        }
    }
}

public enum ChallengeType
{
    // Original challenges
    SaveDaily,          // Save X kr/day
    NoRestaurant,       // No restaurant spending
    NoTakeaway,         // No takeaway spending
    NoCoffeeOut,        // No coffee at cafes
    SavePercentOfIncome, // Save X% of income
    SpendingLimit,      // Limit spending in a category
    Custom,             // User-defined challenge
    
    // New short-term challenges (1-4 weeks)
    NoSpendWeekend,     // 🛍️ No shopping or non-essential spending for a weekend
    LunchBox,           // 🍱 Bring lunch to work every day
    BikeOrPublic,       // 🚴 Only bike/public transport, no car/taxi
    SellItems,          // 📦 Sell 5 items online
    ChangeJar,          // 🪙 Save all coins in a jar
    
    // Medium-term challenges (1-3 months)
    NoImpulseBuying,    // 🛒 No spontaneous purchases, only planned shopping
    StreamingDetox,     // 📺 Pause all paid streaming services
    AlcoholFree,        // 🍷 No alcohol for a month
    GiftFree,           // 🎁 No gifts except birthdays/holidays
    HomeGym,            // 🏋️ Cancel gym membership, workout at home
    
    // Long-term challenges (3-6 months)
    SpecificGoal,       // 💰 Save for a specific goal (trip, gadget, etc.)
    HouseholdGoal,      // 🏠 Household-wide savings challenge
    ClimateChallenge,   // 🌍 Eco-friendly spending reduction
    ProgressiveSaving,  // 📈 Save increasing % of income each month
    RandomChallenge,    // 🎲 Weekly random savings challenges
    
    // Social challenges
    SavingsGroup,       // 👥 Group savings challenge with friends
    Leaderboard         // 🥇 Monthly competition with rankings
}

public enum ChallengeStatus
{
    Active,
    Completed,
    Failed,
    Paused
}

public enum DifficultyLevel
{
    VeryEasy = 1,
    Easy = 2,
    Medium = 3,
    Hard = 4,
    VeryHard = 5
}

public enum ChallengeCategory
{
    Individual,
    Social,
    Household,
    Health,
    Environment,
    Minimalism,
    Thematic,
    GoalBased,
    Fun
}
