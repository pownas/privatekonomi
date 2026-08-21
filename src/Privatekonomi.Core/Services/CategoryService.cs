using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;

namespace Privatekonomi.Core.Services;

public class CategoryService : ICategoryService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;
    private static readonly Random _random = new();

    public CategoryService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private static string GenerateRandomHexColor()
    {
        // Generate a random color that is not too dark or too light for good visibility
        var r = _random.Next(50, 230);
        var g = _random.Next(50, 230);
        var b = _random.Next(50, 230);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.SubCategories)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        
        // Generate a random color if not provided
        if (string.IsNullOrWhiteSpace(category.Color) || category.Color == "#000000")
        {
            category.Color = GenerateRandomHexColor();
        }
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateCategoryAsync(Category category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        _context.Entry(category).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByCategoryAsync(int categoryId, DateTime? from = null, DateTime? toDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.BankSource)
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == categoryId))
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(t => t.UserId == _currentUserService.UserId);
        }

        if (from.HasValue)
        {
            query = query.Where(t => t.Date >= from.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.Date <= toDate.Value);
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<CategoryStatistics> GetCategoryStatisticsAsync(int categoryId, int months)
    {
        var category = await GetCategoryByIdAsync(categoryId);
        if (category == null)
        {
            throw new ArgumentException($"Category with ID {categoryId} not found");
        }

        var toDate = DateTime.Today;
        var fromDate = toDate.AddMonths(-months);

        var transactions = await GetTransactionsByCategoryAsync(categoryId, fromDate, toDate);

        var statistics = new CategoryStatistics
        {
            CategoryId = categoryId,
            CategoryName = category.Name,
            CategoryColor = category.Color
        };

        // Calculate totals
        statistics.TotalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        statistics.TotalExpenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        statistics.NetAmount = statistics.TotalIncome - statistics.TotalExpenses;

        // Calculate monthly breakdown
        var monthlyData = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthlyAmount
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthLabel = $"{g.Key.Year}-{g.Key.Month:D2}",
                Income = g.Where(t => t.IsIncome).Sum(t => t.Amount),
                Expenses = g.Where(t => !t.IsIncome).Sum(t => t.Amount),
                NetAmount = g.Where(t => t.IsIncome).Sum(t => t.Amount) - g.Where(t => !t.IsIncome).Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        // Fill in missing months with zero values
        var allMonths = new List<MonthlyAmount>();
        var currentDate = fromDate;
        while (currentDate <= toDate)
        {
            var existingMonth = monthlyData.FirstOrDefault(m => m.Year == currentDate.Year && m.Month == currentDate.Month);
            if (existingMonth != null)
            {
                allMonths.Add(existingMonth);
            }
            else
            {
                allMonths.Add(new MonthlyAmount
                {
                    Year = currentDate.Year,
                    Month = currentDate.Month,
                    MonthLabel = $"{currentDate.Year}-{currentDate.Month:D2}",
                    Income = 0,
                    Expenses = 0,
                    NetAmount = 0,
                    TransactionCount = 0
                });
            }
            currentDate = currentDate.AddMonths(1);
        }

        statistics.MonthlyBreakdown = allMonths;

        // Calculate 12-month average
        if (months >= 12)
        {
            var last12Months = allMonths.TakeLast(12).ToList();
            statistics.Average12Months = last12Months.Any() 
                ? Math.Abs(last12Months.Average(m => m.NetAmount)) 
                : 0;
        }

        // Calculate 24-month average
        if (months >= 24)
        {
            var last24Months = allMonths.TakeLast(24).ToList();
            statistics.Average24Months = last24Months.Any() 
                ? Math.Abs(last24Months.Average(m => m.NetAmount)) 
                : 0;
        }

        // Calculate trend (compare first half vs second half of the period)
        if (allMonths.Count >= 2)
        {
            var halfPoint = allMonths.Count / 2;
            var firstHalfAvg = allMonths.Take(halfPoint).Average(m => Math.Abs(m.NetAmount));
            var secondHalfAvg = allMonths.Skip(halfPoint).Average(m => Math.Abs(m.NetAmount));
            
            if (firstHalfAvg > 0)
            {
                statistics.TrendPercentage = ((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100;
                statistics.IsIncreasing = statistics.TrendPercentage > 0;
            }
        }

        return statistics;
    }

    public async Task<Category?> ResetSystemCategoryAsync(int categoryId)
    {
        var category = await GetCategoryByIdAsync(categoryId);
        if (category == null || !category.IsSystemCategory)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(category.OriginalName))
        {
            category.Name = category.OriginalName;
        }
        
        if (!string.IsNullOrWhiteSpace(category.OriginalColor))
        {
            category.Color = category.OriginalColor;
        }

        if (!string.IsNullOrWhiteSpace(category.OriginalAccountNumber))
        {
            category.AccountNumber = category.OriginalAccountNumber;
        }

        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<IEnumerable<CategoryStatistics>> GetAllCategoryStatisticsAsync(int months)
    {
        var categories = await GetAllCategoriesAsync();
        var statisticsList = new List<CategoryStatistics>();

        foreach (var category in categories)
        {
            try
            {
                var stats = await GetCategoryStatisticsAsync(category.CategoryId, months);
                statisticsList.Add(stats);
            }
            catch (Exception)
            {
                // Skip categories that have errors
                continue;
            }
        }

        return statisticsList.OrderByDescending(s => Math.Abs(s.NetAmount));
    }
}
