namespace Privatekonomi.Core.Models;

public class Transaction : ITemporalEntity
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsIncome { get; set; }
    public int? BankSourceId { get; set; }
    
    // Temporal tracking
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    
    // Enhanced fields from proposed model
    public string Currency { get; set; } = "SEK"; // Default to Swedish Krona
    public string? Payee { get; set; } // Who received/sent the payment
    public string? Tags { get; set; } // Comma-separated tags for flexible categorization
    public string? Notes { get; set; } // User notes/comments about the transaction
    public int? RecurringId { get; set; } // Link to recurring transaction template
    public bool Imported { get; set; } // Whether transaction was imported from CSV/API
    public string? ImportSource { get; set; } // Source of import (e.g., "ICA-banken CSV", "Swedbank API")
    
    /// <summary>
    /// Account number as read from the import source (e.g., CSV file).
    /// Used to auto-link transactions to the correct BankSource during import.
    /// </summary>
    public string? AccountNumber { get; set; }
    
    /// <summary>
    /// Clearing number (clearingnummer) as read from the import source.
    /// Used together with AccountNumber to identify the correct bank account.
    /// </summary>
    public string? ClearingNumber { get; set; }
    public bool Cleared { get; set; } // Whether transaction has been reconciled/cleared
    public bool IsLocked { get; set; } // Whether transaction is locked and cannot be edited
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // User ownership
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
  
  
    // Swedish payment method specific fields
    /// <summary>
    /// Payment method: "Swish", "Autogiro", "E-faktura", "Banköverföring", "Kort", "Kontant"
    /// </summary>
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Bankgiro number for the recipient (if applicable)
    /// </summary>
    public string? RecipientBankgiro { get; set; }
    
    /// <summary>
    /// Plusgiro number for the recipient (if applicable)
    /// </summary>
    public string? RecipientPlusgiro { get; set; }
    
    /// <summary>
    /// Invoice number (for e-faktura)
    /// </summary>
    public string? InvoiceNumber { get; set; }
    
    /// <summary>
    /// OCR number (Optical Character Recognition reference number for Swedish payments)
    /// </summary>
    public string? OCR { get; set; }
    
    /// <summary>
    /// Whether this is a recurring payment (e.g., Autogiro subscription)
    /// </summary>
    public bool IsRecurring { get; set; }
    
    /// <summary>
    /// Link to pocket if this transaction is from a specific savings pocket
    /// </summary>
    public int? PocketId { get; set; }
    
    /// <summary>
    /// Link to household if this transaction belongs to a specific household
    /// Null value indicates a personal transaction not linked to any household
    /// </summary>
    public int? HouseholdId { get; set; }
    
    public BankSource? BankSource { get; set; }
    public Pocket? Pocket { get; set; }
    public Household? Household { get; set; }
    public ICollection<TransactionCategory> TransactionCategories { get; set; } = new List<TransactionCategory>();
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
