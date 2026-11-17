using Privatekonomi.Core.Data;

using Microsoft.EntityFrameworkCore;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for calculating schablonintäkt (standard income) for ISK/KF accounts
/// ISK = Investeringssparkonto (Investment Savings Account)
/// KF = Kapitalförsäkring (Capital Insurance)
/// </summary>
public interface IISKTaxCalculator
{
    /// <summary>
    /// Calculate schablonintäkt for ISK/KF accounts for a specific year
    /// </summary>
    Task<decimal> CalculateSchablonTaxAsync(int householdId, int taxYear);
    
    /// <summary>
    /// Calculate schablonintäkt for a specific investment
    /// </summary>
    decimal CalculateSchablonTaxForInvestment(decimal capitalBase, int taxYear);
    
    /// <summary>
    /// Get current government lending rate (statslåneräntan) for a year
    /// This is used in the calculation of schablonintäkt
    /// </summary>
    decimal GetGovernmentLendingRate(int taxYear);
}

public class ISKTaxCalculator : IISKTaxCalculator
{
    private readonly PrivatekonomyContext _context;
    
    // Swedish tax rules for ISK/KF
    // Schablonintäkt = Kapitalunderlaget * (Statslåneräntan + 1%) * 30%
    // The government lending rate changes each year
    
    // Historical government lending rates (statslåneräntan)
    private readonly Dictionary<int, decimal> _governmentLendingRates = new()
    {
        { 2024, 0.0284m }, // 2.84%
        { 2023, 0.0084m }, // 0.84%
        { 2022, 0.0000m }, // 0.00%
        { 2021, 0.0000m }, // 0.00%
        { 2020, 0.0000m }, // 0.00%
    };
    
    public ISKTaxCalculator(PrivatekonomyContext context)
    {
        _context = context;
    }
    
    public async Task<decimal> CalculateSchablonTaxAsync(int householdId, int taxYear)
    {
        // Get all ISK/KF investments
        var investments = await _context.Investments
            .Where(i => i.AccountType == "ISK" || i.AccountType == "KF")
            .ToListAsync();
        
        decimal totalTax = 0;
        
        foreach (var investment in investments)
        {
            var capitalBase = investment.TotalValue;
            var tax = CalculateSchablonTaxForInvestment(capitalBase, taxYear);
            
            // Update the investment with calculated tax
            investment.SchablonTax = tax;
            investment.SchablonTaxYear = taxYear;
            
            totalTax += tax;
        }
        
        await _context.SaveChangesAsync();
        
        return totalTax;
    }
    
    public decimal CalculateSchablonTaxForInvestment(decimal capitalBase, int taxYear)
    {
        var governmentRate = GetGovernmentLendingRate(taxYear);
        
        // Formula: Kapitalunderlaget * (Statslåneräntan + 1%) * 30%
        var taxableBase = capitalBase * (governmentRate + 0.01m);
        var tax = taxableBase * 0.30m;
        
        return tax;
    }
    
    public decimal GetGovernmentLendingRate(int taxYear)
    {
        if (_governmentLendingRates.TryGetValue(taxYear, out var rate))
        {
            return rate;
        }
        
        // Default to most recent year if not found
        var maxYear = _governmentLendingRates.Keys.Max();
        return _governmentLendingRates[maxYear];
    }
}

/// <summary>
/// Report for ISK tax calculation
/// </summary>
public class ISKTaxReport
{
    public int TaxYear { get; set; }
    public decimal GovernmentLendingRate { get; set; }
    public decimal TotalCapitalBase { get; set; }
    public decimal TotalSchablonTax { get; set; }
    public List<ISKInvestmentTaxDetail> Details { get; set; } = new();
}

public class ISKInvestmentTaxDetail
{
    public string InvestmentName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal CapitalBase { get; set; }
    public decimal SchablonTax { get; set; }
}
