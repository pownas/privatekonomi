namespace Privatekonomi.Core.Models;

public class BankSource : ITemporalEntity
{
    public int BankSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public string? Logo { get; set; }
    
    // Enhanced fields from proposed Account model
    public string AccountType { get; set; } = "checking"; // checking, savings, credit_card, investment, cash, loan, pension
    public string Currency { get; set; } = "SEK"; // Account currency
    public string? Institution { get; set; } // Bank institution name
    public string? AccountNumber { get; set; } // Account number (e.g., clearing number + account number)
    public string? ClearingNumber { get; set; } // Swedish clearing number (clearingnummer)
    public string? ChartOfAccountsCode { get; set; } // Mapping to chart of accounts (kontoplan) for accounting integration
    public decimal InitialBalance { get; set; } // Starting balance when account was opened
    public DateTime? OpenedDate { get; set; } // When account was opened
    public DateTime? ClosedDate { get; set; } // When account was closed (null if active)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Temporal tracking
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }
    
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    
    // Computed property for current balance (not stored in DB)
    public decimal CurrentBalance => InitialBalance + Transactions.Sum(t => t.IsIncome ? t.Amount : -t.Amount);
}
