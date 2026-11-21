namespace Privatekonomi.Core.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public string? AccountNumber { get; set; } // BAS 2026-inspired account number (e.g., "3000", "7010")
    
    // Enhanced fields from proposed model
    public int? ParentId { get; set; } // For hierarchical categories (e.g., "Transport" -> "Public Transport")
    public decimal? DefaultBudgetMonthly { get; set; } // Default monthly budget suggestion
    public bool TaxRelated { get; set; } // Whether category is tax-deductible or affects tax reporting
    public bool IsSystemCategory { get; set; } // Whether this is a default system category
    public bool IsInternalTransferCategory { get; set; }
    public string? OriginalName { get; set; } // Original name for system categories (for reset)
    public string? OriginalColor { get; set; } // Original color for system categories (for reset)
    public string? OriginalAccountNumber { get; set; } // Original account number for system categories (for reset)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<TransactionCategory> TransactionCategories { get; set; } = new List<TransactionCategory>();
}
