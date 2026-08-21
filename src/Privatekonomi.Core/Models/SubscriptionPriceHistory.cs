using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// Tracks price changes for subscriptions
/// </summary>
public class SubscriptionPriceHistory
{
    public int SubscriptionPriceHistoryId { get; set; }
    
    public int SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }
    
    [Required]
    public decimal OldPrice { get; set; }
    
    [Required]
    public decimal NewPrice { get; set; }
    
    [Required]
    public DateTime ChangeDate { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// Whether user was notified about this price change
    /// </summary>
    public bool NotificationSent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
