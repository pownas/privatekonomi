namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a member of a savings group
/// </summary>
public class SavingsGroupMember
{
    public int SavingsGroupMemberId { get; set; }
    
    public int SavingsGroupId { get; set; }
    public SavingsGroup? SavingsGroup { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Role within the group
    /// </summary>
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
    
    /// <summary>
    /// Display name in the group (for anonymity)
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Whether to show real name or use anonymous display
    /// </summary>
    public bool ShowRealName { get; set; } = true;
    
    /// <summary>
    /// Privacy settings for what to share
    /// </summary>
    public bool ShareProgress { get; set; } = true;
    public bool ShareGoalCount { get; set; } = true;
    public bool ShareTotalSavings { get; set; }
    
    public DateTime JoinedAt { get; set; }
    public GroupMemberStatus Status { get; set; } = GroupMemberStatus.Pending;
}

public enum GroupMemberRole
{
    Owner,
    Admin,
    Member
}

public enum GroupMemberStatus
{
    Pending,
    Active,
    Inactive
}
