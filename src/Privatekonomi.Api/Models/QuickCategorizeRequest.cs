using System.ComponentModel.DataAnnotations;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Api.Models;

/// <summary>
/// Request model for quickly categorizing a transaction with a single category
/// </summary>
public class QuickCategorizeRequest
{
    [Required(ErrorMessage = "CategoryId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Optional: Whether to create a categorization rule from this transaction
    /// </summary>
    public bool CreateRule { get; set; }

    /// <summary>
    /// Optional: Pattern for the rule (defaults to transaction description if CreateRule is true)
    /// </summary>
    public string? RulePattern { get; set; }
}

/// <summary>
/// Response model for quick categorization
/// </summary>
public class QuickCategorizeResponse
{
    /// <summary>
    /// The updated transaction with the new category
    /// </summary>
    public Transaction Transaction { get; set; } = null!;

    /// <summary>
    /// The categorization rule created (if CreateRule was true)
    /// </summary>
    public CategoryRule? CreatedRule { get; set; }
}
