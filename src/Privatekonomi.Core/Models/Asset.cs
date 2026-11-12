namespace Privatekonomi.Core.Models;

public class Asset : ITemporalEntity
{
    public int AssetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Fastighet, Fordon, Möbler, Elektronik, Övrigt
    public string? Description { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal CurrentValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string Currency { get; set; } = "SEK";
    public string? Location { get; set; } // For real estate or where item is stored
    public string? ChartOfAccountsCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Temporal tracking
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Computed properties
    public decimal ValueChange => CurrentValue - PurchaseValue;
    public decimal ValueChangePercentage => PurchaseValue > 0 ? (ValueChange / PurchaseValue) * 100 : 0;
}
