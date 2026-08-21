using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a recurring subscription with price tracking
/// </summary>
public class Subscription
{
    public int SubscriptionId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "SEK";
    
    /// <summary>
    /// Billing frequency: Monthly, Yearly, Quarterly
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string BillingFrequency { get; set; } = "Monthly";
    
    /// <summary>
    /// Next billing date
    /// </summary>
    public DateTime? NextBillingDate { get; set; }
    
    /// <summary>
    /// Date subscription started
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Date subscription ends/ended
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Whether subscription is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Category for subscription expense
    /// </summary>
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    /// <summary>
    /// Link to cancel subscription
    /// </summary>
    [MaxLength(500)]
    public string? CancellationUrl { get; set; }
    
    /// <summary>
    /// Link to manage subscription
    /// </summary>
    [MaxLength(500)]
    public string? ManagementUrl { get; set; }
    
    /// <summary>
    /// Email or username for subscription account
    /// </summary>
    [MaxLength(200)]
    public string? AccountEmail { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Deadline for cancelling subscription (e.g., 30 days before renewal)
    /// </summary>
    public DateTime? CancellationDeadline { get; set; }
    
    /// <summary>
    /// Number of days before renewal when cancellation is required
    /// </summary>
    public int? CancellationNoticeDays { get; set; }
    
    /// <summary>
    /// Last date the subscription was actively used
    /// </summary>
    public DateTime? LastUsedDate { get; set; }
    
    /// <summary>
    /// Comma-separated list of people/family members sharing this subscription
    /// </summary>
    [MaxLength(500)]
    public string? SharedWith { get; set; }
    
    /// <summary>
    /// Whether this subscription was auto-detected from transaction patterns
    /// </summary>
    public bool AutoDetected { get; set; }
    
    /// <summary>
    /// The transaction ID that led to auto-detection (if applicable)
    /// </summary>
    public int? DetectedFromTransactionId { get; set; }
    
    /// <summary>
    /// Price change history
    /// </summary>
    public List<SubscriptionPriceHistory> PriceHistory { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
