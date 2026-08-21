namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a shareable link to a goal for view-only access
/// </summary>
public class GoalShare
{
    public int GoalShareId { get; set; }
    
    /// <summary>
    /// The goal being shared (can be either Goal or SharedGoal)
    /// </summary>
    public int? GoalId { get; set; }
    public Goal? Goal { get; set; }
    
    public int? SharedGoalId { get; set; }
    public SharedGoal? SharedGoal { get; set; }
    
    /// <summary>
    /// Unique token for accessing the shared goal
    /// </summary>
    public string ShareToken { get; set; } = string.Empty;
    
    /// <summary>
    /// User who created the share
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// What information to show in the share
    /// </summary>
    public bool ShowCurrentAmount { get; set; } = true;
    public bool ShowTargetAmount { get; set; } = true;
    public bool ShowTargetDate { get; set; } = true;
    public bool ShowTransactions { get; set; }
    public bool ShowOwnerName { get; set; }
    
    /// <summary>
    /// Optional expiration date for the share link
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Number of times the link has been viewed
    /// </summary>
    public int ViewCount { get; set; }
}
