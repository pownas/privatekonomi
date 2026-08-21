namespace Privatekonomi.Core.Models;

public class Loan : ITemporalEntity
{
    public int LoanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Bolån, CSN-lån, Privatlån, Kreditkort, BNPL/Avbetalning
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; } // Ränta i procent
    public decimal Amortization { get; set; } // Amortering per månad
    public string Currency { get; set; } = "SEK";
    public DateTime? StartDate { get; set; }
    public DateTime? MaturityDate { get; set; } // When loan is expected to be fully paid
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Temporal tracking
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // Bolån (Mortgage) specific fields
    /// <summary>
    /// Property address for mortgage loans
    /// </summary>
    public string? PropertyAddress { get; set; }
    
    /// <summary>
    /// Property value (for calculating LTV)
    /// </summary>
    public decimal? PropertyValue { get; set; }
    
    /// <summary>
    /// Loan-to-Value ratio (belåningsgrad) as percentage
    /// </summary>
    public decimal? LTV => PropertyValue.HasValue && PropertyValue.Value > 0 
        ? (Amount / PropertyValue.Value) * 100 
        : null;
    
    /// <summary>
    /// Loan provider (bank name)
    /// </summary>
    public string? LoanProvider { get; set; }
    
    /// <summary>
    /// Whether interest rate is fixed or variable
    /// </summary>
    public bool IsFixedRate { get; set; }
    
    /// <summary>
    /// Date when fixed rate period ends (bindningstid)
    /// </summary>
    public DateTime? RateResetDate { get; set; }
    
    /// <summary>
    /// Binding period in months (3, 12, 24, 36, 60 months, etc.)
    /// </summary>
    public int? BindingPeriodMonths { get; set; }
    
    // CSN-lån specific fields
    /// <summary>
    /// CSN loan type: "Studiemedel", "Studiemedelsränta"
    /// </summary>
    public string? CsnLoanType { get; set; }
    
    /// <summary>
    /// Study year when loan was taken
    /// </summary>
    public int? CsnStudyYear { get; set; }
    
    /// <summary>
    /// Monthly payment to CSN
    /// </summary>
    public decimal? CsnMonthlyPayment { get; set; }
    
    /// <summary>
    /// Remaining amount owed to CSN
    /// </summary>
    public decimal? CsnRemainingAmount { get; set; }
    
    /// <summary>
    /// Last date CSN information was updated
    /// </summary>
    public DateTime? CsnLastUpdate { get; set; }
  
    // Credit card specific fields
    public decimal? CreditLimit { get; set; } // Kreditgräns för kreditkort
    public decimal? MinimumPayment { get; set; } // Minimum månadsinbetalning
    
    // BNPL/Installment specific fields
    public int? InstallmentMonths { get; set; } // Antal månader för avbetalning
    public decimal? InstallmentFee { get; set; } // Fast avgift för avbetalning
    
    // Extra payment tracking
    public decimal? ExtraMonthlyPayment { get; set; } // Extra amortering per månad
    
    // Priority for debt payoff strategies
    public int Priority { get; set; } // 0 = ingen prioritet, högre nummer = högre prioritet
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Computed properties (not stored in DB)
    public decimal CurrentBalance => Amount; // För framtida implementering av faktisk saldo
    public decimal? UtilizationRate => CreditLimit.HasValue && CreditLimit.Value > 0 
        ? (Amount / CreditLimit.Value) * 100 
        : null;
}
