using Microsoft.EntityFrameworkCore;

using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for analyzing expense patterns and generating heatmap visualizations
/// </summary>
public class HeatmapAnalysisService : IHeatmapAnalysisService
{
    private readonly PrivatekonomyContext _context;
    private static readonly string[] SwedishDayNames = { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };

    public HeatmapAnalysisService(PrivatekonomyContext context)
    {
        _context = context;
    }

    public async Task<ExpenseHeatmapData> GenerateHeatmapAsync(
        DateTime startDate,
        DateTime endDate,
        int? categoryId = null,
        int? householdId = null)
    {
        // Get ALL transactions for the period (both income and expenses)
        var query = _context.Transactions
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .WhereIncludedInTotals();

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == categoryId.Value));
        }

        var transactions = await query
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .ToListAsync();

        // Initialize heatmap data
        var heatmapData = new ExpenseHeatmapData
        {
            StartDate = startDate,
            EndDate = endDate,
            CategoryId = categoryId,
            TransactionCount = transactions.Count,
            TotalExpenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount)
        };

        // Get category name if filtering
        if (categoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(categoryId.Value);
            heatmapData.CategoryName = category?.Name;
        }

        // Group transactions by weekday and hour
        var grouped = transactions
            .GroupBy(t => new
            {
                Weekday = GetMondayBasedWeekday(t.Date.DayOfWeek),
                Hour = t.Date.Hour
            })
            .Select(g => new
            {
                g.Key.Weekday,
                g.Key.Hour,
                ExpenseAmount = g.Where(t => !t.IsIncome).Sum(t => t.Amount),
                IncomeAmount = g.Where(t => t.IsIncome).Sum(t => t.Amount),
                Count = g.Count(),
                Transactions = g.ToList()
            })
            .ToList();

        // Create heatmap cells
        decimal maxNetAmount = 0;
        
        foreach (var group in grouped)
        {
            if (!heatmapData.HeatmapCells.TryGetValue(group.Weekday, out var weekdayCells))
            {
                weekdayCells = new Dictionary<int, HeatmapCell>();
                heatmapData.HeatmapCells[group.Weekday] = weekdayCells;
            }

            var netAmount = group.IncomeAmount - group.ExpenseAmount;
            
            var cell = new HeatmapCell
            {
                Weekday = group.Weekday,
                Hour = group.Hour,
                Amount = group.ExpenseAmount,
                IncomeAmount = group.IncomeAmount,
                NetAmount = netAmount,
                TransactionCount = group.Count,
                AverageAmount = group.Count > 0 ? (group.ExpenseAmount + group.IncomeAmount) / group.Count : 0
            };

            weekdayCells[group.Hour] = cell;
            
            if (Math.Abs(netAmount) > maxNetAmount)
            {
                maxNetAmount = Math.Abs(netAmount);
            }
        }

        heatmapData.MaxCellAmount = maxNetAmount;

        // Calculate intensity levels for color coding
        CalculateIntensityLevels(heatmapData);

        // Generate insights
        heatmapData.Insights = GenerateInsights(transactions, startDate, endDate, heatmapData.TotalExpenses);

        return heatmapData;
    }

    public async Task<PatternInsights> GetPatternInsightsAsync(
        DateTime startDate,
        DateTime endDate,
        int? categoryId = null,
        int? householdId = null)
    {
        var heatmapData = await GenerateHeatmapAsync(startDate, endDate, categoryId, householdId);
        return heatmapData.Insights;
    }

    private PatternInsights GenerateInsights(
        List<Transaction> transactions,
        DateTime startDate,
        DateTime endDate,
        decimal totalExpenses)
    {
        var insights = new PatternInsights();

        // Group by weekday for day insights
        var byWeekday = transactions
            .GroupBy(t => GetMondayBasedWeekday(t.Date.DayOfWeek))
            .Select(g => new
            {
                Weekday = g.Key,
                Amount = g.Sum(t => t.Amount),
                Count = g.Count(),
                Transactions = g.ToList()
            })
            .OrderByDescending(g => g.Amount)
            .ToList();

        if (byWeekday.Any())
        {
            // Most expensive day
            var mostExpensive = byWeekday.First();
            var topCategoryForDay = mostExpensive.Transactions
                .SelectMany(t => t.TransactionCategories)
                .GroupBy(tc => tc.Category?.Name ?? "Okategoriserad")
                .OrderByDescending(g => g.Sum(tc => tc.Transaction?.Amount ?? 0))
                .FirstOrDefault();

            insights.MostExpensiveDay = new DayInsight
            {
                Weekday = mostExpensive.Weekday,
                DayName = SwedishDayNames[mostExpensive.Weekday],
                Amount = mostExpensive.Amount,
                TransactionCount = mostExpensive.Count,
                TopCategory = topCategoryForDay?.Key
            };

            // Least expensive day
            var leastExpensive = byWeekday.Last();
            insights.LeastExpensiveDay = new DayInsight
            {
                Weekday = leastExpensive.Weekday,
                DayName = SwedishDayNames[leastExpensive.Weekday],
                Amount = leastExpensive.Amount,
                TransactionCount = leastExpensive.Count
            };
        }

        // Detect expense peaks (group by 4-hour time slots)
        var timeSlots = transactions
            .GroupBy(t => new
            {
                Weekday = GetMondayBasedWeekday(t.Date.DayOfWeek),
                TimeSlot = t.Date.Hour / 4 // 0-3, 4-7, 8-11, 12-15, 16-19, 20-23
            })
            .Select(g => new
            {
                g.Key.Weekday,
                g.Key.TimeSlot,
                Amount = g.Sum(t => t.Amount),
                Transactions = g.ToList()
            })
            .OrderByDescending(g => g.Amount)
            .Take(3) // Top 3 peaks
            .ToList();

        foreach (var slot in timeSlots)
        {
            var startHour = slot.TimeSlot * 4;
            var endHour = startHour + 3;
            
            var topCategory = slot.Transactions
                .SelectMany(t => t.TransactionCategories)
                .GroupBy(tc => tc.Category?.Name ?? "Okategoriserad")
                .OrderByDescending(g => g.Sum(tc => tc.Transaction?.Amount ?? 0))
                .FirstOrDefault();

            insights.ExpensePeaks.Add(new TimeSlotInsight
            {
                Weekday = slot.Weekday,
                DayName = SwedishDayNames[slot.Weekday],
                TimeRange = $"{startHour:D2}-{endHour + 1:D2}",
                Amount = slot.Amount,
                TopCategory = topCategory?.Key
            });
        }

        // Detect impulse purchases (20:00-23:59)
        var impulsePurchases = transactions
            .Where(t => t.Date.Hour >= 20) // 20:00-23:59
            .ToList();

        if (impulsePurchases.Any())
        {
            var impulseAmount = impulsePurchases.Sum(t => t.Amount);
            var impulsePercentage = totalExpenses > 0 ? (impulseAmount / totalExpenses) * 100 : 0;

            var impulseByDay = impulsePurchases
                .GroupBy(t => GetMondayBasedWeekday(t.Date.DayOfWeek))
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var impulseTopCategory = impulsePurchases
                .SelectMany(t => t.TransactionCategories)
                .GroupBy(tc => tc.Category?.Name ?? "Okategoriserad")
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            insights.ImpulsePurchases = new ImpulsePurchaseInsight
            {
                TotalAmount = impulseAmount,
                TransactionCount = impulsePurchases.Count,
                PercentageOfTotal = impulsePercentage,
                MostCommonDay = impulseByDay != null ? SwedishDayNames[impulseByDay.Key] : null,
                TopCategory = impulseTopCategory?.Key,
                IsDetected = impulsePercentage > 5 // Threshold: >5% is considered significant
            };
        }

        // Identify common patterns
        insights.CommonPattern = IdentifyCommonPattern(transactions);

        // Category time patterns
        insights.CategoryPatterns = GenerateCategoryTimePatterns(transactions);

        return insights;
    }

    private List<CategoryTimePattern> GenerateCategoryTimePatterns(List<Transaction> transactions)
    {
        var patterns = new List<CategoryTimePattern>();

        var categoryTimeGroups = transactions
            .SelectMany(t => t.TransactionCategories.Select(tc => new
            {
                Category = tc.Category?.Name ?? "Okategoriserad",
                Weekday = GetMondayBasedWeekday(t.Date.DayOfWeek),
                TimeSlot = t.Date.Hour / 4,
                Amount = t.Amount
            }))
            .GroupBy(x => new { x.Category, x.Weekday, x.TimeSlot })
            .Select(g => new
            {
                g.Key.Category,
                g.Key.Weekday,
                g.Key.TimeSlot,
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(g => g.Amount)
            .Take(5) // Top 5 category-time patterns
            .ToList();

        foreach (var group in categoryTimeGroups)
        {
            var startHour = group.TimeSlot * 4;
            var endHour = startHour + 3;

            // Calculate percentage of total in this category
            var categoryTotal = transactions
                .Where(t => t.TransactionCategories.Any(tc => tc.Category?.Name == group.Category))
                .Sum(t => t.Amount);

            patterns.Add(new CategoryTimePattern
            {
                CategoryName = group.Category,
                Weekday = group.Weekday,
                TimeRange = $"{startHour:D2}-{endHour + 1:D2}",
                Amount = group.Amount,
                Percentage = categoryTotal > 0 ? (group.Amount / categoryTotal) * 100 : 0
            });
        }

        return patterns;
    }

    private string? IdentifyCommonPattern(List<Transaction> transactions)
    {
        if (!transactions.Any())
            return null;

        // Check for lunch pattern (11:00-14:00 on weekdays)
        var lunchTransactions = transactions
            .Where(t => t.Date.Hour >= 11 && t.Date.Hour <= 14 && 
                       GetMondayBasedWeekday(t.Date.DayOfWeek) < 5)
            .ToList();

        if (lunchTransactions.Count > transactions.Count * 0.2m) // >20% are lunch
        {
            return "Lunch på vardagar";
        }

        // Check for weekend shopping pattern
        var weekendTransactions = transactions
            .Where(t => GetMondayBasedWeekday(t.Date.DayOfWeek) >= 5)
            .ToList();

        if (weekendTransactions.Count > transactions.Count * 0.3m) // >30% on weekends
        {
            return "Shoppingmönster på helger";
        }

        // Check for evening expenses
        var eveningTransactions = transactions
            .Where(t => t.Date.Hour >= 18 && t.Date.Hour <= 22)
            .ToList();

        if (eveningTransactions.Count > transactions.Count * 0.4m) // >40% in evening
        {
            return "Kvällsutgifter dominerar";
        }

        return "Blandade utgiftsmönster";
    }

    private void CalculateIntensityLevels(ExpenseHeatmapData heatmapData)
    {
        if (heatmapData.MaxCellAmount == 0)
            return;

        // Define thresholds for diverging intensity levels
        // -3 = Very High Expense (Dark Red)
        // -2 = High Expense (Red)
        // -1 = Medium Expense (Light Red)
        // 0 = Neutral (Blue)
        // +1 = Medium Income (Light Green)
        // +2 = High Income (Green)
        // +3 = Very High Income (Dark Green)
        foreach (var weekdayDict in heatmapData.HeatmapCells.Values)
        {
            foreach (var cell in weekdayDict.Values)
            {
                // Calculate as percentage of max absolute net amount
                var percentage = heatmapData.MaxCellAmount > 0 
                    ? (cell.NetAmount / heatmapData.MaxCellAmount) * 100 
                    : 0;
                
                // Assign intensity level based on net amount (positive = income, negative = expense)
                if (cell.NetAmount >= 0)
                {
                    // Positive = Income (Green scale)
                    cell.IntensityLevel = percentage switch
                    {
                        >= 75 => 3,  // Very High Income (Dark Green)
                        >= 50 => 2,  // High Income (Green)
                        >= 25 => 1,  // Medium Income (Light Green)
                        _ => 0       // Neutral (Blue)
                    };
                }
                else
                {
                    // Negative = Expense (Red scale)
                    var absPercentage = Math.Abs(percentage);
                    cell.IntensityLevel = absPercentage switch
                    {
                        >= 75 => -3, // Very High Expense (Dark Red)
                        >= 50 => -2, // High Expense (Red)
                        >= 25 => -1, // Medium Expense (Light Red)
                        _ => 0       // Neutral (Blue)
                    };
                }
            }
        }
    }

    /// <summary>
    /// Convert .NET DayOfWeek (Sunday=0) to Monday-based (Monday=0, Sunday=6)
    /// </summary>
    private int GetMondayBasedWeekday(DayOfWeek dayOfWeek)
    {
        // .NET: Sunday=0, Monday=1, ..., Saturday=6
        // We want: Monday=0, Tuesday=1, ..., Sunday=6
        return dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
    }
}
