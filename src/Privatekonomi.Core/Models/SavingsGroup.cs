namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a group where members can support each other in saving
/// </summary>
public class SavingsGroup
{
    public int SavingsGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// Group creator/owner
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser? CreatedByUser { get; set; }
    
    /// <summary>
    /// Type of group (Family, Friends, Community, etc.)
    /// </summary>
    public SavingsGroupType GroupType { get; set; } = SavingsGroupType.Friends;
    
    /// <summary>
    /// Privacy level for the group
    /// </summary>
    public GroupPrivacyLevel PrivacyLevel { get; set; } = GroupPrivacyLevel.Private;
    
    /// <summary>
    /// Whether members are shown anonymously
    /// </summary>
    public bool AnonymousMembers { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<SavingsGroupMember> Members { get; set; } = new List<SavingsGroupMember>();
    public ICollection<GroupComment> Comments { get; set; } = new List<GroupComment>();
    public ICollection<GroupGoal> GroupGoals { get; set; } = new List<GroupGoal>();
}

public enum SavingsGroupType
{
    Family,
    Friends,
    Community,
    Challenge,
    Other
}

public enum GroupPrivacyLevel
{
    Private,      // Only invited members
    Restricted,   // Members can invite others
    Public        // Anyone can join
}
