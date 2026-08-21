namespace Privatekonomi.Core.Models;

/// <summary>
/// Links a user's goal to a savings group for sharing and comparison
/// </summary>
public class GroupGoal
{
    public int GroupGoalId { get; set; }
    
    public int SavingsGroupId { get; set; }
    public SavingsGroup? SavingsGroup { get; set; }
    
    /// <summary>
    /// The user's personal goal being shared in the group
    /// </summary>
    public int GoalId { get; set; }
    public Goal? Goal { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Whether to show this goal anonymously in the group
    /// </summary>
    public bool IsAnonymous { get; set; }
    
    /// <summary>
    /// Custom display name for the goal in the group
    /// </summary>
    public string? DisplayName { get; set; }
    
    public DateTime SharedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
