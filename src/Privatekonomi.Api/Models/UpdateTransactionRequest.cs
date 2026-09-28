using System.ComponentModel.DataAnnotations;
using Privatekonomi.Core.Models;
using System.Text.Json.Serialization;
using Privatekonomi.Core.Serialization;

namespace Privatekonomi.Api.Models;

/// <summary>
/// Request model for updating a transaction with all editable fields
/// </summary>
public class UpdateTransactionRequest
{
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [JsonConverter(typeof(DateOnlyDateTimeJsonConverter))]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Payee cannot exceed 200 characters")]
    public string? Payee { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
    public string? Tags { get; set; }

    /// <summary>
    /// Categories with their split amounts
    /// </summary>
    public List<TransactionCategoryDto>? Categories { get; set; }

    /// <summary>
    /// The UpdatedAt timestamp from the client for optimistic locking
    /// If provided, must match the current UpdatedAt in the database
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for transaction category with amount
/// </summary>
public class TransactionCategoryDto
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}
