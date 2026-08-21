namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents an automatic categorization rule for transactions.
/// Rules can use patterns to automatically assign categories to transactions based on their description or payee.
/// </summary>
public class CategoryRule
{
    public int CategoryRuleId { get; set; }
    
    /// <summary>
    /// The pattern to match against transaction descriptions or payee names.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of pattern matching to use.
    /// </summary>
    public PatternMatchType MatchType { get; set; } = PatternMatchType.Contains;
    
    /// <summary>
    /// The category ID to assign when the pattern matches.
    /// </summary>
    public int CategoryId { get; set; }
    
    /// <summary>
    /// Priority of the rule (higher priority rules are evaluated first).
    /// Useful when multiple rules could match the same transaction.
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Whether the rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Optional description of the rule for documentation purposes.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Case-sensitive matching (default is case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; set; }
    
    /// <summary>
    /// Field to match against (Description, Payee, or Both).
    /// </summary>
    public MatchField Field { get; set; } = MatchField.Both;
    
    /// <summary>
    /// Type of rule - System (predefined, admin-only) or User (user-created).
    /// </summary>
    public RuleType RuleType { get; set; } = RuleType.User;
    
    /// <summary>
    /// User ID who created this rule (null for system rules).
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Navigation property to the user who created this rule.
    /// </summary>
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// For user rules that override system rules - references the system rule being overridden.
    /// </summary>
    public int? OverridesSystemRuleId { get; set; }
    
    /// <summary>
    /// Navigation property to the system rule being overridden.
    /// </summary>
    public CategoryRule? OverridesSystemRule { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public Category? Category { get; set; }
}

/// <summary>
/// Types of pattern matching supported for categorization rules.
/// </summary>
public enum PatternMatchType
{
    /// <summary>
    /// Pattern must exactly match the entire description/payee (e.g., "ICA Maxi" matches only "ICA Maxi").
    /// </summary>
    Exact,
    
    /// <summary>
    /// Pattern can appear anywhere in the description/payee (e.g., "ICA" matches "ICA Maxi", "ICA Kvantum").
    /// </summary>
    Contains,
    
    /// <summary>
    /// Description/payee must start with the pattern (e.g., "ICA" matches "ICA Maxi" but not "Maxi ICA").
    /// </summary>
    StartsWith,
    
    /// <summary>
    /// Description/payee must end with the pattern (e.g., "Bank" matches "Swedbank" but not "Bank of Sweden").
    /// </summary>
    EndsWith,
    
    /// <summary>
    /// Pattern is a regular expression (e.g., "ICA.*" matches "ICA Maxi", "ICA Kvantum").
    /// </summary>
    Regex
}

/// <summary>
/// Field to match the pattern against.
/// </summary>
public enum MatchField
{
    /// <summary>
    /// Match against transaction description only.
    /// </summary>
    Description,
    
    /// <summary>
    /// Match against payee only.
    /// </summary>
    Payee,
    
    /// <summary>
    /// Match against both description and payee (rule matches if either field matches).
    /// </summary>
    Both
}

/// <summary>
/// Type of categorization rule.
/// </summary>
public enum RuleType
{
    /// <summary>
    /// System rule - predefined by the system, can only be modified by admins.
    /// Users can override these rules but cannot delete them.
    /// </summary>
    System,
    
    /// <summary>
    /// User rule - created by a user, can be modified or deleted by the user.
    /// </summary>
    User
}
