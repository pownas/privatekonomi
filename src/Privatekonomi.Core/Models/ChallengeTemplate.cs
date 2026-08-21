namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a predefined challenge template that users can start
/// </summary>
public class ChallengeTemplate
{
    public int ChallengeTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public ChallengeType Type { get; set; }
    public int DurationDays { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public ChallengeCategory Category { get; set; }
    public decimal? EstimatedSavingsMin { get; set; }
    public decimal? EstimatedSavingsMax { get; set; }
    public decimal? SuggestedTargetAmount { get; set; }
    public string? Rules { get; set; } // JSON or text describing the rules
    public List<string> Tags { get; set; } = new List<string>();
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Converts this template into a new SavingsChallenge for a user
    /// </summary>
    public SavingsChallenge ToChallenge()
    {
        return new SavingsChallenge
        {
            Name = Name,
            Description = Description,
            Icon = Icon,
            Type = Type,
            DurationDays = DurationDays,
            Difficulty = Difficulty,
            Category = Category,
            EstimatedSavingsMin = EstimatedSavingsMin,
            EstimatedSavingsMax = EstimatedSavingsMax,
            TargetAmount = SuggestedTargetAmount ?? ((EstimatedSavingsMin ?? 0) + (EstimatedSavingsMax ?? 0)) / 2,
            StartDate = DateTime.UtcNow,
            Status = ChallengeStatus.Active
        };
    }
}
