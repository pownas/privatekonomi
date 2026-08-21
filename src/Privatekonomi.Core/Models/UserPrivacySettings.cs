namespace Privatekonomi.Core.Models;

/// <summary>
/// User's privacy settings for social features - GDPR compliant opt-in system
/// </summary>
public class UserPrivacySettings
{
    public int UserPrivacySettingsId { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Master opt-in for all social features
    /// </summary>
    public bool EnableSocialFeatures { get; set; }
    
    /// <summary>
    /// Allow creating shareable links to goals
    /// </summary>
    public bool AllowGoalSharing { get; set; }
    
    /// <summary>
    /// Allow joining savings groups
    /// </summary>
    public bool AllowSavingsGroups { get; set; }
    
    /// <summary>
    /// Allow appearing in leaderboards
    /// </summary>
    public bool AllowLeaderboards { get; set; }
    
    /// <summary>
    /// Allow community comparison (always anonymized)
    /// </summary>
    public bool AllowCommunityComparison { get; set; }
    
    /// <summary>
    /// Default anonymization preference
    /// </summary>
    public bool AnonymousByDefault { get; set; } = true;
    
    /// <summary>
    /// Whether to show real name in groups
    /// </summary>
    public bool ShowRealNameInGroups { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Date when user last reviewed and accepted privacy terms
    /// </summary>
    public DateTime? PrivacyTermsAcceptedAt { get; set; }
}
