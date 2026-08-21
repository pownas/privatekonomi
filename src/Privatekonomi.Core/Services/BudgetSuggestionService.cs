using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;

namespace Privatekonomi.Core.Services;

public class BudgetSuggestionService : IBudgetSuggestionService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ICategoryService _categoryService;
    private readonly ITransactionService _transactionService;

    public BudgetSuggestionService(
        PrivatekonomyContext context,
        ICategoryService categoryService,
        ITransactionService transactionService,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _categoryService = categoryService;
        _transactionService = transactionService;
        _currentUserService = currentUserService;
    }

    public async Task<BudgetSuggestion> GenerateSuggestionAsync(
        decimal totalIncome,
        BudgetDistributionModel model,
        string? name = null,
        int? householdId = null)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        var categoryList = categories.ToList();

        var suggestion = new BudgetSuggestion
        {
            Name = name ?? $"{GetModelDisplayName(model)} förslag - {DateTime.Now:yyyy-MM-dd}",
            TotalIncome = totalIncome,
            DistributionModel = model,
            CreatedAt = DateTime.UtcNow,
            IsAccepted = false,
            HouseholdId = householdId,
            UserId = _currentUserService?.UserId
        };

        var allocations = GetDistributionAllocations(model, totalIncome, categoryList);

        foreach (var allocation in allocations)
        {
            var item = new BudgetSuggestionItem
            {
                CategoryId = allocation.CategoryId,
                SuggestedAmount = allocation.Amount,
                AdjustedAmount = allocation.Amount,
                Percentage = totalIncome > 0 ? (allocation.Amount / totalIncome) * 100 : 0,
                AllocationCategory = allocation.AllocationCategory,
                RecurrencePeriodMonths = 1,
                IsManuallyAdjusted = false
            };
            suggestion.Items.Add(item);
        }

        _context.BudgetSuggestions.Add(suggestion);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return (await GetSuggestionByIdAsync(suggestion.BudgetSuggestionId))!;
    }

    public async Task<BudgetSuggestion> GenerateSuggestionFromHistoryAsync(
        BudgetDistributionModel model,
        int monthsToAnalyze = 3,
        int? householdId = null)
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddMonths(-monthsToAnalyze);

        var transactions = await _transactionService.GetTransactionsByDateRangeAsync(startDate, endDate);
        
        // Calculate average monthly income
        var incomeTransactions = transactions.Where(t => t.IsIncome);
        decimal averageMonthlyIncome = 30000m; // Default

        if (incomeTransactions.Any())
        {
            averageMonthlyIncome = incomeTransactions.Sum(t => t.Amount) / monthsToAnalyze;
        }

        return await GenerateSuggestionAsync(
            averageMonthlyIncome, 
            model, 
            $"{GetModelDisplayName(model)} baserat på historik ({monthsToAnalyze} månader)",
            householdId);
    }

    public async Task<IEnumerable<BudgetSuggestion>> GetAllSuggestionsAsync()
    {
        var query = _context.BudgetSuggestions
            .Include(s => s.Items)
                .ThenInclude(i => i.Category)
            .Include(s => s.Adjustments)
                .ThenInclude(a => a.Category)
            .AsQueryable();

        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(s => s.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<BudgetSuggestion?> GetSuggestionByIdAsync(int id)
    {
        var query = _context.BudgetSuggestions
            .Include(s => s.Items)
                .ThenInclude(i => i.Category)
            .Include(s => s.Adjustments)
                .ThenInclude(a => a.Category)
            .Include(s => s.AppliedBudget)
            .AsQueryable();

        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(s => s.UserId == _currentUserService.UserId);
        }

        return await query.FirstOrDefaultAsync(s => s.BudgetSuggestionId == id);
    }

    public async Task<IEnumerable<BudgetSuggestion>> GetPendingSuggestionsAsync()
    {
        var query = _context.BudgetSuggestions
            .Include(s => s.Items)
                .ThenInclude(i => i.Category)
            .Where(s => !s.IsAccepted)
            .AsQueryable();

        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(s => s.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<BudgetSuggestionItem> AdjustSuggestionItemAsync(
        int suggestionId,
        int categoryId,
        decimal newAmount,
        string? reason = null)
    {
        var suggestion = await GetSuggestionByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new InvalidOperationException($"Suggestion with ID {suggestionId} not found");
        }

        if (suggestion.IsAccepted)
        {
            throw new InvalidOperationException("Cannot adjust an accepted suggestion");
        }

        var item = suggestion.Items.FirstOrDefault(i => i.CategoryId == categoryId);
        if (item == null)
        {
            throw new InvalidOperationException($"Category {categoryId} not found in suggestion");
        }

        // Record the adjustment
        var adjustment = new BudgetAdjustment
        {
            BudgetSuggestionId = suggestionId,
            CategoryId = categoryId,
            PreviousAmount = item.AdjustedAmount,
            NewAmount = newAmount,
            Reason = reason,
            AdjustedAt = DateTime.UtcNow,
            Type = AdjustmentType.Modification
        };

        _context.BudgetAdjustments.Add(adjustment);

        // Update the item
        item.AdjustedAmount = newAmount;
        item.Percentage = suggestion.TotalIncome > 0 ? (newAmount / suggestion.TotalIncome) * 100 : 0;
        item.IsManuallyAdjusted = true;

        await _context.SaveChangesAsync();
        return item;
    }

    public async Task TransferBetweenItemsAsync(
        int suggestionId,
        int fromCategoryId,
        int toCategoryId,
        decimal amount,
        string? reason = null)
    {
        var suggestion = await GetSuggestionByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new InvalidOperationException($"Suggestion with ID {suggestionId} not found");
        }

        if (suggestion.IsAccepted)
        {
            throw new InvalidOperationException("Cannot adjust an accepted suggestion");
        }

        var fromItem = suggestion.Items.FirstOrDefault(i => i.CategoryId == fromCategoryId);
        var toItem = suggestion.Items.FirstOrDefault(i => i.CategoryId == toCategoryId);

        if (fromItem == null || toItem == null)
        {
            throw new InvalidOperationException("Source or target category not found in suggestion");
        }

        if (fromItem.AdjustedAmount < amount)
        {
            throw new InvalidOperationException("Insufficient amount in source category");
        }

        // Record the transfer adjustment
        var adjustment = new BudgetAdjustment
        {
            BudgetSuggestionId = suggestionId,
            CategoryId = fromCategoryId,
            PreviousAmount = fromItem.AdjustedAmount,
            NewAmount = fromItem.AdjustedAmount - amount,
            TransferToCategoryId = toCategoryId,
            Reason = reason ?? $"Överföring till {toItem.Category?.Name ?? "annan kategori"}",
            AdjustedAt = DateTime.UtcNow,
            Type = AdjustmentType.Transfer
        };

        _context.BudgetAdjustments.Add(adjustment);

        // Update items
        fromItem.AdjustedAmount -= amount;
        fromItem.Percentage = suggestion.TotalIncome > 0 ? (fromItem.AdjustedAmount / suggestion.TotalIncome) * 100 : 0;
        fromItem.IsManuallyAdjusted = true;

        toItem.AdjustedAmount += amount;
        toItem.Percentage = suggestion.TotalIncome > 0 ? (toItem.AdjustedAmount / suggestion.TotalIncome) * 100 : 0;
        toItem.IsManuallyAdjusted = true;

        await _context.SaveChangesAsync();
    }

    public async Task<Budget> AcceptSuggestionAsync(
        int suggestionId,
        DateTime startDate,
        DateTime endDate,
        BudgetPeriod period)
    {
        var suggestion = await GetSuggestionByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new InvalidOperationException($"Suggestion with ID {suggestionId} not found");
        }

        if (suggestion.IsAccepted)
        {
            throw new InvalidOperationException("Suggestion has already been accepted");
        }

        // Create the budget from the suggestion
        var budget = new Budget
        {
            Name = $"Budget från {suggestion.Name}",
            Description = $"Skapad från förslag med {GetModelDisplayName(suggestion.DistributionModel)}-modellen",
            StartDate = startDate,
            EndDate = endDate,
            Period = period,
            TemplateType = MapDistributionModelToTemplateType(suggestion.DistributionModel),
            CreatedAt = DateTime.UtcNow,
            UserId = _currentUserService?.UserId,
            HouseholdId = suggestion.HouseholdId,
            ValidFrom = DateTime.UtcNow
        };

        // Add budget categories from adjusted suggestion items
        foreach (var item in suggestion.Items.Where(i => i.AdjustedAmount > 0))
        {
            budget.BudgetCategories.Add(new BudgetCategory
            {
                CategoryId = item.CategoryId,
                PlannedAmount = item.AdjustedAmount,
                RecurrencePeriodMonths = item.RecurrencePeriodMonths
            });
        }

        _context.Budgets.Add(budget);

        // Update the suggestion
        suggestion.IsAccepted = true;
        suggestion.AcceptedAt = DateTime.UtcNow;
        suggestion.AppliedBudgetId = budget.BudgetId;

        await _context.SaveChangesAsync();

        // Update the reference after save (budget now has an ID)
        suggestion.AppliedBudgetId = budget.BudgetId;
        await _context.SaveChangesAsync();

        return budget;
    }

    public async Task DeleteSuggestionAsync(int id)
    {
        var suggestion = await _context.BudgetSuggestions.FindAsync(id);
        if (suggestion != null)
        {
            _context.BudgetSuggestions.Remove(suggestion);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<BudgetSuggestionEffects> CalculateEffectsAsync(int suggestionId)
    {
        var suggestion = await GetSuggestionByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new InvalidOperationException($"Suggestion with ID {suggestionId} not found");
        }

        var effects = new BudgetSuggestionEffects
        {
            AdjustmentsCount = suggestion.Adjustments.Count
        };

        foreach (var item in suggestion.Items)
        {
            effects.TotalOriginalAmount += item.SuggestedAmount;
            effects.TotalAdjustedAmount += item.AdjustedAmount;

            switch (item.AllocationCategory)
            {
                case BudgetAllocationCategory.Needs:
                    effects.NeedsOriginal += item.SuggestedAmount;
                    effects.NeedsAdjusted += item.AdjustedAmount;
                    break;
                case BudgetAllocationCategory.Wants:
                    effects.WantsOriginal += item.SuggestedAmount;
                    effects.WantsAdjusted += item.AdjustedAmount;
                    break;
                case BudgetAllocationCategory.Savings:
                    effects.SavingsOriginal += item.SuggestedAmount;
                    effects.SavingsAdjusted += item.AdjustedAmount;
                    break;
            }

            effects.CategoryEffects.Add(new CategoryEffect
            {
                CategoryId = item.CategoryId,
                CategoryName = item.Category?.Name ?? string.Empty,
                OriginalAmount = item.SuggestedAmount,
                AdjustedAmount = item.AdjustedAmount,
                AllocationCategory = item.AllocationCategory
            });
        }

        return effects;
    }

    public string GetDistributionModelDescription(BudgetDistributionModel model)
    {
        return model switch
        {
            BudgetDistributionModel.FiftyThirtyTwenty =>
                "50/30/20-regeln: 50% till behov (boende, mat, transport), 30% till önskemål (nöje, shopping), 20% till sparande. En enkel och populär metod för att balansera ekonomin.",
            
            BudgetDistributionModel.ZeroBased =>
                "Zero-based budgeting: Varje krona tilldelas ett specifikt syfte. Perfekt för dig som vill ha full kontroll över varje utgift.",
            
            BudgetDistributionModel.Envelope =>
                "Kuvertbudget: Strikta gränser per kategori. När pengarna är slut, inget mer spenderande i den kategorin. Bra för att lära sig budgetdisciplin.",
            
            BudgetDistributionModel.SwedishFamily =>
                "Svenska Familjehushåll: Baserad på Länsförsäkringar's mall för familjer. Inkluderar 15% sparande som månadskostnad och separation av mat i butik vs restaurang.",
            
            BudgetDistributionModel.SwedishSingle =>
                "Svenska Singelhushåll: Anpassad för ensamstående med lägre fasta kostnader och högre sparkvot (20%). Fokus på ekonomisk självständighet.",
            
            BudgetDistributionModel.EightyTwenty =>
                "80/20-regeln: 80% för alla utgifter, 20% direkt till sparande. Enkel metod som prioriterar sparande högt.",
            
            BudgetDistributionModel.SeventyTwentyTen =>
                "70/20/10-regeln: 70% till behov och önskemål, 20% till sparande, 10% till välgörenhet eller extra amorteringar.",
            
            _ => "Anpassad budget"
        };
    }

    public IEnumerable<(BudgetDistributionModel Model, string Name, string Description)> GetAvailableModels()
    {
        return new[]
        {
            (BudgetDistributionModel.FiftyThirtyTwenty, "50/30/20-regeln", GetDistributionModelDescription(BudgetDistributionModel.FiftyThirtyTwenty)),
            (BudgetDistributionModel.SwedishFamily, "Svenska Familjehushåll", GetDistributionModelDescription(BudgetDistributionModel.SwedishFamily)),
            (BudgetDistributionModel.SwedishSingle, "Svenska Singelhushåll", GetDistributionModelDescription(BudgetDistributionModel.SwedishSingle)),
            (BudgetDistributionModel.EightyTwenty, "80/20-regeln", GetDistributionModelDescription(BudgetDistributionModel.EightyTwenty)),
            (BudgetDistributionModel.SeventyTwentyTen, "70/20/10-regeln", GetDistributionModelDescription(BudgetDistributionModel.SeventyTwentyTen)),
            (BudgetDistributionModel.ZeroBased, "Zero-based budgeting", GetDistributionModelDescription(BudgetDistributionModel.ZeroBased)),
            (BudgetDistributionModel.Envelope, "Kuvertbudget", GetDistributionModelDescription(BudgetDistributionModel.Envelope))
        };
    }

    private static string GetModelDisplayName(BudgetDistributionModel model)
    {
        return model switch
        {
            BudgetDistributionModel.FiftyThirtyTwenty => "50/30/20",
            BudgetDistributionModel.ZeroBased => "Zero-based",
            BudgetDistributionModel.Envelope => "Kuvertbudget",
            BudgetDistributionModel.SwedishFamily => "Svenska Familjehushåll",
            BudgetDistributionModel.SwedishSingle => "Svenska Singelhushåll",
            BudgetDistributionModel.EightyTwenty => "80/20",
            BudgetDistributionModel.SeventyTwentyTen => "70/20/10",
            _ => "Anpassad"
        };
    }

    private static BudgetTemplateType? MapDistributionModelToTemplateType(BudgetDistributionModel model)
    {
        return model switch
        {
            BudgetDistributionModel.FiftyThirtyTwenty => BudgetTemplateType.FiftyThirtyTwenty,
            BudgetDistributionModel.ZeroBased => BudgetTemplateType.ZeroBased,
            BudgetDistributionModel.Envelope => BudgetTemplateType.Envelope,
            BudgetDistributionModel.SwedishFamily => BudgetTemplateType.SwedishFamily,
            BudgetDistributionModel.SwedishSingle => BudgetTemplateType.SwedishSingle,
            _ => BudgetTemplateType.Custom
        };
    }

    private List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetDistributionAllocations(
        BudgetDistributionModel model,
        decimal totalIncome,
        List<Category> categories)
    {
        return model switch
        {
            BudgetDistributionModel.FiftyThirtyTwenty => GetFiftyThirtyTwentyAllocations(totalIncome, categories),
            BudgetDistributionModel.SwedishFamily => GetSwedishFamilyAllocations(totalIncome, categories),
            BudgetDistributionModel.SwedishSingle => GetSwedishSingleAllocations(totalIncome, categories),
            BudgetDistributionModel.EightyTwenty => GetEightyTwentyAllocations(totalIncome, categories),
            BudgetDistributionModel.SeventyTwentyTen => GetSeventyTwentyTenAllocations(totalIncome, categories),
            BudgetDistributionModel.ZeroBased => GetZeroBasedAllocations(totalIncome, categories),
            BudgetDistributionModel.Envelope => GetEnvelopeAllocations(totalIncome, categories),
            _ => GetDefaultAllocations(totalIncome, categories)
        };
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetFiftyThirtyTwentyAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();
        var needs = totalIncome * 0.50m;
        var wants = totalIncome * 0.30m;
        var savings = totalIncome * 0.20m;

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal amount = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            // 50% - Needs (Behov)
            if (categoryName.Contains("boende"))
            {
                amount = needs * 0.40m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("mat") && !categoryName.Contains("restaurang"))
            {
                amount = needs * 0.35m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("transport"))
            {
                amount = needs * 0.15m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("hälsa") || categoryName.Contains("försäkring"))
            {
                amount = needs * 0.10m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            // 30% - Wants (Önskemål)
            else if (categoryName.Contains("nöje"))
            {
                amount = wants * 0.50m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping"))
            {
                amount = wants * 0.30m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("restaurang") || categoryName.Contains("hobby"))
            {
                amount = wants * 0.20m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            // 20% - Savings (Sparande)
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering") || categoryName.Contains("pension"))
            {
                amount = savings;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else
            {
                amount = totalIncome * 0.01m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }

            if (amount > 0)
            {
                result.Add((category.CategoryId, Math.Round(amount, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetSwedishFamilyAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal percentage = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            if (categoryName.Contains("boende"))
            {
                percentage = 0.30m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("försäkring"))
            {
                percentage = 0.03m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("el"))
            {
                percentage = 0.02m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
            {
                percentage = 0.15m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else if (categoryName.Contains("mat") && !categoryName.Contains("restaurang"))
            {
                percentage = 0.15m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("restaurang") || categoryName.Contains("utemat"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("transport") || categoryName.Contains("bensin"))
            {
                percentage = 0.08m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("barn") || categoryName.Contains("fritidsaktivitet"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("nöje") || categoryName.Contains("streaming"))
            {
                percentage = 0.04m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping") || categoryName.Contains("kläder"))
            {
                percentage = 0.04m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("hälsa") || categoryName.Contains("träning") || categoryName.Contains("gym"))
            {
                percentage = 0.03m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("buffert") || categoryName.Contains("övrigt"))
            {
                percentage = 0.06m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }

            if (percentage > 0)
            {
                result.Add((category.CategoryId, Math.Round(totalIncome * percentage, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetSwedishSingleAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal percentage = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            if (categoryName.Contains("boende"))
            {
                percentage = 0.28m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("försäkring"))
            {
                percentage = 0.02m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("el"))
            {
                percentage = 0.015m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
            {
                percentage = 0.20m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else if (categoryName.Contains("mat") && !categoryName.Contains("restaurang"))
            {
                percentage = 0.12m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("restaurang") || categoryName.Contains("utemat"))
            {
                percentage = 0.06m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("transport") || categoryName.Contains("bensin"))
            {
                percentage = 0.07m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("nöje") || categoryName.Contains("streaming"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping") || categoryName.Contains("kläder"))
            {
                percentage = 0.06m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("hälsa") || categoryName.Contains("träning") || categoryName.Contains("gym"))
            {
                percentage = 0.04m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("buffert") || categoryName.Contains("övrigt"))
            {
                percentage = 0.095m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }

            if (percentage > 0)
            {
                result.Add((category.CategoryId, Math.Round(totalIncome * percentage, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetEightyTwentyAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();
        var expenses = totalIncome * 0.80m;
        var savings = totalIncome * 0.20m;

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal amount = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
            {
                amount = savings;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else if (categoryName.Contains("boende"))
            {
                amount = expenses * 0.35m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("mat"))
            {
                amount = expenses * 0.20m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("transport"))
            {
                amount = expenses * 0.12m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("nöje"))
            {
                amount = expenses * 0.10m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping"))
            {
                amount = expenses * 0.08m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("hälsa"))
            {
                amount = expenses * 0.05m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("övrigt"))
            {
                amount = expenses * 0.10m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }

            if (amount > 0)
            {
                result.Add((category.CategoryId, Math.Round(amount, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetSeventyTwentyTenAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();
        var expenses = totalIncome * 0.70m;
        var savings = totalIncome * 0.20m;
        var giving = totalIncome * 0.10m;

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal amount = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
            {
                amount = savings;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else if (categoryName.Contains("välgörenhet") || categoryName.Contains("donation"))
            {
                amount = giving;
                allocationCategory = BudgetAllocationCategory.Giving;
            }
            else if (categoryName.Contains("boende"))
            {
                amount = expenses * 0.40m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("mat"))
            {
                amount = expenses * 0.20m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("transport"))
            {
                amount = expenses * 0.15m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("nöje"))
            {
                amount = expenses * 0.10m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping") || categoryName.Contains("övrigt"))
            {
                amount = expenses * 0.15m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }

            if (amount > 0)
            {
                result.Add((category.CategoryId, Math.Round(amount, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetZeroBasedAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal percentage = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            if (categoryName.Contains("boende"))
            {
                percentage = 0.30m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("mat"))
            {
                percentage = 0.15m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("transport"))
            {
                percentage = 0.10m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("sparande") || categoryName.Contains("investering"))
            {
                percentage = 0.15m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else if (categoryName.Contains("nöje"))
            {
                percentage = 0.10m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("hälsa"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("försäkring"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("övrigt"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }

            if (percentage > 0)
            {
                result.Add((category.CategoryId, Math.Round(totalIncome * percentage, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetEnvelopeAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        var result = new List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)>();

        foreach (var category in categories)
        {
            var categoryName = category.Name.ToLower(CultureInfo.CurrentCulture);
            decimal percentage = 0;
            var allocationCategory = BudgetAllocationCategory.Wants;

            if (categoryName.Contains("boende"))
            {
                percentage = 0.30m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("mat"))
            {
                percentage = 0.12m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("transport"))
            {
                percentage = 0.08m;
                allocationCategory = BudgetAllocationCategory.Needs;
            }
            else if (categoryName.Contains("sparande"))
            {
                percentage = 0.20m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }
            else if (categoryName.Contains("nöje"))
            {
                percentage = 0.08m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("shopping"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("hälsa"))
            {
                percentage = 0.05m;
                allocationCategory = BudgetAllocationCategory.Wants;
            }
            else if (categoryName.Contains("buffert") || categoryName.Contains("övrigt"))
            {
                percentage = 0.12m;
                allocationCategory = BudgetAllocationCategory.Savings;
            }

            if (percentage > 0)
            {
                result.Add((category.CategoryId, Math.Round(totalIncome * percentage, 2), allocationCategory));
            }
        }

        return result;
    }

    private static List<(int CategoryId, decimal Amount, BudgetAllocationCategory AllocationCategory)> GetDefaultAllocations(
        decimal totalIncome,
        List<Category> categories)
    {
        // Default: distribute evenly
        var amountPerCategory = totalIncome / Math.Max(categories.Count, 1);
        return categories.Select(c => (c.CategoryId, Math.Round(amountPerCategory, 2), BudgetAllocationCategory.Wants)).ToList();
    }
}
