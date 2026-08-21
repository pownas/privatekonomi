using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;
using System.Text;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for generating K4 tax form data (Blankett för kapitalinkomster)
/// K4 is used in Swedish tax returns to report capital gains/losses from securities
/// </summary>
public interface IK4Generator
{
    /// <summary>
    /// Generate K4 report for a specific tax year
    /// </summary>
    Task<K4Report> GenerateK4ReportAsync(int householdId, int taxYear);
    
    /// <summary>
    /// Export K4 data to text format suitable for import into tax software
    /// </summary>
    Task<string> ExportK4ToTextAsync(int householdId, int taxYear);
}

public class K4Report
{
    public int TaxYear { get; set; }
    public List<CapitalGain> Gains { get; set; } = new();
    public decimal TotalGain { get; set; }
    public decimal TotalLoss { get; set; }
    public decimal NetGain => TotalGain - TotalLoss;
    public decimal TaxableGain => NetGain * 0.3m; // 30% tax rate on capital gains in Sweden
    
    public List<K4CategorySummary> CategorySummaries { get; set; } = new();
}

public class K4CategorySummary
{
    public string SecurityType { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalGain { get; set; }
    public decimal TotalLoss { get; set; }
    public decimal NetGain => TotalGain - TotalLoss;
}

public class K4Generator : IK4Generator
{
    private readonly PrivatekonomyContext _context;
    
    public K4Generator(PrivatekonomyContext context)
    {
        _context = context;
    }
    
    public async Task<K4Report> GenerateK4ReportAsync(int householdId, int taxYear)
    {
        // Fetch capital gains for the tax year
        var gains = await _context.Set<CapitalGain>()
            .Include(g => g.Investment)
            .Where(g => g.TaxYear == taxYear)
            .Where(g => !g.IsISK) // ISK accounts have different tax rules (schablonbeskattning)
            .OrderBy(g => g.SaleDate)
            .ToListAsync();
        
        var report = new K4Report
        {
            TaxYear = taxYear,
            Gains = gains,
            TotalGain = gains.Where(g => g.Gain > 0).Sum(g => g.Gain),
            TotalLoss = gains.Where(g => g.Gain < 0).Sum(g => Math.Abs(g.Gain))
        };
        
        // Group by security type for summary
        var summaries = gains
            .GroupBy(g => g.SecurityType)
            .Select(group => new K4CategorySummary
            {
                SecurityType = group.Key,
                TransactionCount = group.Count(),
                TotalGain = group.Where(g => g.Gain > 0).Sum(g => g.Gain),
                TotalLoss = group.Where(g => g.Gain < 0).Sum(g => Math.Abs(g.Gain))
            })
            .ToList();
        
        report.CategorySummaries = summaries;
        
        return report;
    }
    
    public async Task<string> ExportK4ToTextAsync(int householdId, int taxYear)
    {
        var report = await GenerateK4ReportAsync(householdId, taxYear);
        var sb = new StringBuilder();
        
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine($"K4 - BLANKETT FÖR KAPITALINKOMSTER");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Beskattningsår: {taxYear}");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        
        sb.AppendLine("SAMMANFATTNING");
        sb.AppendLine("-".PadRight(80, '-'));
        sb.AppendLine(CultureInfo.InvariantCulture, $"Totala kapitalvinster:    {report.TotalGain:N2} SEK");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Totala kapitalförluster:  {report.TotalLoss:N2} SEK");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Nettovinst/-förlust:      {report.NetGain:N2} SEK");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Skattepliktig vinst (30%): {report.TaxableGain:N2} SEK");
        sb.AppendLine();
        
        if (report.CategorySummaries.Any())
        {
            sb.AppendLine("SAMMANFATTNING PER VÄRDEPAPPERSTYP");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine(CultureInfo.InvariantCulture, $"{"Typ",-15} {"Antal",-10} {"Vinst",-15} {"Förlust",-15} {"Netto",-15}");
            sb.AppendLine("-".PadRight(80, '-'));
            
            foreach (var summary in report.CategorySummaries)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{summary.SecurityType,-15} {summary.TransactionCount,-10} " +
                    $"{summary.TotalGain,-15:N2} {summary.TotalLoss,-15:N2} {summary.NetGain,-15:N2}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("DETALJERADE TRANSAKTIONER");
        sb.AppendLine("-".PadRight(80, '-'));
        sb.AppendLine(CultureInfo.InvariantCulture, $"{"Namn",-25} {"ISIN",-12} {"Sälj.datum",-12} {"Antal",-10} {"Vinst/Förlust",15}");
        sb.AppendLine("-".PadRight(80, '-'));
        
        foreach (var gain in report.Gains)
        {
            var name = gain.SecurityName.Length > 24
                ? string.Concat(gain.SecurityName.AsSpan(0, 21), "...")
                : gain.SecurityName;
            var isin = gain.ISIN ?? "N/A";
            
            sb.AppendLine(CultureInfo.InvariantCulture, $"{name,-25} {isin,-12} " +
                $"{gain.SaleDate:yyyy-MM-dd,-12} {gain.Quantity,-10:N2} {gain.Gain,15:N2}");
        }
        
        sb.AppendLine();
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine("NOTERINGAR:");
        sb.AppendLine("- Kapitalvinster beskattas med 30% i Sverige");
        sb.AppendLine("- Kapitalförluster kan kvittas mot kapitalvinster samma år");
        sb.AppendLine("- Kvarvarande förlust kan dras av med 70% mot andra inkomster (max 100 000 SEK/år)");
        sb.AppendLine("- ISK-konton (Investeringssparkonto) beskattas med schablonintäkt och ingår ej här");
        sb.AppendLine("- Kontrollera alltid uppgifterna med dina kontoutdrag och handlingar");
        sb.AppendLine("=".PadRight(80, '='));
        
        return sb.ToString();
    }
}
