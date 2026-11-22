using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service interface for managing automatic categorization rules.
/// </summary>
public interface ICategoryRuleService
{
    /// <summary>
    /// Gets all categorization rules for a user, ordered by priority (descending).
    /// Includes system rules and user's own rules/overrides.
    /// </summary>
    Task<IEnumerable<CategoryRule>> GetAllRulesAsync(string? userId = null);
    
    /// <summary>
    /// Gets active categorization rules for a user, ordered by priority (descending).
    /// User overrides take precedence over system rules.
    /// </summary>
    Task<IEnumerable<CategoryRule>> GetActiveRulesAsync(string? userId = null);
    
    /// <summary>
    /// Gets a categorization rule by ID.
    /// </summary>
    Task<CategoryRule?> GetRuleByIdAsync(int id);
    
    /// <summary>
    /// Creates a new categorization rule (user rule or admin system rule).
    /// </summary>
    Task<CategoryRule> CreateRuleAsync(CategoryRule rule, string? userId = null, bool isAdmin = false);
    
    /// <summary>
    /// Updates an existing categorization rule.
    /// System rules can only be updated by admins.
    /// </summary>
    Task<CategoryRule> UpdateRuleAsync(CategoryRule rule, string? userId = null, bool isAdmin = false);
    
    /// <summary>
    /// Deletes a categorization rule.
    /// System rules cannot be deleted, only overridden.
    /// </summary>
    Task DeleteRuleAsync(int id, string? userId = null, bool isAdmin = false);
    
    /// <summary>
    /// Creates a user override for a system rule.
    /// </summary>
    Task<CategoryRule> CreateOverrideAsync(int systemRuleId, CategoryRule overrideRule, string userId);
    
    /// <summary>
    /// Restores a system rule by removing the user's override.
    /// </summary>
    Task RestoreSystemRuleAsync(int systemRuleId, string userId);
    
    /// <summary>
    /// Finds the first matching category rule for a transaction.
    /// Returns null if no rule matches.
    /// </summary>
    Task<CategoryRule?> FindMatchingRuleAsync(
        string description,
        string? payee = null,
        string? userId = null,
        decimal? amount = null);
    
    /// <summary>
    /// Applies categorization rules to a transaction.
    /// Returns the category ID if a rule matches, otherwise null.
    /// </summary>
    Task<int?> ApplyCategoryRulesAsync(
        string description,
        string? payee = null,
        string? userId = null,
        decimal? amount = null);
    
    /// <summary>
    /// Applies categorization rules to multiple transactions in bulk.
    /// Returns a dictionary mapping transaction IDs to suggested category IDs.
    /// </summary>
    Task<Dictionary<int, int>> ApplyRulesToTransactionsAsync(IEnumerable<int> transactionIds, string? userId = null);
}
