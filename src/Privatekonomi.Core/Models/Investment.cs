namespace Privatekonomi.Core.Models;

public class Investment : ITemporalEntity
{
    public int InvestmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Aktie", "Fond", "ETF", "Certifikat", "Krypto", "P2P-lån"
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Temporal tracking
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // Bank and account information
    public int? BankSourceId { get; set; }
    public BankSource? BankSource { get; set; }
    public string? AccountNumber { get; set; }
    public string? ShortName { get; set; }
    public string? ISIN { get; set; }
    public string? Currency { get; set; }
    public string? Country { get; set; }
    public string? Market { get; set; }
    public string? ChartOfAccountsCode { get; set; }
    
    // Swedish account type specific fields
    /// <summary>
    /// Type of investment account: "ISK" (Investeringssparkonto), "KF" (Kapitalförsäkring), 
    /// "AF" (Aktie- och fondkonto), "Depå" (regular depot), or null if not specified
    /// </summary>
    public string? AccountType { get; set; }
    
    /// <summary>
    /// Calculated schablon tax for ISK/KF accounts (schablonintäkt)
    /// Based on Swedish tax rules: typically 0.375% of capital base per quarter
    /// </summary>
    public decimal? SchablonTax { get; set; }
    
    /// <summary>
    /// Tax year for the schablon tax calculation
    /// </summary>
    public int? SchablonTaxYear { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public decimal TotalValue => Quantity * CurrentPrice;
    public decimal TotalCost => Quantity * PurchasePrice;
    public decimal ProfitLoss => TotalValue - TotalCost;
    public decimal ProfitLossPercentage => TotalCost > 0 ? (ProfitLoss / TotalCost) * 100 : 0;
}
