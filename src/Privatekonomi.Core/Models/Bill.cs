using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a bill with reminder and payment tracking
/// </summary>
public class Bill
{
    public int BillId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "SEK";
    
    /// <summary>
    /// Bill issue date
    /// </summary>
    [Required]
    public DateTime IssueDate { get; set; }
    
    /// <summary>
    /// Bill due date
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Date bill was paid
    /// </summary>
    public DateTime? PaidDate { get; set; }
    
    /// <summary>
    /// Bill status: Pending, Paid, Overdue, Cancelled
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// Whether bill is recurring
    /// </summary>
    public bool IsRecurring { get; set; }
    
    /// <summary>
    /// Recurring frequency if applicable
    /// </summary>
    [MaxLength(50)]
    public string? RecurringFrequency { get; set; }
    
    /// <summary>
    /// Payment method: Autogiro, E-invoice, Manual
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Invoice/bill number
    /// </summary>
    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }
    
    /// <summary>
    /// OCR number for Swedish bills
    /// </summary>
    [MaxLength(50)]
    public string? OCR { get; set; }
    
    /// <summary>
    /// Bankgiro number
    /// </summary>
    [MaxLength(20)]
    public string? Bankgiro { get; set; }
    
    /// <summary>
    /// Plusgiro number
    /// </summary>
    [MaxLength(20)]
    public string? Plusgiro { get; set; }
    
    /// <summary>
    /// Payee/creditor name
    /// </summary>
    [MaxLength(200)]
    public string? Payee { get; set; }
    
    /// <summary>
    /// Category for bill expense
    /// </summary>
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    /// <summary>
    /// Household sharing if applicable
    /// </summary>
    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }
    
    /// <summary>
    /// Related transaction if paid
    /// </summary>
    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    
    /// <summary>
    /// Path to bill document/PDF
    /// </summary>
    [MaxLength(500)]
    public string? DocumentPath { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Reminders for this bill
    /// </summary>
    public List<BillReminder> Reminders { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
