namespace Privatekonomi.Core.Models;

public class InvestmentAccount : ITemporalEntity
{
    public int InvestmentAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "Annan";
    public string AccountType { get; set; } = "AF";
    public string? AccountNumber { get; set; }
    public string Currency { get; set; } = "SEK";
    public bool IsFundRobot { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public decimal? InitialContributions { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateTime? LastUpdated { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public ICollection<Investment> Investments { get; set; } = new List<Investment>();

    public decimal Contributions => InitialContributions ?? Investments.Sum(i => i.TotalCost);
    public decimal MarketValue => IsFundRobot
        ? CurrentValue ?? 0
        : Investments.Sum(i => i.TotalValue);
    public decimal ProfitLoss => MarketValue - Contributions;
    public decimal ProfitLossPercentage => Contributions > 0
        ? ProfitLoss / Contributions * 100
        : 0;
}
