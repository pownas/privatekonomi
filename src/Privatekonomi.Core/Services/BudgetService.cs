using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class BudgetService : IBudgetService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;

    public BudgetService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<Budget>> GetAllBudgetsAsync()
    {
        var query = _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(b => b.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(b => b.StartDate).ToListAsync();
    }

    public async Task<Budget?> GetBudgetByIdAsync(int id)
    {
        var query = _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(b => b.UserId == _currentUserService.UserId);
        }

        return await query.FirstOrDefaultAsync(b => b.BudgetId == id);
    }

    public async Task<Budget> CreateBudgetAsync(Budget budget)
    {
        budget.CreatedAt = DateTime.UtcNow;
        
        // Set user ID for new budgets
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            budget.UserId = _currentUserService.UserId;
        }
        
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();
        return budget;
    }

    public async Task<Budget> UpdateBudgetAsync(Budget budget)
    {
        budget.UpdatedAt = DateTime.UtcNow;
        
        // Get the existing budget with its categories
        var existingBudget = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .FirstOrDefaultAsync(b => b.BudgetId == budget.BudgetId);
            
        if (existingBudget == null)
        {
            throw new InvalidOperationException($"Budget with ID {budget.BudgetId} not found");
        }

        // Update basic properties
        existingBudget.Name = budget.Name;
        existingBudget.Description = budget.Description;
        existingBudget.StartDate = budget.StartDate;
        existingBudget.EndDate = budget.EndDate;
        existingBudget.Period = budget.Period;
        existingBudget.TemplateType = budget.TemplateType;
        existingBudget.RolloverUnspent = budget.RolloverUnspent;
        existingBudget.HouseholdId = budget.HouseholdId;
        existingBudget.UpdatedAt = budget.UpdatedAt;

        // Remove all existing budget categories
        existingBudget.BudgetCategories.Clear();

        // Add new budget categories
        foreach (var bc in budget.BudgetCategories)
        {
            existingBudget.BudgetCategories.Add(new BudgetCategory
            {
                BudgetId = existingBudget.BudgetId,
                CategoryId = bc.CategoryId,
                PlannedAmount = bc.PlannedAmount
            });
        }

        await _context.SaveChangesAsync();
        return existingBudget;
    }

    public async Task DeleteBudgetAsync(int id)
    {
        var budget = await _context.Budgets.FindAsync(id);
        if (budget != null)
        {
            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Budget>> GetActiveBudgetsAsync()
    {
        var now = DateTime.Now;
        return await _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .Where(b => b.StartDate <= now && b.EndDate >= now)
            .ToListAsync() ?? new List<Budget>();
    }

    public async Task<Dictionary<int, decimal>> GetActualAmountsByCategoryAsync(int budgetId)
    {
        var budget = await _context.Budgets.FindAsync(budgetId);
        if (budget == null)
        {
            return new Dictionary<int, decimal>();
        }

        var transactions = await _context.Transactions
            .Include(t => t.TransactionCategories)
            .Where(t => t.Date >= budget.StartDate && t.Date <= budget.EndDate)
            .ToListAsync();

        var actualAmounts = new Dictionary<int, decimal>();
        
        foreach (var transaction in transactions)
        {
            foreach (var tc in transaction.TransactionCategories)
            {
                if (!actualAmounts.ContainsKey(tc.CategoryId))
                {
                    actualAmounts[tc.CategoryId] = 0;
                }
                
                // For expenses, use positive amounts; for income, use negative amounts
                actualAmounts[tc.CategoryId] += transaction.IsIncome ? -tc.Amount : tc.Amount;
            }
        }

        return actualAmounts;
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsForBudgetAsync(int budgetId)
    {
        var budget = await _context.Budgets.FindAsync(budgetId);
        if (budget == null)
        {
            return new List<Transaction>();
        }

        return await _context.Transactions
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.BankSource)
            .Where(t => t.Date >= budget.StartDate && t.Date <= budget.EndDate)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<Dictionary<int, List<Transaction>>> GetTransactionsByCategoryForBudgetAsync(int budgetId)
    {
        var budget = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.BudgetId == budgetId);
            
        if (budget == null)
        {
            return new Dictionary<int, List<Transaction>>();
        }

        var transactions = await _context.Transactions
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Include(t => t.BankSource)
            .Where(t => t.Date >= budget.StartDate && t.Date <= budget.EndDate && !t.IsIncome)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var transactionsByCategory = new Dictionary<int, List<Transaction>>();
        
        // Group transactions by their categories
        foreach (var transaction in transactions)
        {
            foreach (var tc in transaction.TransactionCategories)
            {
                if (!transactionsByCategory.TryGetValue(tc.CategoryId, out var categoryTransactions))
                {
                    categoryTransactions = new List<Transaction>();
                    transactionsByCategory[tc.CategoryId] = categoryTransactions;
                }

                // Only add if not already in the list (avoid duplicates)
                if (!categoryTransactions.Contains(transaction))
                {
                    categoryTransactions.Add(transaction);
                }
            }
        }

        return transactionsByCategory;
    }

    public async Task<Budget?> GetBudgetForCategoryAndMonthAsync(int categoryId, DateTime budgetDate)
    {
        // Get the first day of the month
        var startDate = new DateTime(budgetDate.Year, budgetDate.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = _context.Budgets
            .Include(b => b.BudgetCategories.Where(bc => bc.CategoryId == categoryId))
            .ThenInclude(bc => bc.Category)
            .Where(b => b.StartDate <= startDate && b.EndDate >= endDate && b.Period == BudgetPeriod.Monthly)
            .AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(b => b.UserId == _currentUserService.UserId);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Budget> CreateOrUpdateCategoryBudgetAsync(int categoryId, DateTime monthDate, decimal plannedAmount)
    {
        // Get the first and last day of the month
        var startDate = new DateTime(monthDate.Year, monthDate.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Check if a budget already exists for this month
        var existingBudget = await GetBudgetForCategoryAndMonthAsync(categoryId, monthDate);

        if (existingBudget != null)
        {
            // Update existing budget category
            var existingBudgetCategory = existingBudget.BudgetCategories.FirstOrDefault(bc => bc.CategoryId == categoryId);
            
            if (existingBudgetCategory != null)
            {
                // Update the planned amount
                existingBudgetCategory.PlannedAmount = plannedAmount;
            }
            else
            {
                // Add new budget category to existing budget
                existingBudget.BudgetCategories.Add(new BudgetCategory
                {
                    BudgetId = existingBudget.BudgetId,
                    CategoryId = categoryId,
                    PlannedAmount = plannedAmount
                });
            }

            await UpdateBudgetAsync(existingBudget);
            return existingBudget;
        }
        else
        {
            // Create new monthly budget
            var newBudget = new Budget
            {
                Name = $"Budget {startDate:yyyy-MM}",
                StartDate = startDate,
                EndDate = endDate,
                Period = BudgetPeriod.Monthly,
                BudgetCategories = new List<BudgetCategory>
                {
                    new BudgetCategory
                    {
                        CategoryId = categoryId,
                        PlannedAmount = plannedAmount
                    }
                }
            };

            return await CreateBudgetAsync(newBudget);
        }
    }

    public async Task<Budget?> CreateNextMonthBudgetAsync(int budgetId)
    {
        var existingBudget = await GetBudgetByIdAsync(budgetId);
        if (existingBudget == null)
        {
            return null;
        }

        // Only create next month budget for monthly budgets
        if (existingBudget.Period != BudgetPeriod.Monthly)
        {
            return null;
        }

        // Calculate next month dates
        var nextMonthStart = existingBudget.EndDate.AddDays(1);
        var nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1);

        // Check if a budget already exists for next month
        var existingNextMonthBudget = await _context.Budgets
            .Where(b => b.StartDate == nextMonthStart && b.EndDate == nextMonthEnd)
            .FirstOrDefaultAsync();

        if (existingNextMonthBudget != null)
        {
            return null; // Budget already exists for next month
        }

        // Create new budget for next month based on the existing budget
        var nextMonthBudget = new Budget
        {
            Name = $"Budget {nextMonthStart:yyyy-MM}",
            Description = existingBudget.Description,
            StartDate = nextMonthStart,
            EndDate = nextMonthEnd,
            Period = BudgetPeriod.Monthly,
            TemplateType = existingBudget.TemplateType,
            RolloverUnspent = existingBudget.RolloverUnspent,
            HouseholdId = existingBudget.HouseholdId,
            UserId = existingBudget.UserId,
            BudgetCategories = existingBudget.BudgetCategories.Select(bc => new BudgetCategory
            {
                CategoryId = bc.CategoryId,
                PlannedAmount = bc.PlannedAmount
            }).ToList()
        };

        return await CreateBudgetAsync(nextMonthBudget);
    }

    public async Task<IEnumerable<Budget>> CreateNextMonthBudgetsForAllActiveAsync()
    {
        var today = DateTime.Today;
        var currentMonthEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        
        // Check if we're in the last 3 days of the month
        var daysUntilMonthEnd = (currentMonthEnd - today).Days;
        if (daysUntilMonthEnd > 3)
        {
            return new List<Budget>(); // Not time to create next month budgets yet
        }

        // Get all monthly budgets that end this month
        var activeMonthlyBudgets = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .Where(b => b.Period == BudgetPeriod.Monthly && 
                       b.EndDate.Year == today.Year && 
                       b.EndDate.Month == today.Month)
            .ToListAsync();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            activeMonthlyBudgets = activeMonthlyBudgets
                .Where(b => b.UserId == _currentUserService.UserId)
                .ToList();
        }

        var createdBudgets = new List<Budget>();

        foreach (var budget in activeMonthlyBudgets)
        {
            var nextMonthBudget = await CreateNextMonthBudgetAsync(budget.BudgetId);
            if (nextMonthBudget != null)
            {
                createdBudgets.Add(nextMonthBudget);
            }
        }

        return createdBudgets;
    }
}
