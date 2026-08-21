using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class BudgetTemplateService
{
    /// <summary>
    /// Apply a budget template to distribute income across categories
    /// </summary>
    /// <param name="templateType">Type of budget template to apply</param>
    /// <param name="totalIncome">Total monthly/yearly income</param>
    /// <param name="categories">Available categories</param>
    /// <returns>Dictionary of CategoryId to PlannedAmount</returns>
    public static Dictionary<int, decimal> ApplyTemplate(
        BudgetTemplateType templateType, 
        decimal totalIncome, 
        IEnumerable<Category> categories)
    {
        var result = new Dictionary<int, decimal>();
        
        switch (templateType)
        {
            case BudgetTemplateType.FiftyThirtyTwenty:
                return ApplyFiftyThirtyTwentyTemplate(totalIncome, categories);
            
            case BudgetTemplateType.ZeroBased:
                return ApplyZeroBasedTemplate(totalIncome, categories);
            
            case BudgetTemplateType.Envelope:
                return ApplyEnvelopeTemplate(totalIncome, categories);
            
            case BudgetTemplateType.SwedishFamily:
                return ApplySwedishFamilyTemplate(totalIncome, categories);
            
            case BudgetTemplateType.SwedishSingle:
                return ApplySwedishSingleTemplate(totalIncome, categories);
            
            default:
                // Custom - all zeros, user fills in manually
                foreach (var category in categories)
                {
                    result[category.CategoryId] = 0;
                }
                return result;
        }
    }

    /// <summary>
    /// 50/30/20 Budget Rule:
    /// - 50% Needs (Boende, Transport, Mat)
    /// - 30% Wants (Nöje, Shopping)
    /// - 20% Savings (Sparande)
    /// </summary>
    private static Dictionary<int, decimal> ApplyFiftyThirtyTwentyTemplate(
        decimal totalIncome, 
        IEnumerable<Category> categories)
    {
        var result = new Dictionary<int, decimal>();
        var needs = totalIncome * 0.50m;
        var wants = totalIncome * 0.30m;
        var savings = totalIncome * 0.20m;

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLowerInvariant();
            
            // 50% - Needs (Behov)
            if (categoryName.Contains("boende") || 
                categoryName.Contains("transport") || 
                categoryName.Contains("mat") ||
                categoryName.Contains("hälsa") ||
                categoryName.Contains("försäkring"))
            {
                // Distribute needs budget among need categories
                if (categoryName.Contains("boende"))
                    result[category.CategoryId] = needs * 0.40m; // 40% of needs for housing
                else if (categoryName.Contains("mat"))
                    result[category.CategoryId] = needs * 0.35m; // 35% of needs for food
                else if (categoryName.Contains("transport"))
                    result[category.CategoryId] = needs * 0.15m; // 15% of needs for transport
                else
                    result[category.CategoryId] = needs * 0.10m; // 10% for other needs
            }
            // 30% - Wants (Önskemål)
            else if (categoryName.Contains("nöje") || 
                     categoryName.Contains("shopping") ||
                     categoryName.Contains("hobby") ||
                     categoryName.Contains("restaurang"))
            {
                // Distribute wants budget among want categories
                if (categoryName.Contains("nöje"))
                    result[category.CategoryId] = wants * 0.50m;
                else if (categoryName.Contains("shopping"))
                    result[category.CategoryId] = wants * 0.30m;
                else
                    result[category.CategoryId] = wants * 0.20m;
            }
            // 20% - Savings (Sparande)
            else if (categoryName.Contains("sparande") || 
                     categoryName.Contains("investering") ||
                     categoryName.Contains("pension"))
            {
                result[category.CategoryId] = savings;
            }
            else
            {
                // Other categories get minimal allocation
                result[category.CategoryId] = totalIncome * 0.01m;
            }
        }

        return result;
    }

    /// <summary>
    /// Zero-Based Budget:
    /// Every krona is assigned a job. Total allocations should equal total income.
    /// This provides suggested starting percentages based on typical Swedish household spending.
    /// </summary>
    private static Dictionary<int, decimal> ApplyZeroBasedTemplate(
        decimal totalIncome, 
        IEnumerable<Category> categories)
    {
        var result = new Dictionary<int, decimal>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLowerInvariant();
            
            // Typical Swedish household budget percentages
            if (categoryName.Contains("boende"))
                result[category.CategoryId] = totalIncome * 0.30m; // 30% housing
            else if (categoryName.Contains("mat") || categoryName.Contains("dryck"))
                result[category.CategoryId] = totalIncome * 0.15m; // 15% food
            else if (categoryName.Contains("transport"))
                result[category.CategoryId] = totalIncome * 0.10m; // 10% transport
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
                result[category.CategoryId] = totalIncome * 0.15m; // 15% savings
            else if (categoryName.Contains("nöje"))
                result[category.CategoryId] = totalIncome * 0.10m; // 10% entertainment
            else if (categoryName.Contains("shopping"))
                result[category.CategoryId] = totalIncome * 0.05m; // 5% shopping
            else if (categoryName.Contains("hälsa"))
                result[category.CategoryId] = totalIncome * 0.05m; // 5% health
            else if (categoryName.Contains("försäkring"))
                result[category.CategoryId] = totalIncome * 0.05m; // 5% insurance
            else
                result[category.CategoryId] = totalIncome * 0.05m; // 5% other/buffer
        }

        // Ensure total equals income (adjust last category if needed)
        var total = result.Values.Sum();
        if (total != totalIncome && result.Any())
        {
            var lastKey = result.Keys.Last();
            result[lastKey] += (totalIncome - total);
        }

        return result;
    }

    /// <summary>
    /// Envelope Budget:
    /// Each category gets a specific "envelope" of money.
    /// Once the envelope is empty, no more spending in that category.
    /// Similar to zero-based but with emphasis on strict category limits.
    /// </summary>
    private static Dictionary<int, decimal> ApplyEnvelopeTemplate(
        decimal totalIncome, 
        IEnumerable<Category> categories)
    {
        var result = new Dictionary<int, decimal>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLowerInvariant();
            
            // Envelope budgeting with conservative allocations
            if (categoryName.Contains("boende"))
                result[category.CategoryId] = totalIncome * 0.30m;
            else if (categoryName.Contains("mat"))
                result[category.CategoryId] = totalIncome * 0.12m;
            else if (categoryName.Contains("transport"))
                result[category.CategoryId] = totalIncome * 0.08m;
            else if (categoryName.Contains("sparande"))
                result[category.CategoryId] = totalIncome * 0.20m; // Higher savings emphasis
            else if (categoryName.Contains("nöje"))
                result[category.CategoryId] = totalIncome * 0.08m;
            else if (categoryName.Contains("shopping"))
                result[category.CategoryId] = totalIncome * 0.05m;
            else if (categoryName.Contains("hälsa"))
                result[category.CategoryId] = totalIncome * 0.05m;
            else if (categoryName.Contains("buffert") || categoryName.Contains("övrigt"))
                result[category.CategoryId] = totalIncome * 0.12m; // Emergency buffer
            else
                result[category.CategoryId] = 0m; // No allocation unless specifically planned
        }

        return result;
    }

    /// <summary>
    /// Swedish Family Household Budget Template (Länsförsäkringar style):
    /// Based on typical Swedish household spending patterns for families.
    /// Emphasizes: 
    /// - Fixed costs (boende, försäkringar, el)
    /// - Variable costs split by type (mat butik vs restaurang)
    /// - Savings treated as a fixed monthly cost (10-15%)
    /// - Annual costs divided into monthly amounts
    /// </summary>
    private static Dictionary<int, decimal> ApplySwedishFamilyTemplate(
        decimal totalIncome,
        IEnumerable<Category> categories)
    {
        var result = new Dictionary<int, decimal>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLowerInvariant();
            
            // Fasta månadskostnader (Fixed monthly costs)
            if (categoryName.Contains("boende"))
                result[category.CategoryId] = totalIncome * 0.30m; // 30% housing (rent/mortgage)
            else if (categoryName.Contains("försäkring"))
                result[category.CategoryId] = totalIncome * 0.03m; // 3% insurance
            else if (categoryName.Contains("el") || categoryName.Contains("elräkning"))
                result[category.CategoryId] = totalIncome * 0.02m; // 2% electricity
            
            // Sparande som månadskostnad (Savings as a monthly cost - top priority!)
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
                result[category.CategoryId] = totalIncome * 0.15m; // 15% savings (pay yourself first!)
            
            // Rörliga månadskostnader (Variable monthly costs)
            else if (categoryName.Contains("mat") && !categoryName.Contains("restaurang"))
                result[category.CategoryId] = totalIncome * 0.15m; // 15% groceries
            else if (categoryName.Contains("restaurang") || categoryName.Contains("utemat"))
                result[category.CategoryId] = totalIncome * 0.05m; // 5% restaurants/takeout
            else if (categoryName.Contains("transport") || categoryName.Contains("bensin"))
                result[category.CategoryId] = totalIncome * 0.08m; // 8% transport
            
            // Barn och familj (Children and family)
            else if (categoryName.Contains("barn") || categoryName.Contains("fritidsaktivitet"))
                result[category.CategoryId] = totalIncome * 0.05m; // 5% children activities
            
            // Underhållning och fritid (Entertainment and leisure)
            else if (categoryName.Contains("nöje") || categoryName.Contains("streaming"))
                result[category.CategoryId] = totalIncome * 0.04m; // 4% entertainment
            else if (categoryName.Contains("shopping") || categoryName.Contains("kläder"))
                result[category.CategoryId] = totalIncome * 0.04m; // 4% shopping
            
            // Hälsa och välmående (Health and wellness)
            else if (categoryName.Contains("hälsa") || categoryName.Contains("träning") || categoryName.Contains("gym"))
                result[category.CategoryId] = totalIncome * 0.03m; // 3% health/gym
            
            // Buffert/Övrigt (Buffer/Other)
            else if (categoryName.Contains("buffert") || categoryName.Contains("övrigt"))
                result[category.CategoryId] = totalIncome * 0.06m; // 6% buffer for unexpected costs
            else
                result[category.CategoryId] = 0m;
        }

        return result;
    }

    /// <summary>
    /// Swedish Single Household Budget Template (Länsförsäkringar style):
    /// Based on typical Swedish household spending patterns for single person households.
    /// Generally lower fixed costs and more flexibility for personal choices.
    /// </summary>
    private static Dictionary<int, decimal> ApplySwedishSingleTemplate(
        decimal totalIncome,
        IEnumerable<Category> categories)
    {
        var result = new Dictionary<int, decimal>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLowerInvariant();
            
            // Fasta månadskostnader (Fixed monthly costs)
            if (categoryName.Contains("boende"))
                result[category.CategoryId] = totalIncome * 0.28m; // 28% housing (slightly lower for singles)
            else if (categoryName.Contains("försäkring"))
                result[category.CategoryId] = totalIncome * 0.02m; // 2% insurance
            else if (categoryName.Contains("el") || categoryName.Contains("elräkning"))
                result[category.CategoryId] = totalIncome * 0.015m; // 1.5% electricity
            
            // Sparande som månadskostnad (Savings as a monthly cost - top priority!)
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
                result[category.CategoryId] = totalIncome * 0.20m; // 20% savings (higher for singles)
            
            // Rörliga månadskostnader (Variable monthly costs)
            else if (categoryName.Contains("mat") && !categoryName.Contains("restaurang"))
                result[category.CategoryId] = totalIncome * 0.12m; // 12% groceries (lower for one person)
            else if (categoryName.Contains("restaurang") || categoryName.Contains("utemat"))
                result[category.CategoryId] = totalIncome * 0.06m; // 6% restaurants/takeout
            else if (categoryName.Contains("transport") || categoryName.Contains("bensin"))
                result[category.CategoryId] = totalIncome * 0.07m; // 7% transport
            
            // Underhållning och fritid (Entertainment and leisure)
            else if (categoryName.Contains("nöje") || categoryName.Contains("streaming"))
                result[category.CategoryId] = totalIncome * 0.05m; // 5% entertainment
            else if (categoryName.Contains("shopping") || categoryName.Contains("kläder"))
                result[category.CategoryId] = totalIncome * 0.06m; // 6% shopping
            
            // Hälsa och välmående (Health and wellness)
            else if (categoryName.Contains("hälsa") || categoryName.Contains("träning") || categoryName.Contains("gym"))
                result[category.CategoryId] = totalIncome * 0.04m; // 4% health/gym
            
            // Buffert/Övrigt (Buffer/Other)
            else if (categoryName.Contains("buffert") || categoryName.Contains("övrigt"))
                result[category.CategoryId] = totalIncome * 0.095m; // 9.5% buffer for unexpected costs
            else
                result[category.CategoryId] = 0m;
        }

        return result;
    }

    /// <summary>
    /// Get template description and recommendations
    /// </summary>
    public static string GetTemplateDescription(BudgetTemplateType templateType)
    {
        return templateType switch
        {
            BudgetTemplateType.FiftyThirtyTwenty => 
                "50/30/20-regeln: 50% behov (boende, mat, transport), 30% önskemål (nöje, shopping), 20% sparande",
            
            BudgetTemplateType.ZeroBased => 
                "Zero-based budgeting: Varje krona tilldelas ett syfte. Alla inkomster fördelas över kategorier.",
            
            BudgetTemplateType.Envelope => 
                "Kuvertbudget: Varje kategori får ett fast belopp. När pengarna är slut, inget mer spenderande i den kategorin.",
            
            BudgetTemplateType.SwedishFamily => 
                "Svenska Familjehushåll: Baserad på Länsförsäkringar's mall för familjer. Inkluderar fasta kostnader, sparande som månadskostnad, och uppdelning av mat (butik vs restaurang). 15% sparande prioriteras.",
            
            BudgetTemplateType.SwedishSingle => 
                "Svenska Singelhushåll: Baserad på Länsförsäkringar's mall för singelhushåll. Lägre fasta kostnader, högre sparkvot (20%). Fokus på flexibilitet och ekonomisk självständighet.",
            
            _ => "Anpassad budget: Du väljer själv hur mycket som ska budgeteras per kategori"
        };
    }

    /// <summary>
    /// Get suggested monthly income based on historical transactions
    /// </summary>
    public static decimal GetSuggestedIncome(IEnumerable<Transaction> recentTransactions)
    {
        var incomeTransactions = recentTransactions.Where(t => t.IsIncome);
        
        if (!incomeTransactions.Any())
            return 30000m; // Default Swedish median income
        
        // Average of last 3 months income
        return incomeTransactions
            .Where(t => t.Date >= DateTime.Now.AddMonths(-3))
            .Average(t => t.Amount);
    }
}
