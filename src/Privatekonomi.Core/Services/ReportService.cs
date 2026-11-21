using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class ReportService : IReportService
{
    private readonly PrivatekonomyContext _context;

    public ReportService(PrivatekonomyContext context)
    {
        _context = context;
    }

    public async Task<CashFlowReport> GetCashFlowReportAsync(DateTime fromDate, DateTime toDate, string groupBy = "month", int? householdId = null)
    {
        fromDate = fromDate.Date;
        toDate = toDate.Date;
        var query = _context.Transactions
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => t.Date >= fromDate && t.Date <= toDate)
            .Where(t => !t.IsInternalTransfer &&
                        !t.TransactionCategories.Any(tc => tc.Category != null && tc.Category.IsInternalTransferCategory));

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();

        var periods = new List<CashFlowPeriod>();

        if (string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase))
        {
            var monthlyGroups = transactions.GroupBy(t => new { t.Date.Year, t.Date.Month });
            
            foreach (var group in monthlyGroups.OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month))
            {
                var income = group.Where(t => t.IsIncome).Sum(t => Math.Abs(t.Amount));
                var expenses = group.Where(t => !t.IsIncome).Sum(t => Math.Abs(t.Amount));
                
                periods.Add(new CashFlowPeriod
                {
                    Period = $"{group.Key.Year}-{group.Key.Month:D2}",
                    StartDate = new DateTime(group.Key.Year, group.Key.Month, 1),
                    EndDate = new DateTime(group.Key.Year, group.Key.Month, DateTime.DaysInMonth(group.Key.Year, group.Key.Month)),
                    Income = income,
                    Expenses = expenses,
                    NetFlow = income - expenses
                });
            }
        }
        else if (string.Equals(groupBy, "week", StringComparison.OrdinalIgnoreCase))
        {
            // Group by ISO week
            var weeklyGroups = transactions.GroupBy(t => new 
            { 
                Year = System.Globalization.ISOWeek.GetYear(t.Date),
                Week = System.Globalization.ISOWeek.GetWeekOfYear(t.Date)
            });
            
            foreach (var group in weeklyGroups.OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week))
            {
                var income = group.Where(t => t.IsIncome).Sum(t => Math.Abs(t.Amount));
                var expenses = group.Where(t => !t.IsIncome).Sum(t => Math.Abs(t.Amount));
                
                periods.Add(new CashFlowPeriod
                {
                    Period = $"{group.Key.Year}-W{group.Key.Week:D2}",
                    StartDate = System.Globalization.ISOWeek.ToDateTime(group.Key.Year, group.Key.Week, DayOfWeek.Monday),
                    EndDate = System.Globalization.ISOWeek.ToDateTime(group.Key.Year, group.Key.Week, DayOfWeek.Sunday),
                    Income = income,
                    Expenses = expenses,
                    NetFlow = income - expenses
                });
            }
        }

        var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => Math.Abs(t.Amount));
        var totalExpenses = transactions.Where(t => !t.IsIncome).Sum(t => Math.Abs(t.Amount));

        return new CashFlowReport
        {
            Periods = periods,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = totalIncome - totalExpenses,
            GroupBy = groupBy
        };
    }

    public async Task<NetWorthReport> GetNetWorthReportAsync(string? userId = null)
    {
        // Query investments with optional user filter
        var investmentsQuery = _context.Investments.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
        {
            investmentsQuery = investmentsQuery.Where(i => i.UserId == userId);
        }
        var investments = await investmentsQuery.ToListAsync();

        // Query assets with optional user filter
        var assetsQuery = _context.Assets.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
        {
            assetsQuery = assetsQuery.Where(a => a.UserId == userId);
        }
        var assets = await assetsQuery.ToListAsync();

        // Query loans with optional user filter
        var loansQuery = _context.Loans.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
        {
            loansQuery = loansQuery.Where(l => l.UserId == userId);
        }
        var loans = await loansQuery.ToListAsync();

        var assetItems = new List<AssetItem>();
        var liabilityItems = new List<LiabilityItem>();

        // Add investments
        foreach (var investment in investments)
        {
            assetItems.Add(new AssetItem
            {
                Name = investment.Name,
                Type = "Investment",
                Value = investment.TotalValue
            });
        }

        // Add other assets
        foreach (var asset in assets)
        {
            assetItems.Add(new AssetItem
            {
                Name = asset.Name,
                Type = asset.Type,
                Value = asset.CurrentValue
            });
        }

        // Add loans as liabilities
        foreach (var loan in loans)
        {
            liabilityItems.Add(new LiabilityItem
            {
                Name = loan.Name,
                Type = loan.Type,
                Amount = loan.CurrentBalance
            });
        }

        var totalAssets = assetItems.Sum(a => a.Value);
        var totalInvestments = investments.Sum(i => i.TotalValue);
        var totalLiabilities = liabilityItems.Sum(l => l.Amount);
        var netWorth = totalAssets - totalLiabilities;

        // Create historical data for the last 12 months
        var history = await CreateNetWorthHistory(userId, 12);
        
        // Calculate percentage change from previous month
        var percentageChange = CalculatePercentageChange(history, netWorth);

        return new NetWorthReport
        {
            TotalAssets = totalAssets,
            TotalInvestments = totalInvestments,
            TotalLiabilities = totalLiabilities,
            NetWorth = netWorth,
            PercentageChange = percentageChange,
            Assets = assetItems,
            Liabilities = liabilityItems,
            History = history,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private async Task<List<NetWorthDataPoint>> CreateNetWorthHistory(string? userId, int months)
    {
        var history = new List<NetWorthDataPoint>();
        var endDate = DateTime.Today;
        var startDate = endDate.AddMonths(-months);

        // Get snapshots for the date range
        var snapshotsQuery = _context.NetWorthSnapshots
            .Where(s => s.Date >= startDate && s.Date <= endDate);
        
        if (!string.IsNullOrEmpty(userId))
        {
            snapshotsQuery = snapshotsQuery.Where(s => s.UserId == userId);
        }

        var snapshots = await snapshotsQuery
            .OrderBy(s => s.Date)
            .ToListAsync();

        // Group by month and take the latest snapshot per month
        var monthlySnapshots = snapshots
            .GroupBy(s => new { s.Date.Year, s.Date.Month })
            .Select(g => g.OrderByDescending(s => s.Date).First())
            .OrderBy(s => s.Date)
            .ToList();

        // If we don't have historical snapshots, calculate current values for each month
        if (!monthlySnapshots.Any())
        {
            // Create data points based on current values
            // This is a simplified approach - in production, you'd want actual historical data
            for (int i = months - 1; i >= 0; i--)
            {
                var date = DateTime.Today.AddMonths(-i);
                var monthStart = new DateTime(date.Year, date.Month, 1);
                
                // For now, we'll use current values as approximation
                // In a real scenario, you'd query historical data
                var investmentsQuery = _context.Investments.AsQueryable();
                var assetsQuery = _context.Assets.AsQueryable();
                var loansQuery = _context.Loans.AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    investmentsQuery = investmentsQuery.Where(inv => inv.UserId == userId);
                    assetsQuery = assetsQuery.Where(a => a.UserId == userId);
                    loansQuery = loansQuery.Where(l => l.UserId == userId);
                }

                // Materialize the queries before using computed properties
                var investments = await investmentsQuery.ToListAsync();
                var assets = await assetsQuery.ToListAsync();
                var loans = await loansQuery.ToListAsync();

                var investmentValue = investments.Sum(inv => inv.TotalValue);
                var assetValue = assets.Sum(a => a.CurrentValue);
                var loanValue = loans.Sum(l => l.CurrentBalance);

                var totalAssets = investmentValue + assetValue;
                var totalLiabilities = loanValue;

                history.Add(new NetWorthDataPoint
                {
                    Date = monthStart,
                    Assets = totalAssets,
                    Liabilities = totalLiabilities,
                    NetWorth = totalAssets - totalLiabilities
                });
            }
        }
        else
        {
            // Use actual snapshots
            foreach (var snapshot in monthlySnapshots)
            {
                history.Add(new NetWorthDataPoint
                {
                    Date = snapshot.Date,
                    Assets = snapshot.TotalAssets,
                    Liabilities = snapshot.TotalLiabilities,
                    NetWorth = snapshot.NetWorth
                });
            }

            // If we don't have a full 12 months, pad with current values
            while (history.Count < months)
            {
                var lastDate = history.Any() ? history[0].Date : DateTime.Today;
                var newDate = new DateTime(lastDate.Year, lastDate.Month, 1).AddMonths(-1);
                
                history.Insert(0, new NetWorthDataPoint
                {
                    Date = newDate,
                    Assets = history.FirstOrDefault()?.Assets ?? 0,
                    Liabilities = history.FirstOrDefault()?.Liabilities ?? 0,
                    NetWorth = history.FirstOrDefault()?.NetWorth ?? 0
                });
            }
        }

        return history.OrderBy(h => h.Date).ToList();
    }

    private decimal CalculatePercentageChange(List<NetWorthDataPoint> history, decimal currentNetWorth)
    {
        if (!history.Any() || history.Count < 2)
            return 0;

        // Get the previous month's net worth (second to last in history)
        var previousNetWorth = history[history.Count - 2].NetWorth;
        
        if (previousNetWorth == 0)
            return 0;

        return ((currentNetWorth - previousNetWorth) / Math.Abs(previousNetWorth)) * 100;
    }

    public async Task<TrendAnalysis> GetTrendAnalysisAsync(int? categoryId, int months = 6, int? householdId = null)
    {
        var fromDate = DateTime.Now.AddMonths(-months);
        
        var query = _context.Transactions
            .Where(t => t.Date >= fromDate);

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        string categoryName = "Alla kategorier";
        
        if (categoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(categoryId.Value);
            categoryName = category?.Name ?? "Okänd kategori";
            
            query = query.Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == categoryId.Value));
        }

        var transactions = await query.ToListAsync();

        var monthlyGroups = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyTrend
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Amount = g.Where(t => !t.IsIncome).Sum(t => t.Amount)
            })
            .ToList();

        var average = monthlyGroups.Any() ? monthlyGroups.Average(m => m.Amount) : 0;
        
        // Simple trend calculation: compare first half vs second half
        var trend = 0m;
        var trendDirection = "Stable";
        
        if (monthlyGroups.Count >= 2)
        {
            var halfPoint = monthlyGroups.Count / 2;
            var firstHalfAvg = monthlyGroups.Take(halfPoint).Average(m => m.Amount);
            var secondHalfAvg = monthlyGroups.Skip(halfPoint).Average(m => m.Amount);
            
            trend = secondHalfAvg - firstHalfAvg;
            
            if (Math.Abs(trend) > average * 0.1m) // More than 10% change
            {
                trendDirection = trend > 0 ? "Increasing" : "Decreasing";
            }
        }

        return new TrendAnalysis
        {
            CategoryName = categoryName,
            MonthlyTrends = monthlyGroups,
            AverageMonthly = average,
            Trend = trend,
            TrendDirection = trendDirection
        };
    }

    public async Task<IEnumerable<TopMerchant>> GetTopMerchantsAsync(int limit = 10, DateTime? fromDate = null, DateTime? toDate = null, int? householdId = null)
    {
        var query = _context.Transactions
            .Where(t => !t.IsIncome); // Only expenses

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.Date <= toDate.Value);
        }

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var transactions = await query.ToListAsync();

        // Group by Description (which often contains merchant name)
        var merchants = transactions
            .GroupBy(t => GetMerchantName(t.Description))
            .Select(g => new TopMerchant
            {
                Name = g.Key,
                TotalSpent = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageTransaction = g.Average(t => t.Amount)
            })
            .OrderByDescending(m => m.TotalSpent)
            .Take(limit)
            .ToList();

        return merchants;
    }

    private string GetMerchantName(string description)
    {
        // Extract merchant name from description
        // This is a simple implementation - could be improved with more sophisticated parsing
        if (string.IsNullOrWhiteSpace(description))
            return "Okänd";

        // Remove common transaction words and trim
        var cleaned = description
            .Replace("Köp", "")
            .Replace("Betalning", "")
            .Replace("Till:", "")
            .Trim();

        // Take first part (usually merchant name)
        var parts = cleaned.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : description;
    }

    public async Task<NetWorthHistoryReport> GetNetWorthHistoryAsync(string groupBy = "month", DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Default date range to last 12 months if not specified
        var endDate = toDate ?? DateTime.Today;
        var startDate = fromDate ?? endDate.AddMonths(-12);

        // Get snapshots within date range
        var snapshots = await _context.NetWorthSnapshots
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .OrderBy(s => s.Date)
            .ToListAsync();

        var periods = new List<NetWorthHistoryPeriod>();

        if (string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase))
        {
            // Group snapshots by month and take the latest snapshot per month
            var monthlyGroups = snapshots
                .GroupBy(s => new { s.Date.Year, s.Date.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);

            foreach (var group in monthlyGroups)
            {
                // Take the last snapshot of the month
                var snapshot = group.OrderByDescending(s => s.Date).First();
                
                periods.Add(new NetWorthHistoryPeriod
                {
                    Period = $"{group.Key.Year}-{group.Key.Month:D2}",
                    Date = snapshot.Date,
                    NetWorth = snapshot.NetWorth,
                    TotalAssets = snapshot.TotalAssets,
                    TotalLiabilities = snapshot.TotalLiabilities,
                    BankBalance = snapshot.BankBalance,
                    InvestmentValue = snapshot.InvestmentValue,
                    PhysicalAssetValue = snapshot.PhysicalAssetValue,
                    LoanBalance = snapshot.LoanBalance
                });
            }
        }
        else if (string.Equals(groupBy, "quarter", StringComparison.OrdinalIgnoreCase))
        {
            // Group by quarter
            var quarterlyGroups = snapshots
                .GroupBy(s => new { s.Date.Year, Quarter = (s.Date.Month - 1) / 3 + 1 })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Quarter);

            foreach (var group in quarterlyGroups)
            {
                var snapshot = group.OrderByDescending(s => s.Date).First();
                
                periods.Add(new NetWorthHistoryPeriod
                {
                    Period = $"{group.Key.Year}-Q{group.Key.Quarter}",
                    Date = snapshot.Date,
                    NetWorth = snapshot.NetWorth,
                    TotalAssets = snapshot.TotalAssets,
                    TotalLiabilities = snapshot.TotalLiabilities,
                    BankBalance = snapshot.BankBalance,
                    InvestmentValue = snapshot.InvestmentValue,
                    PhysicalAssetValue = snapshot.PhysicalAssetValue,
                    LoanBalance = snapshot.LoanBalance
                });
            }
        }
        else if (string.Equals(groupBy, "year", StringComparison.OrdinalIgnoreCase))
        {
            // Group by year
            var yearlyGroups = snapshots
                .GroupBy(s => s.Date.Year)
                .OrderBy(g => g.Key);

            foreach (var group in yearlyGroups)
            {
                var snapshot = group.OrderByDescending(s => s.Date).First();
                
                periods.Add(new NetWorthHistoryPeriod
                {
                    Period = $"{group.Key}",
                    Date = snapshot.Date,
                    NetWorth = snapshot.NetWorth,
                    TotalAssets = snapshot.TotalAssets,
                    TotalLiabilities = snapshot.TotalLiabilities,
                    BankBalance = snapshot.BankBalance,
                    InvestmentValue = snapshot.InvestmentValue,
                    PhysicalAssetValue = snapshot.PhysicalAssetValue,
                    LoanBalance = snapshot.LoanBalance
                });
            }
        }

        var currentNetWorth = periods.Any() ? periods.Last().NetWorth : 0;
        var startNetWorth = periods.Any() ? periods.First().NetWorth : 0;
        var netWorthChange = currentNetWorth - startNetWorth;
        var netWorthChangePercentage = startNetWorth != 0 ? (netWorthChange / Math.Abs(startNetWorth)) * 100 : 0;

        return new NetWorthHistoryReport
        {
            Periods = periods,
            CurrentNetWorth = currentNetWorth,
            StartNetWorth = startNetWorth,
            NetWorthChange = netWorthChange,
            NetWorthChangePercentage = netWorthChangePercentage,
            StartDate = startDate,
            EndDate = endDate,
            GroupBy = groupBy
        };
    }

    public async Task<NetWorthSnapshot> CreateNetWorthSnapshotAsync(bool isManual = false, string? notes = null)
    {
        // Calculate current net worth
        var investments = await _context.Investments.ToListAsync();
        var assets = await _context.Assets.ToListAsync();
        var loans = await _context.Loans.ToListAsync();
        var bankSources = await _context.BankSources.ToListAsync();

        var bankBalance = bankSources.Sum(b => b.CurrentBalance);
        var investmentValue = investments.Sum(i => i.TotalValue);
        var physicalAssetValue = assets.Sum(a => a.CurrentValue);
        var totalAssets = bankBalance + investmentValue + physicalAssetValue;

        var loanBalance = loans.Sum(l => l.Amount);
        var totalLiabilities = loanBalance;

        var netWorth = totalAssets - totalLiabilities;

        var snapshot = new NetWorthSnapshot
        {
            Date = DateTime.Today,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = netWorth,
            BankBalance = bankBalance,
            InvestmentValue = investmentValue,
            PhysicalAssetValue = physicalAssetValue,
            LoanBalance = loanBalance,
            IsManual = isManual,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.NetWorthSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        return snapshot;
    }

    public async Task<PeriodComparisonReport> GetPeriodComparisonAsync(DateTime? referenceDate = null, int? householdId = null)
    {
        var refDate = referenceDate ?? DateTime.Today;
        
        // Define current period (current month)
        var currentPeriodStart = new DateTime(refDate.Year, refDate.Month, 1);
        var currentPeriodEnd = currentPeriodStart.AddMonths(1).AddDays(-1);
        
        // Define previous period (previous month)
        var previousPeriodStart = currentPeriodStart.AddMonths(-1);
        var previousPeriodEnd = previousPeriodStart.AddMonths(1).AddDays(-1);
        
        // Define year ago period (same month last year)
        var yearAgoPeriodStart = currentPeriodStart.AddYears(-1);
        var yearAgoPeriodEnd = yearAgoPeriodStart.AddMonths(1).AddDays(-1);

        // Get transactions for all periods
        var query = _context.Transactions
            .Include(t => t.TransactionCategories)
            .ThenInclude(tc => tc.Category)
            .Where(t => !t.IsInternalTransfer &&
                        !t.TransactionCategories.Any(tc => tc.Category != null && tc.Category.IsInternalTransferCategory))
            .AsQueryable();
        
        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var allTransactions = await query
            .Where(t => t.Date >= yearAgoPeriodStart && t.Date <= currentPeriodEnd)
            .ToListAsync();

        // Current period
        var currentTransactions = allTransactions.Where(t => t.Date >= currentPeriodStart && t.Date <= currentPeriodEnd).ToList();
        var currentIncome = currentTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var currentExpenses = currentTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var currentNetFlow = currentIncome - currentExpenses;

        // Previous period
        var previousTransactions = allTransactions.Where(t => t.Date >= previousPeriodStart && t.Date <= previousPeriodEnd).ToList();
        var previousIncome = previousTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var previousExpenses = previousTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var previousNetFlow = previousIncome - previousExpenses;

        // Year ago period
        var yearAgoTransactions = allTransactions.Where(t => t.Date >= yearAgoPeriodStart && t.Date <= yearAgoPeriodEnd).ToList();
        var yearAgoIncome = yearAgoTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var yearAgoExpenses = yearAgoTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var yearAgoNetFlow = yearAgoIncome - yearAgoExpenses;

        // Calculate sparkline data (last 6 months of expenses)
        var sparklineData = new List<double>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = currentPeriodStart.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthExpenses = allTransactions
                .Where(t => !t.IsIncome && t.Date >= monthStart && t.Date <= monthEnd)
                .Sum(t => t.Amount);
            sparklineData.Add((double)monthExpenses);
        }

        return new PeriodComparisonReport
        {
            Income = CreatePeriodComparison(currentIncome, previousIncome, yearAgoIncome, true),
            Expenses = CreatePeriodComparison(currentExpenses, previousExpenses, yearAgoExpenses, false),
            NetFlow = CreatePeriodComparison(currentNetFlow, previousNetFlow, yearAgoNetFlow, true),
            SparklineData = sparklineData,
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd,
            PreviousPeriodStart = previousPeriodStart,
            PreviousPeriodEnd = previousPeriodEnd,
            YearAgoPeriodStart = yearAgoPeriodStart,
            YearAgoPeriodEnd = yearAgoPeriodEnd
        };
    }

    private PeriodComparison CreatePeriodComparison(decimal current, decimal previous, decimal yearAgo, bool higherIsBetter)
    {
        var changeFromPrevious = current - previous;
        var changeFromYearAgo = current - yearAgo;
        
        var percentageFromPrevious = previous != 0 ? (changeFromPrevious / previous) * 100 : 0;
        var percentageFromYearAgo = yearAgo != 0 ? (changeFromYearAgo / yearAgo) * 100 : 0;

        // Determine trend direction based on whether higher values are better
        string trendDirection;
        if (Math.Abs(percentageFromPrevious) < 5)
        {
            trendDirection = "Stable";
        }
        else if (higherIsBetter)
        {
            trendDirection = changeFromPrevious > 0 ? "Improving" : "Worsening";
        }
        else
        {
            trendDirection = changeFromPrevious < 0 ? "Improving" : "Worsening";
        }

        return new PeriodComparison
        {
            CurrentPeriod = current,
            PreviousPeriod = previous,
            YearAgoPeriod = yearAgo,
            ChangeFromPrevious = changeFromPrevious,
            ChangeFromYearAgo = changeFromYearAgo,
            PercentageChangeFromPrevious = percentageFromPrevious,
            PercentageChangeFromYearAgo = percentageFromYearAgo,
            TrendDirection = trendDirection
        };
    }

    public async Task<HealthScoreReport> GetHealthScoreAsync(int? householdId = null)
    {
        var report = new HealthScoreReport
        {
            CalculatedAt = DateTime.UtcNow
        };

        // Calculate each component
        report.SavingsRate = await CalculateSavingsRateScoreAsync(householdId);
        report.DebtLevel = await CalculateDebtLevelScoreAsync(householdId);
        report.EmergencyFund = await CalculateEmergencyFundScoreAsync(householdId);
        report.BudgetAdherence = await CalculateBudgetAdherenceScoreAsync(householdId);
        report.InvestmentDiversification = await CalculateInvestmentDiversificationScoreAsync(householdId);
        report.IncomeStability = await CalculateIncomeStabilityScoreAsync(householdId);

        // Calculate total score
        report.TotalScore = report.SavingsRate.Score + 
                           report.DebtLevel.Score + 
                           report.EmergencyFund.Score + 
                           report.BudgetAdherence.Score + 
                           report.InvestmentDiversification.Score + 
                           report.IncomeStability.Score;

        // Determine health level
        report.HealthLevel = report.TotalScore switch
        {
            >= 80 => "Utmärkt",
            >= 60 => "Bra",
            >= 40 => "Godkänt",
            _ => "Behöver förbättras"
        };

        // Identify strengths and improvement areas
        var components = new[] 
        { 
            report.SavingsRate, 
            report.DebtLevel, 
            report.EmergencyFund, 
            report.BudgetAdherence, 
            report.InvestmentDiversification, 
            report.IncomeStability 
        };

        foreach (var component in components.OrderByDescending(c => c.Score))
        {
            var percentage = component.MaxScore > 0 ? (component.Score * 100.0 / component.MaxScore) : 0;
            if (percentage >= 80 && report.Strengths.Count < 3)
            {
                var valueText = component.Value.HasValue ? $" ({component.Value.Value:F1}{component.Unit})" : "";
                report.Strengths.Add($"{component.Name}{valueText}");
            }
            else if (percentage < 60 && report.ImprovementAreas.Count < 3)
            {
                report.ImprovementAreas.Add(component.Description);
            }
        }

        // Get historical data (last 12 months)
        var historyStart = DateTime.Today.AddMonths(-12);
        var snapshots = await _context.NetWorthSnapshots
            .Where(s => s.Date >= historyStart)
            .OrderBy(s => s.Date)
            .ToListAsync();

        // For now, we'll add placeholder history points
        // In a real implementation, we'd store historical health scores
        report.History = new List<HealthScoreHistoryPoint>();

        return report;
    }

    private async Task<HealthScoreComponent> CalculateSavingsRateScoreAsync(int? householdId)
    {
        // Calculate savings rate for the last 3 months
        var threeMonthsAgo = DateTime.Today.AddMonths(-3);
        
        var query = _context.Transactions
            .Where(t => t.Date >= threeMonthsAgo);

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var transactions = await query.ToListAsync();
        
        var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var totalExpenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        
        var savingsRate = totalIncome > 0 ? ((totalIncome - totalExpenses) / totalIncome) * 100 : 0;
        
        // Score: 0-20 points based on savings rate
        var score = savingsRate switch
        {
            >= 20 => 20,
            >= 15 => 18,
            >= 10 => 15,
            >= 5 => 10,
            >= 0 => 5,
            _ => 0
        };

        var status = savingsRate switch
        {
            >= 20 => "Utmärkt",
            >= 10 => "Bra",
            >= 5 => "Godkänt",
            _ => "Låg"
        };

        return new HealthScoreComponent
        {
            Name = "Sparprocent",
            Score = score,
            MaxScore = 20,
            Value = savingsRate,
            Unit = "%",
            Status = status,
            Description = savingsRate < 10 ? "Öka sparandet till minst 10%" : ""
        };
    }

    private async Task<HealthScoreComponent> CalculateDebtLevelScoreAsync(int? householdId)
    {
        var loans = await _context.Loans.ToListAsync();
        var totalDebt = loans.Sum(l => l.CurrentBalance);
        
        // Get annual income
        var oneYearAgo = DateTime.Today.AddYears(-1);
        var query = _context.Transactions
            .Where(t => t.Date >= oneYearAgo && t.IsIncome);

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var annualIncome = await query.SumAsync(t => t.Amount);
        
        var debtToIncomeRatio = annualIncome > 0 ? (totalDebt / annualIncome) * 100 : 0;
        
        // Score: 0-20 points (lower debt is better)
        var score = debtToIncomeRatio switch
        {
            0 => 20,
            <= 100 => 18,
            <= 200 => 15,
            <= 300 => 10,
            <= 400 => 5,
            _ => 0
        };

        var status = debtToIncomeRatio switch
        {
            0 => "Utmärkt",
            <= 150 => "Bra",
            <= 300 => "Godkänt",
            _ => "Hög"
        };

        return new HealthScoreComponent
        {
            Name = "Skuldsättning",
            Score = score,
            MaxScore = 20,
            Value = debtToIncomeRatio,
            Unit = "%",
            Status = status,
            Description = debtToIncomeRatio > 200 ? "Minska skulder i förhållande till inkomst" : ""
        };
    }

    private async Task<HealthScoreComponent> CalculateEmergencyFundScoreAsync(int? householdId)
    {
        // Calculate monthly expenses (average of last 3 months)
        var threeMonthsAgo = DateTime.Today.AddMonths(-3);
        
        var query = _context.Transactions
            .Where(t => t.Date >= threeMonthsAgo && !t.IsIncome);

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var monthlyExpenses = await query.AverageAsync(t => (decimal?)t.Amount) ?? 0;
        
        // Get liquid assets (bank balances + liquid investments)
        var bankSources = await _context.BankSources.ToListAsync();
        var liquidAssets = bankSources.Sum(b => b.CurrentBalance);
        
        var monthsOfExpenses = monthlyExpenses > 0 ? liquidAssets / monthlyExpenses : 0;
        
        // Score: 0-20 points based on months of emergency fund
        var score = monthsOfExpenses switch
        {
            >= 6 => 20,
            >= 4 => 16,
            >= 3 => 12,
            >= 2 => 8,
            >= 1 => 4,
            _ => 0
        };

        var status = monthsOfExpenses switch
        {
            >= 6 => "Utmärkt",
            >= 3 => "Bra",
            >= 1 => "Godkänt",
            _ => "Låg"
        };

        return new HealthScoreComponent
        {
            Name = "Buffert",
            Score = score,
            MaxScore = 20,
            Value = monthsOfExpenses,
            Unit = " månader",
            Status = status,
            Description = monthsOfExpenses < 3 ? "Bygg upp minst 3 månaders buffert" : ""
        };
    }

    private async Task<HealthScoreComponent> CalculateBudgetAdherenceScoreAsync(int? householdId)
    {
        // Get current month's budget (active budget overlapping current month)
        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var currentMonthEnd = currentMonth.AddMonths(1).AddDays(-1);
        
        var budget = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.StartDate <= currentMonthEnd && b.EndDate >= currentMonth);

        if (budget == null || !budget.BudgetCategories.Any())
        {
            return new HealthScoreComponent
            {
                Name = "Budgetföljning",
                Score = 0,
                MaxScore = 15,
                Status = "Saknas",
                Description = "Skapa en budget för att spåra utgifter"
            };
        }

        // Calculate budget adherence for each category
        var adherenceScores = new List<decimal>();
        
        foreach (var budgetCategory in budget.BudgetCategories)
        {
            var spent = await _context.Transactions
                .Where(t => t.Date >= currentMonth && 
                           t.Date < currentMonth.AddMonths(1) &&
                           !t.IsIncome &&
                           t.TransactionCategories.Any(tc => tc.CategoryId == budgetCategory.CategoryId))
                .SumAsync(t => t.Amount);

            if (budgetCategory.PlannedAmount > 0)
            {
                var adherence = Math.Min(100, (1 - Math.Abs(spent - budgetCategory.PlannedAmount) / budgetCategory.PlannedAmount) * 100);
                adherenceScores.Add(adherence);
            }
        }

        var averageAdherence = adherenceScores.Any() ? adherenceScores.Average() : 0;
        
        // Score: 0-15 points
        var score = averageAdherence switch
        {
            >= 90 => 15,
            >= 80 => 12,
            >= 70 => 9,
            >= 60 => 6,
            >= 50 => 3,
            _ => 0
        };

        var status = averageAdherence switch
        {
            >= 85 => "Utmärkt",
            >= 70 => "Bra",
            >= 50 => "Godkänt",
            _ => "Låg"
        };

        return new HealthScoreComponent
        {
            Name = "Budgetföljning",
            Score = score,
            MaxScore = 15,
            Value = averageAdherence,
            Unit = "%",
            Status = status,
            Description = averageAdherence < 70 ? "Förbättra budgetdisciplin" : ""
        };
    }

    private async Task<HealthScoreComponent> CalculateInvestmentDiversificationScoreAsync(int? householdId)
    {
        var investments = await _context.Investments.ToListAsync();
        
        if (!investments.Any())
        {
            return new HealthScoreComponent
            {
                Name = "Investeringsdiversifiering",
                Score = 0,
                MaxScore = 15,
                Status = "Saknas",
                Description = "Överväg att diversifiera investeringar"
            };
        }

        // Calculate diversification based on number of different investments
        var totalValue = investments.Sum(i => i.TotalValue);
        var diversificationScore = 0m;

        if (totalValue > 0)
        {
            // Calculate Herfindahl index (lower is more diversified)
            var herfindahl = investments.Sum(i => Math.Pow((double)(i.TotalValue / totalValue), 2));
            
            // Convert to diversification score (0-100, higher is better)
            diversificationScore = (decimal)((1 - herfindahl) * 100);
        }

        // Score: 0-15 points
        var score = diversificationScore switch
        {
            >= 70 => 15,
            >= 50 => 12,
            >= 30 => 9,
            >= 20 => 6,
            >= 10 => 3,
            _ => 0
        };

        var status = diversificationScore switch
        {
            >= 60 => "Utmärkt",
            >= 40 => "Bra",
            >= 20 => "Godkänt",
            _ => "Låg"
        };

        return new HealthScoreComponent
        {
            Name = "Investeringsdiversifiering",
            Score = score,
            MaxScore = 15,
            Value = diversificationScore,
            Unit = "%",
            Status = status,
            Description = diversificationScore < 40 ? "Öka diversifieringen av investeringar" : ""
        };
    }

    private async Task<HealthScoreComponent> CalculateIncomeStabilityScoreAsync(int? householdId)
    {
        // Get last 6 months of income
        var sixMonthsAgo = DateTime.Today.AddMonths(-6);
        
        var query = _context.Transactions
            .Where(t => t.Date >= sixMonthsAgo && t.IsIncome);

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var incomeTransactions = await query.ToListAsync();
        
        if (!incomeTransactions.Any())
        {
            return new HealthScoreComponent
            {
                Name = "Inkomststabilitet",
                Score = 0,
                MaxScore = 10,
                Status = "Saknas",
                Description = "Ingen inkomstdata registrerad"
            };
        }

        // Group by month and calculate coefficient of variation
        var monthlyIncomes = incomeTransactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(t => t.Amount))
            .ToList();

        if (monthlyIncomes.Count < 2)
        {
            return new HealthScoreComponent
            {
                Name = "Inkomststabilitet",
                Score = 5,
                MaxScore = 10,
                Status = "Begränsad data",
                Description = "Behöver mer inkomsthistorik"
            };
        }

        var mean = monthlyIncomes.Average();
        var variance = monthlyIncomes.Sum(i => Math.Pow((double)(i - mean), 2)) / monthlyIncomes.Count;
        var stdDev = (decimal)Math.Sqrt(variance);
        var coefficientOfVariation = mean > 0 ? (stdDev / mean) * 100 : 100;
        
        // Score: 0-10 points (lower variation is better)
        var score = coefficientOfVariation switch
        {
            <= 10 => 10,
            <= 20 => 8,
            <= 30 => 6,
            <= 40 => 4,
            <= 50 => 2,
            _ => 0
        };

        var status = coefficientOfVariation switch
        {
            <= 15 => "Utmärkt",
            <= 30 => "Bra",
            <= 50 => "Godkänt",
            _ => "Variabel"
        };

        var stability = 100 - coefficientOfVariation;

        return new HealthScoreComponent
        {
            Name = "Inkomststabilitet",
            Score = score,
            MaxScore = 10,
            Value = stability,
            Unit = "%",
            Status = status,
            Description = coefficientOfVariation > 30 ? "Inkomsten varierar mycket mellan månader" : ""
        };
    }

    public async Task<SpendingPatternReport> GetSpendingPatternReportAsync(DateTime fromDate, DateTime toDate, int? householdId = null)
    {
        // Get all expense transactions for the period
        var query = _context.Transactions
            .Include(t => t.TransactionCategories)
                .ThenInclude(tc => tc.Category)
            .Where(t => !t.IsIncome && t.Date >= fromDate && t.Date <= toDate);

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var transactions = await query.OrderBy(t => t.Date).ToListAsync();
        var totalSpending = transactions.Sum(t => t.Amount);

        // Calculate number of months in period
        var monthsDiff = ((toDate.Year - fromDate.Year) * 12) + toDate.Month - fromDate.Month + 1;
        var averageMonthlySpending = monthsDiff > 0 ? totalSpending / monthsDiff : totalSpending;

        // Category distribution analysis
        var categoryDistribution = await CalculateCategoryDistribution(transactions, totalSpending, fromDate, toDate, householdId);
        
        // Top spending categories (top 5)
        var topCategories = categoryDistribution
            .OrderByDescending(c => c.Amount)
            .Take(5)
            .ToList();

        // Monthly data for visualization
        var monthlyData = CalculateMonthlyData(transactions);

        // Trend analysis
        var trends = await CalculateSpendingTrends(transactions, fromDate, toDate);

        // Anomaly detection
        var anomalies = DetectSpendingAnomalies(transactions, monthlyData);

        // Generate recommendations
        var recommendations = GenerateRecommendations(categoryDistribution, trends, anomalies, averageMonthlySpending);

        return new SpendingPatternReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalSpending = totalSpending,
            AverageMonthlySpending = averageMonthlySpending,
            CategoryDistribution = categoryDistribution,
            TopCategories = topCategories,
            Trends = trends,
            Anomalies = anomalies,
            Recommendations = recommendations,
            MonthlyData = monthlyData,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<List<CategorySpending>> CalculateCategoryDistribution(
        List<Transaction> transactions, 
        decimal totalSpending, 
        DateTime fromDate, 
        DateTime toDate,
        int? householdId)
    {
        // Calculate previous period for comparison
        var periodLength = (toDate - fromDate).Days;
        var previousFromDate = fromDate.AddDays(-periodLength);
        var previousToDate = fromDate.AddDays(-1);

        var previousQuery = _context.Transactions
            .Include(t => t.TransactionCategories)
                .ThenInclude(tc => tc.Category)
            .Where(t => !t.IsIncome && t.Date >= previousFromDate && t.Date <= previousToDate);

        if (householdId.HasValue)
        {
            previousQuery = previousQuery.Where(t => t.HouseholdId == householdId.Value);
        }

        var previousTransactions = await previousQuery.ToListAsync();

        // Group by category
        var categoryGroups = transactions
            .SelectMany(t => t.TransactionCategories.Select(tc => new { Transaction = t, Category = tc.Category }))
            .GroupBy(x => new { x.Category?.CategoryId, x.Category?.Name, x.Category?.Color })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId ?? 0,
                CategoryName = g.Key.Name ?? "Okategoriserad",
                CategoryColor = g.Key.Color ?? "#757575",
                Transactions = g.Select(x => x.Transaction).ToList()
            })
            .ToList();

        var result = new List<CategorySpending>();

        foreach (var group in categoryGroups)
        {
            var amount = group.Transactions.Sum(t => t.Amount);
            var transactionCount = group.Transactions.Count;
            
            // Calculate previous period amount for this category
            var previousAmount = previousTransactions
                .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == group.CategoryId))
                .Sum(t => t.Amount);

            var change = amount - previousAmount;
            var changePercentage = previousAmount > 0 ? (change / previousAmount) * 100 : 0;

            result.Add(new CategorySpending
            {
                CategoryId = group.CategoryId,
                CategoryName = group.CategoryName,
                CategoryColor = group.CategoryColor,
                Amount = amount,
                Percentage = totalSpending > 0 ? (amount / totalSpending) * 100 : 0,
                TransactionCount = transactionCount,
                AverageTransactionAmount = transactionCount > 0 ? amount / transactionCount : 0,
                PreviousPeriodAmount = previousAmount,
                ChangeFromPreviousPeriod = change,
                ChangePercentage = changePercentage
            });
        }

        // Handle uncategorized transactions
        var uncategorizedTransactions = transactions.Where(t => !t.TransactionCategories.Any()).ToList();
        if (uncategorizedTransactions.Any())
        {
            var amount = uncategorizedTransactions.Sum(t => t.Amount);
            var transactionCount = uncategorizedTransactions.Count;
            
            var previousUncategorized = previousTransactions.Where(t => !t.TransactionCategories.Any()).Sum(t => t.Amount);
            var change = amount - previousUncategorized;
            var changePercentage = previousUncategorized > 0 ? (change / previousUncategorized) * 100 : 0;

            result.Add(new CategorySpending
            {
                CategoryId = 0,
                CategoryName = "Okategoriserad",
                CategoryColor = "#757575",
                Amount = amount,
                Percentage = totalSpending > 0 ? (amount / totalSpending) * 100 : 0,
                TransactionCount = transactionCount,
                AverageTransactionAmount = transactionCount > 0 ? amount / transactionCount : 0,
                PreviousPeriodAmount = previousUncategorized,
                ChangeFromPreviousPeriod = change,
                ChangePercentage = changePercentage
            });
        }

        return result.OrderByDescending(c => c.Amount).ToList();
    }

    private List<MonthlySpendingData> CalculateMonthlyData(List<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var categoryBreakdown = g
                    .SelectMany(t => t.TransactionCategories.Select(tc => new { t.Amount, CategoryName = tc.Category?.Name ?? "Okategoriserad" }))
                    .GroupBy(x => x.CategoryName)
                    .ToDictionary(cg => cg.Key, cg => cg.Sum(x => x.Amount));

                return new MonthlySpendingData
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    TotalAmount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    CategoryBreakdown = categoryBreakdown
                };
            })
            .ToList();
    }

    private Task<List<SpendingTrend>> CalculateSpendingTrends(List<Transaction> transactions, DateTime fromDate, DateTime toDate)
    {
        var trends = new List<SpendingTrend>();

        // Overall spending trend
        var monthlyData = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                Amount = g.Sum(t => t.Amount)
            })
            .ToList();

        if (monthlyData.Count >= 3)
        {
            var firstHalfAvg = monthlyData.Take(monthlyData.Count / 2).Average(m => m.Amount);
            var secondHalfAvg = monthlyData.Skip(monthlyData.Count / 2).Average(m => m.Amount);
            var trendPercentage = firstHalfAvg > 0 ? ((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100 : 0;

            var trendType = Math.Abs(trendPercentage) < 5 ? "Stable" : trendPercentage > 0 ? "Increasing" : "Decreasing";
            var description = trendType == "Stable" 
                ? "Dina totala utgifter är relativt stabila över perioden."
                : trendType == "Increasing"
                ? $"Dina totala utgifter ökar med i genomsnitt {Math.Abs(trendPercentage):F1}% mellan perioderna."
                : $"Dina totala utgifter minskar med i genomsnitt {Math.Abs(trendPercentage):F1}% mellan perioderna.";

            trends.Add(new SpendingTrend
            {
                CategoryId = null,
                CategoryName = "Total utgifter",
                TrendType = trendType,
                TrendPercentage = trendPercentage,
                Description = description,
                DataPoints = monthlyData.Select(m => new TrendDataPoint
                {
                    Date = m.Date,
                    Period = m.Period,
                    Amount = m.Amount
                }).ToList()
            });
        }

        // Category-level trends
        var categoriesWithTrends = transactions
            .SelectMany(t => t.TransactionCategories.Select(tc => new { Transaction = t, Category = tc.Category }))
            .GroupBy(x => new { x.Category?.CategoryId, x.Category?.Name })
            .Where(g => g.Key.CategoryId.HasValue)
            .ToList();

        foreach (var categoryGroup in categoriesWithTrends)
        {
            var categoryMonthlyData = categoryGroup
                .GroupBy(x => new { x.Transaction.Date.Year, x.Transaction.Date.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Amount = g.Sum(x => x.Transaction.Amount)
                })
                .ToList();

            if (categoryMonthlyData.Count >= 3)
            {
                var firstHalfAvg = categoryMonthlyData.Take(categoryMonthlyData.Count / 2).Average(m => m.Amount);
                var secondHalfAvg = categoryMonthlyData.Skip(categoryMonthlyData.Count / 2).Average(m => m.Amount);
                var trendPercentage = firstHalfAvg > 0 ? ((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100 : 0;

                // Only include significant trends (> 10% change)
                if (Math.Abs(trendPercentage) > 10)
                {
                    var trendType = trendPercentage > 0 ? "Increasing" : "Decreasing";
                    var description = trendType == "Increasing"
                        ? $"Utgifter i kategorin {categoryGroup.Key.Name} ökar med {Math.Abs(trendPercentage):F1}%."
                        : $"Utgifter i kategorin {categoryGroup.Key.Name} minskar med {Math.Abs(trendPercentage):F1}%.";

                    trends.Add(new SpendingTrend
                    {
                        CategoryId = categoryGroup.Key.CategoryId,
                        CategoryName = categoryGroup.Key.Name ?? "Okategoriserad",
                        TrendType = trendType,
                        TrendPercentage = trendPercentage,
                        Description = description,
                        DataPoints = categoryMonthlyData.Select(m => new TrendDataPoint
                        {
                            Date = m.Date,
                            Period = m.Period,
                            Amount = m.Amount
                        }).ToList()
                    });
                }
            }
        }

        return Task.FromResult(trends);
    }

    private List<SpendingAnomaly> DetectSpendingAnomalies(List<Transaction> transactions, List<MonthlySpendingData> monthlyData)
    {
        var anomalies = new List<SpendingAnomaly>();

        if (monthlyData.Count < 3)
            return anomalies;

        // Calculate average and standard deviation for overall spending
        var amounts = monthlyData.Select(m => (double)m.TotalAmount).ToArray();
        var average = amounts.Average();
        var stdDev = Math.Sqrt(amounts.Average(v => Math.Pow(v - average, 2)));

        // Detect months with unusually high or low spending (> 2 standard deviations)
        foreach (var month in monthlyData)
        {
            var deviation = ((double)month.TotalAmount - average) / (stdDev > 0 ? stdDev : 1);
            
            if (Math.Abs(deviation) > 2)
            {
                var type = deviation > 0 ? "UnusuallyHigh" : "UnusuallyLow";
                var description = deviation > 0
                    ? $"Ovanligt höga utgifter i {month.Month}: {month.TotalAmount:C0} (förväntat: {average:C0})"
                    : $"Ovanligt låga utgifter i {month.Month}: {month.TotalAmount:C0} (förväntat: {average:C0})";

                anomalies.Add(new SpendingAnomaly
                {
                    Date = month.Date,
                    Type = type,
                    CategoryName = "Alla kategorier",
                    Amount = month.TotalAmount,
                    ExpectedAmount = (decimal)average,
                    Deviation = (decimal)(deviation * stdDev),
                    Description = description
                });
            }
        }

        return anomalies.OrderByDescending(a => Math.Abs(a.Deviation)).Take(5).ToList();
    }

    private List<SpendingRecommendation> GenerateRecommendations(
        List<CategorySpending> categoryDistribution,
        List<SpendingTrend> trends,
        List<SpendingAnomaly> anomalies,
        decimal averageMonthlySpending)
    {
        var recommendations = new List<SpendingRecommendation>();

        // Recommendation: High percentage categories
        var highPercentageCategories = categoryDistribution
            .Where(c => c.Percentage > 20 && c.CategoryName != "Okategoriserad")
            .OrderByDescending(c => c.Percentage)
            .Take(2);

        foreach (var category in highPercentageCategories)
        {
            recommendations.Add(new SpendingRecommendation
            {
                Type = "BudgetAlert",
                Priority = "Medium",
                Title = $"Hög andel utgifter i {category.CategoryName}",
                Description = $"{category.CategoryName} utgör {category.Percentage:F1}% av dina totala utgifter. Överväg om det finns möjligheter att minska kostnaderna i denna kategori.",
                CategoryName = category.CategoryName
            });
        }

        // Recommendation: Increasing trends
        var increasingTrends = trends
            .Where(t => t.TrendType == "Increasing" && t.TrendPercentage > 15)
            .OrderByDescending(t => t.TrendPercentage)
            .Take(2);

        foreach (var trend in increasingTrends)
        {
            var potentialSavings = categoryDistribution
                .FirstOrDefault(c => c.CategoryId == trend.CategoryId)?.Amount * 0.1m; // Assume 10% reduction potential

            recommendations.Add(new SpendingRecommendation
            {
                Type = "TrendWarning",
                Priority = "High",
                Title = $"Stigande trend i {trend.CategoryName}",
                Description = $"Utgifter i {trend.CategoryName} ökar med {trend.TrendPercentage:F1}%. Detta kan påverka din ekonomiska planering.",
                PotentialSavings = potentialSavings,
                CategoryName = trend.CategoryName
            });
        }

        // Recommendation: Uncategorized transactions
        var uncategorized = categoryDistribution.FirstOrDefault(c => c.CategoryName == "Okategoriserad");
        if (uncategorized != null && uncategorized.Percentage > 10)
        {
            recommendations.Add(new SpendingRecommendation
            {
                Type = "SavingsOpportunity",
                Priority = "Low",
                Title = "Många okategoriserade transaktioner",
                Description = $"{uncategorized.Percentage:F1}% av dina utgifter är okategoriserade. Kategorisera dem för bättre överblick och kontroll.",
                CategoryName = "Okategoriserad"
            });
        }

        // Recommendation: Anomalies
        if (anomalies.Any(a => a.Type == "UnusuallyHigh"))
        {
            var highestAnomaly = anomalies.Where(a => a.Type == "UnusuallyHigh").OrderByDescending(a => a.Deviation).First();
            recommendations.Add(new SpendingRecommendation
            {
                Type = "TrendWarning",
                Priority = "Medium",
                Title = "Ovanligt hög utgift upptäckt",
                Description = highestAnomaly.Description,
                CategoryName = highestAnomaly.CategoryName
            });
        }

        // Recommendation: Decreasing trends (positive feedback)
        var decreasingTrends = trends
            .Where(t => t.TrendType == "Decreasing" && t.TrendPercentage < -10)
            .OrderBy(t => t.TrendPercentage)
            .Take(1);

        foreach (var trend in decreasingTrends)
        {
            var actualSavings = categoryDistribution
                .FirstOrDefault(c => c.CategoryId == trend.CategoryId)?.ChangeFromPreviousPeriod;

            recommendations.Add(new SpendingRecommendation
            {
                Type = "SavingsOpportunity",
                Priority = "Low",
                Title = $"Bra jobbat! Minskade utgifter i {trend.CategoryName}",
                Description = $"Du har minskat utgifter i {trend.CategoryName} med {Math.Abs(trend.TrendPercentage):F1}%. Fortsätt så!",
                PotentialSavings = actualSavings.HasValue ? Math.Abs(actualSavings.Value) : null,
                CategoryName = trend.CategoryName
            });
        }

        return recommendations.OrderByDescending(r => r.Priority == "High" ? 3 : r.Priority == "Medium" ? 2 : 1).ToList();
    }

    private static readonly string[] SwedishMonthNames = 
    {
        "januari", "februari", "mars", "april", "maj", "juni",
        "juli", "augusti", "september", "oktober", "november", "december"
    };

    public async Task<MonthlyReportData> GenerateMonthlyReportAsync(int year, int month, string? userId = null, int? householdId = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthEnd = monthStart.AddDays(-1);

        // Get transactions for current month
        var query = _context.Transactions
            .Include(t => t.TransactionCategories)
                .ThenInclude(tc => tc.Category)
            .Where(t => t.Date >= monthStart && t.Date <= monthEnd);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        if (householdId.HasValue)
        {
            query = query.Where(t => t.HouseholdId == householdId.Value);
        }

        var transactions = await query.ToListAsync();

        // Get transactions for previous month for comparison
        var previousQuery = _context.Transactions
            .Include(t => t.TransactionCategories)
                .ThenInclude(tc => tc.Category)
            .Where(t => t.Date >= previousMonthStart && t.Date <= previousMonthEnd);

        if (!string.IsNullOrEmpty(userId))
        {
            previousQuery = previousQuery.Where(t => t.UserId == userId);
        }

        if (householdId.HasValue)
        {
            previousQuery = previousQuery.Where(t => t.HouseholdId == householdId.Value);
        }

        var previousTransactions = await previousQuery.ToListAsync();

        // Calculate totals
        var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var totalExpenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var netFlow = totalIncome - totalExpenses;
        var savingsRate = totalIncome > 0 ? (netFlow / totalIncome) * 100 : 0;

        var previousIncome = previousTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var previousExpenses = previousTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var previousNetFlow = previousIncome - previousExpenses;

        // Category summaries
        var categorySummaries = await CalculateCategorySummariesAsync(transactions, totalExpenses, previousTransactions);

        // Top merchants
        var topMerchants = (await GetTopMerchantsAsync(5, monthStart, monthEnd, householdId)).ToList();

        // Budget outcomes
        var budgetOutcomes = await CalculateBudgetOutcomesAsync(monthStart, monthEnd, userId, householdId);

        // Generate insights
        var insights = GenerateMonthlyInsights(totalIncome, totalExpenses, netFlow, savingsRate, 
            previousIncome, previousExpenses, categorySummaries, budgetOutcomes);

        // Build comparison data
        var comparison = new MonthlyComparison
        {
            IncomeChange = totalIncome - previousIncome,
            IncomeChangePercent = previousIncome > 0 ? ((totalIncome - previousIncome) / previousIncome) * 100 : 0,
            ExpenseChange = totalExpenses - previousExpenses,
            ExpenseChangePercent = previousExpenses > 0 ? ((totalExpenses - previousExpenses) / previousExpenses) * 100 : 0,
            NetFlowChange = netFlow - previousNetFlow,
            NetFlowChangePercent = previousNetFlow != 0 ? ((netFlow - previousNetFlow) / Math.Abs(previousNetFlow)) * 100 : 0,
            TrendDirection = DetermineTrendDirection(netFlow, previousNetFlow)
        };

        return new MonthlyReportData
        {
            Year = year,
            Month = month,
            MonthName = SwedishMonthNames[month - 1],
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetFlow = netFlow,
            SavingsRate = savingsRate,
            PreviousMonthComparison = comparison,
            CategorySummaries = categorySummaries,
            TopMerchants = topMerchants,
            BudgetOutcomes = budgetOutcomes,
            Insights = insights,
            TransactionCount = transactions.Count,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<List<ReportCategorySummary>> CalculateCategorySummariesAsync(
        List<Transaction> transactions, 
        decimal totalExpenses,
        List<Transaction> previousTransactions)
    {
        var expenseTransactions = transactions.Where(t => !t.IsIncome).ToList();
        var previousExpenseTransactions = previousTransactions.Where(t => !t.IsIncome).ToList();

        // Group by category
        var categoryGroups = expenseTransactions
            .SelectMany(t => t.TransactionCategories.Select(tc => new { Transaction = t, Category = tc.Category }))
            .GroupBy(x => new { x.Category?.CategoryId, x.Category?.Name, x.Category?.Color })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId ?? 0,
                CategoryName = g.Key.Name ?? "Okategoriserad",
                CategoryColor = g.Key.Color ?? "#757575",
                Transactions = g.Select(x => x.Transaction).Distinct().ToList()
            })
            .ToList();

        var result = new List<ReportCategorySummary>();

        foreach (var group in categoryGroups)
        {
            var amount = group.Transactions.Sum(t => t.Amount);
            var transactionCount = group.Transactions.Count;

            // Calculate previous month amount for this category
            var previousAmount = previousExpenseTransactions
                .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == group.CategoryId))
                .Sum(t => t.Amount);

            var changePercent = previousAmount > 0 ? ((amount - previousAmount) / previousAmount) * 100 : (decimal?)null;

            result.Add(new ReportCategorySummary
            {
                CategoryId = group.CategoryId,
                CategoryName = group.CategoryName,
                CategoryColor = group.CategoryColor,
                Amount = amount,
                Percentage = totalExpenses > 0 ? (amount / totalExpenses) * 100 : 0,
                TransactionCount = transactionCount,
                PreviousMonthAmount = previousAmount > 0 ? previousAmount : null,
                ChangePercent = changePercent
            });
        }

        // Handle uncategorized transactions
        var uncategorizedTransactions = expenseTransactions.Where(t => !t.TransactionCategories.Any()).ToList();
        if (uncategorizedTransactions.Any())
        {
            var amount = uncategorizedTransactions.Sum(t => t.Amount);
            var previousUncategorized = previousExpenseTransactions.Where(t => !t.TransactionCategories.Any()).Sum(t => t.Amount);
            var changePercent = previousUncategorized > 0 ? ((amount - previousUncategorized) / previousUncategorized) * 100 : (decimal?)null;

            result.Add(new ReportCategorySummary
            {
                CategoryId = 0,
                CategoryName = "Okategoriserad",
                CategoryColor = "#757575",
                Amount = amount,
                Percentage = totalExpenses > 0 ? (amount / totalExpenses) * 100 : 0,
                TransactionCount = uncategorizedTransactions.Count,
                PreviousMonthAmount = previousUncategorized > 0 ? previousUncategorized : null,
                ChangePercent = changePercent
            });
        }

        return result.OrderByDescending(c => c.Amount).ToList();
    }

    private async Task<List<BudgetOutcome>> CalculateBudgetOutcomesAsync(DateTime monthStart, DateTime monthEnd, string? userId, int? householdId)
    {
        var outcomes = new List<BudgetOutcome>();

        // Get active budget for the month
        var budget = await _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .Where(b => b.StartDate <= monthEnd && b.EndDate >= monthStart)
            .Where(b => householdId.HasValue ? b.HouseholdId == householdId : string.IsNullOrEmpty(userId) || b.UserId == userId)
            .FirstOrDefaultAsync();

        if (budget == null || !budget.BudgetCategories.Any())
        {
            return outcomes;
        }

        foreach (var budgetCategory in budget.BudgetCategories)
        {
            var actualAmount = await _context.Transactions
                .Where(t => t.Date >= monthStart && t.Date <= monthEnd && !t.IsIncome)
                .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == budgetCategory.CategoryId))
                .SumAsync(t => t.Amount);

            var budgetedAmount = budgetCategory.PlannedAmount;
            var difference = budgetedAmount - actualAmount;
            var percentageUsed = budgetedAmount > 0 ? (actualAmount / budgetedAmount) * 100 : 0;

            var status = percentageUsed switch
            {
                <= 90 => "UnderBudget",
                <= 105 => "OnTrack",
                _ => "OverBudget"
            };

            outcomes.Add(new BudgetOutcome
            {
                CategoryId = budgetCategory.CategoryId,
                CategoryName = budgetCategory.Category?.Name ?? "Okänd",
                BudgetedAmount = budgetedAmount,
                ActualAmount = actualAmount,
                Difference = difference,
                PercentageUsed = percentageUsed,
                Status = status
            });
        }

        return outcomes.OrderByDescending(o => o.ActualAmount).ToList();
    }

    private List<ReportInsight> GenerateMonthlyInsights(
        decimal totalIncome, 
        decimal totalExpenses, 
        decimal netFlow, 
        decimal savingsRate,
        decimal previousIncome,
        decimal previousExpenses,
        List<ReportCategorySummary> categorySummaries,
        List<BudgetOutcome> budgetOutcomes)
    {
        var insights = new List<ReportInsight>();

        // Savings rate insight
        if (savingsRate >= 20)
        {
            insights.Add(new ReportInsight
            {
                Type = "Positive",
                Title = "Utmärkt sparprocent!",
                Description = $"Du sparade {savingsRate:F1}% av din inkomst denna månad, vilket är över rekommenderade 20%.",
                Amount = netFlow
            });
        }
        else if (savingsRate >= 10)
        {
            insights.Add(new ReportInsight
            {
                Type = "Info",
                Title = "Bra sparande",
                Description = $"Du sparade {savingsRate:F1}% av din inkomst. Sikta på 20% för optimal ekonomisk hälsa.",
                Amount = netFlow
            });
        }
        else if (savingsRate >= 0)
        {
            insights.Add(new ReportInsight
            {
                Type = "Warning",
                Title = "Lågt sparande",
                Description = $"Din sparprocent är {savingsRate:F1}%. Försök hitta områden där du kan minska utgifterna.",
                Amount = netFlow
            });
        }
        else
        {
            insights.Add(new ReportInsight
            {
                Type = "Warning",
                Title = "Negativt netto",
                Description = $"Du spenderade {Math.Abs(netFlow):C0} mer än du tjänade denna månad.",
                Amount = netFlow
            });
        }

        // Expense trend insight
        if (previousExpenses > 0)
        {
            var expenseChange = ((totalExpenses - previousExpenses) / previousExpenses) * 100;
            if (expenseChange <= -10)
            {
                insights.Add(new ReportInsight
                {
                    Type = "Positive",
                    Title = "Minskade utgifter",
                    Description = $"Dina utgifter minskade med {Math.Abs(expenseChange):F1}% jämfört med förra månaden. Bra jobbat!",
                    Amount = totalExpenses - previousExpenses
                });
            }
            else if (expenseChange >= 20)
            {
                insights.Add(new ReportInsight
                {
                    Type = "Warning",
                    Title = "Ökade utgifter",
                    Description = $"Dina utgifter ökade med {expenseChange:F1}% jämfört med förra månaden.",
                    Amount = totalExpenses - previousExpenses
                });
            }
        }

        // Top category insight
        var topCategory = categorySummaries.FirstOrDefault();
        if (topCategory != null && topCategory.Percentage > 30)
        {
            insights.Add(new ReportInsight
            {
                Type = "Info",
                Title = $"Största utgiftskategori: {topCategory.CategoryName}",
                Description = $"{topCategory.CategoryName} stod för {topCategory.Percentage:F1}% av dina totala utgifter.",
                CategoryName = topCategory.CategoryName,
                Amount = topCategory.Amount
            });
        }

        // Categories with significant increase
        var increasingCategories = categorySummaries
            .Where(c => c.ChangePercent.HasValue && c.ChangePercent > 25 && c.Amount > 500)
            .OrderByDescending(c => c.ChangePercent)
            .Take(2);

        foreach (var category in increasingCategories)
        {
            insights.Add(new ReportInsight
            {
                Type = "Warning",
                Title = $"Ökad kostnad: {category.CategoryName}",
                Description = $"Utgifter för {category.CategoryName} ökade med {category.ChangePercent:F1}% jämfört med förra månaden.",
                CategoryName = category.CategoryName,
                Amount = category.Amount
            });
        }

        // Budget performance insight
        var overBudgetCategories = budgetOutcomes.Where(b => b.Status == "OverBudget").ToList();
        if (overBudgetCategories.Count > 0)
        {
            var worstCategory = overBudgetCategories.OrderByDescending(b => b.PercentageUsed).First();
            insights.Add(new ReportInsight
            {
                Type = "Warning",
                Title = $"Budgetöverskridande: {worstCategory.CategoryName}",
                Description = $"Du överskred budgeten för {worstCategory.CategoryName} med {Math.Abs(worstCategory.Difference):C0} ({worstCategory.PercentageUsed:F0}% av budget).",
                CategoryName = worstCategory.CategoryName,
                Amount = worstCategory.Difference
            });
        }

        var underBudgetCount = budgetOutcomes.Count(b => b.Status == "UnderBudget");
        if (underBudgetCount > 0 && budgetOutcomes.Count > 0)
        {
            var underBudgetPercent = (underBudgetCount * 100.0m) / budgetOutcomes.Count;
            if (underBudgetPercent >= 70)
            {
                insights.Add(new ReportInsight
                {
                    Type = "Positive",
                    Title = "Bra budgetföljning",
                    Description = $"Du höll dig inom budget för {underBudgetCount} av {budgetOutcomes.Count} kategorier."
                });
            }
        }

        return insights.Take(6).ToList();
    }

    private static string DetermineTrendDirection(decimal current, decimal previous)
    {
        if (previous == 0)
            return "Stable";
        
        var changePercent = ((current - previous) / Math.Abs(previous)) * 100;
        
        return changePercent switch
        {
            > 5 => "Improving",
            < -5 => "Worsening",
            _ => "Stable"
        };
    }

    public async Task<MonthlyReport?> GetMonthlyReportAsync(int year, int month, string? userId = null, int? householdId = null)
    {
        var reportMonth = new DateTime(year, month, 1);
        
        var query = _context.MonthlyReports
            .Include(r => r.Deliveries)
            .Where(r => r.ReportMonth == reportMonth);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(r => r.UserId == userId);
        }

        if (householdId.HasValue)
        {
            query = query.Where(r => r.HouseholdId == householdId);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MonthlyReport>> GetMonthlyReportsAsync(string? userId = null, int? householdId = null, int limit = 12)
    {
        var query = _context.MonthlyReports
            .Include(r => r.Deliveries)
            .OrderByDescending(r => r.ReportMonth)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(r => r.UserId == userId);
        }

        if (householdId.HasValue)
        {
            query = query.Where(r => r.HouseholdId == householdId);
        }

        return await query.Take(limit).ToListAsync();
    }

    public async Task<MonthlyReport> SaveMonthlyReportAsync(MonthlyReportData reportData, string? userId = null, int? householdId = null)
    {
        var reportMonth = new DateTime(reportData.Year, reportData.Month, 1);
        
        // Check if report already exists
        var existingReport = await GetMonthlyReportAsync(reportData.Year, reportData.Month, userId, householdId);
        
        if (existingReport != null)
        {
            // Update existing report
            existingReport.TotalIncome = reportData.TotalIncome;
            existingReport.TotalExpenses = reportData.TotalExpenses;
            existingReport.NetFlow = reportData.NetFlow;
            existingReport.IncomeChangePercent = reportData.PreviousMonthComparison?.IncomeChangePercent ?? 0;
            existingReport.ExpenseChangePercent = reportData.PreviousMonthComparison?.ExpenseChangePercent ?? 0;
            existingReport.CategorySummariesJson = System.Text.Json.JsonSerializer.Serialize(reportData.CategorySummaries);
            existingReport.TopMerchantsJson = System.Text.Json.JsonSerializer.Serialize(reportData.TopMerchants);
            existingReport.BudgetOutcomeJson = System.Text.Json.JsonSerializer.Serialize(reportData.BudgetOutcomes);
            existingReport.InsightsJson = System.Text.Json.JsonSerializer.Serialize(reportData.Insights);
            existingReport.GeneratedAt = reportData.GeneratedAt;
            existingReport.Status = ReportStatus.Generated;
            existingReport.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingReport;
        }

        // Create new report
        var report = new MonthlyReport
        {
            ReportMonth = reportMonth,
            TotalIncome = reportData.TotalIncome,
            TotalExpenses = reportData.TotalExpenses,
            NetFlow = reportData.NetFlow,
            IncomeChangePercent = reportData.PreviousMonthComparison?.IncomeChangePercent ?? 0,
            ExpenseChangePercent = reportData.PreviousMonthComparison?.ExpenseChangePercent ?? 0,
            CategorySummariesJson = System.Text.Json.JsonSerializer.Serialize(reportData.CategorySummaries),
            TopMerchantsJson = System.Text.Json.JsonSerializer.Serialize(reportData.TopMerchants),
            BudgetOutcomeJson = System.Text.Json.JsonSerializer.Serialize(reportData.BudgetOutcomes),
            InsightsJson = System.Text.Json.JsonSerializer.Serialize(reportData.Insights),
            GeneratedAt = reportData.GeneratedAt,
            Status = ReportStatus.Generated,
            UserId = userId,
            HouseholdId = householdId,
            ValidFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.MonthlyReports.Add(report);
        await _context.SaveChangesAsync();

        return report;
    }

    public async Task<ReportPreference> GetReportPreferencesAsync(string userId)
    {
        var preferences = await _context.ReportPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Return default preferences
            preferences = new ReportPreference
            {
                UserId = userId,
                SendEmail = false,
                ShowInApp = true,
                PreferredDeliveryDay = 1,
                IncludeCategoryDetails = true,
                IncludeTopMerchants = true,
                IncludeBudgetComparison = true,
                IncludeTrendAnalysis = true,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        return preferences;
    }

    public async Task<ReportPreference> SaveReportPreferencesAsync(ReportPreference preferences)
    {
        var existingPreferences = await _context.ReportPreferences
            .FirstOrDefaultAsync(p => p.UserId == preferences.UserId);

        if (existingPreferences != null)
        {
            existingPreferences.SendEmail = preferences.SendEmail;
            existingPreferences.ShowInApp = preferences.ShowInApp;
            existingPreferences.EmailAddress = preferences.EmailAddress;
            existingPreferences.PreferredDeliveryDay = preferences.PreferredDeliveryDay;
            existingPreferences.IncludeCategoryDetails = preferences.IncludeCategoryDetails;
            existingPreferences.IncludeTopMerchants = preferences.IncludeTopMerchants;
            existingPreferences.IncludeBudgetComparison = preferences.IncludeBudgetComparison;
            existingPreferences.IncludeTrendAnalysis = preferences.IncludeTrendAnalysis;
            existingPreferences.IsEnabled = preferences.IsEnabled;
            existingPreferences.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingPreferences;
        }

        preferences.CreatedAt = DateTime.UtcNow;
        _context.ReportPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        return preferences;
    }

    public async Task<HistoricalOverviewReport> GetHistoricalOverviewAsync(DateTime asOfDate, string? userId = null)
    {
        var report = new HistoricalOverviewReport
        {
            AsOfDate = asOfDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get historical accounts (BankSources) with balances calculated as of the date
        var bankSourcesQuery = _context.BankSources.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
        {
            bankSourcesQuery = bankSourcesQuery.Where(b => b.UserId == userId);
        }
        // Only include accounts that existed as of the date
        bankSourcesQuery = bankSourcesQuery.Where(b => b.CreatedAt <= asOfDate && (b.ClosedDate == null || b.ClosedDate > asOfDate));
        var bankSources = await bankSourcesQuery.Include(b => b.Transactions).ToListAsync();

        foreach (var account in bankSources)
        {
            // Calculate balance as of the date by summing transactions up to that date
            var transactionsUpToDate = account.Transactions
                .Where(t => t.Date <= asOfDate)
                .ToList();
            var balance = account.InitialBalance + transactionsUpToDate.Sum(t => t.IsIncome ? t.Amount : -t.Amount);

            report.Accounts.Add(new HistoricalAccountItem
            {
                AccountId = account.BankSourceId,
                Name = account.Name,
                AccountType = account.AccountType,
                Balance = balance,
                Currency = account.Currency,
                Color = account.Color
            });
        }
        report.TotalBankBalance = report.Accounts.Sum(a => a.Balance);

        // Get investments as of the date using temporal tracking
        var investmentsQuery = _context.Investments
            .Where(i => i.ValidFrom <= asOfDate && (i.ValidTo == null || i.ValidTo > asOfDate));
        if (!string.IsNullOrEmpty(userId))
        {
            investmentsQuery = investmentsQuery.Where(i => i.UserId == userId);
        }
        var investments = await investmentsQuery.ToListAsync();

        foreach (var investment in investments)
        {
            // For historical prices, use the CurrentPrice recorded at that time
            // In a more sophisticated implementation, you might have a price history table
            var totalValue = investment.Quantity * investment.CurrentPrice;

            report.Investments.Add(new HistoricalInvestmentItem
            {
                InvestmentId = investment.InvestmentId,
                Name = investment.Name,
                Type = investment.Type,
                Quantity = investment.Quantity,
                PriceAtDate = investment.CurrentPrice,
                TotalValue = totalValue,
                Currency = investment.Currency
            });
        }
        report.TotalInvestments = report.Investments.Sum(i => i.TotalValue);

        // Get assets as of the date using temporal tracking
        var assetsQuery = _context.Assets
            .Where(a => a.ValidFrom <= asOfDate && (a.ValidTo == null || a.ValidTo > asOfDate));
        if (!string.IsNullOrEmpty(userId))
        {
            assetsQuery = assetsQuery.Where(a => a.UserId == userId);
        }
        var assets = await assetsQuery.ToListAsync();

        foreach (var asset in assets)
        {
            report.Assets.Add(new HistoricalAssetItem
            {
                AssetId = asset.AssetId,
                Name = asset.Name,
                Type = asset.Type,
                Value = asset.CurrentValue,
                Currency = asset.Currency
            });
        }
        report.TotalPhysicalAssets = report.Assets.Sum(a => a.Value);

        // Get loans as of the date using temporal tracking
        var loansQuery = _context.Loans
            .Where(l => l.ValidFrom <= asOfDate && (l.ValidTo == null || l.ValidTo > asOfDate));
        if (!string.IsNullOrEmpty(userId))
        {
            loansQuery = loansQuery.Where(l => l.UserId == userId);
        }
        var loans = await loansQuery.ToListAsync();

        foreach (var loan in loans)
        {
            report.Loans.Add(new HistoricalLoanItem
            {
                LoanId = loan.LoanId,
                Name = loan.Name,
                Type = loan.Type,
                Balance = loan.Amount,
                InterestRate = loan.InterestRate,
                Currency = loan.Currency
            });
        }
        report.TotalLoans = report.Loans.Sum(l => l.Balance);

        // Calculate totals
        report.TotalAssets = report.TotalBankBalance + report.TotalInvestments + report.TotalPhysicalAssets;
        report.TotalLiabilities = report.TotalLoans;
        report.NetWorth = report.TotalAssets - report.TotalLiabilities;

        // Get monthly income/expenses for the month containing asOfDate
        var monthStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var monthTransactionsQuery = _context.Transactions
            .Where(t => t.Date >= monthStart && t.Date <= monthEnd);
        if (!string.IsNullOrEmpty(userId))
        {
            monthTransactionsQuery = monthTransactionsQuery.Where(t => t.UserId == userId);
        }
        var monthTransactions = await monthTransactionsQuery.ToListAsync();

        report.MonthlyIncome = monthTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        report.MonthlyExpenses = monthTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        report.TransactionCount = monthTransactions.Count;

        // Calculate comparison with current values
        var currentReport = await GetNetWorthReportAsync(userId);
        report.Comparison = new HistoricalComparison
        {
            NetWorthChange = currentReport.NetWorth - report.NetWorth,
            NetWorthChangePercent = report.NetWorth != 0 
                ? ((currentReport.NetWorth - report.NetWorth) / Math.Abs(report.NetWorth)) * 100 
                : 0,
            TotalAssetsChange = currentReport.TotalAssets - report.TotalAssets,
            TotalLiabilitiesChange = currentReport.TotalLiabilities - report.TotalLiabilities,
            DaysElapsed = (int)(DateTime.Today - asOfDate.Date).TotalDays
        };

        return report;
    }

    public async Task<List<TimelineKeyDate>> GetTimelineKeyDatesAsync(string? userId = null, int limit = 12)
    {
        var keyDates = new List<TimelineKeyDate>();

        // Get net worth snapshots for key dates
        var snapshotsQuery = _context.NetWorthSnapshots.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
        {
            snapshotsQuery = snapshotsQuery.Where(s => s.UserId == userId);
        }
        var snapshots = await snapshotsQuery
            .OrderByDescending(s => s.Date)
            .Take(limit * 2) // Get more to find peaks/valleys
            .ToListAsync();

        if (snapshots.Any())
        {
            // Add month-end snapshots
            var monthlySnapshots = snapshots
                .GroupBy(s => new { s.Date.Year, s.Date.Month })
                .Select(g => g.OrderByDescending(s => s.Date).First())
                .OrderByDescending(s => s.Date)
                .Take(limit);

            foreach (var snapshot in monthlySnapshots)
            {
                var monthName = new System.Globalization.CultureInfo("sv-SE")
                    .DateTimeFormat.GetMonthName(snapshot.Date.Month);
                keyDates.Add(new TimelineKeyDate
                {
                    Date = snapshot.Date,
                    Description = $"Slutet av {monthName} {snapshot.Date.Year}",
                    EventType = "MonthEnd",
                    NetWorth = snapshot.NetWorth
                });
            }

            // Find peaks and valleys
            if (snapshots.Count >= 3)
            {
                var maxSnapshot = snapshots.OrderByDescending(s => s.NetWorth).First();
                var minSnapshot = snapshots.OrderBy(s => s.NetWorth).First();

                if (!keyDates.Any(k => k.Date.Date == maxSnapshot.Date.Date))
                {
                    keyDates.Add(new TimelineKeyDate
                    {
                        Date = maxSnapshot.Date,
                        Description = "Högsta nettoförmögenhet",
                        EventType = "NetWorthPeak",
                        NetWorth = maxSnapshot.NetWorth
                    });
                }

                if (!keyDates.Any(k => k.Date.Date == minSnapshot.Date.Date))
                {
                    keyDates.Add(new TimelineKeyDate
                    {
                        Date = minSnapshot.Date,
                        Description = "Lägsta nettoförmögenhet",
                        EventType = "NetWorthLow",
                        NetWorth = minSnapshot.NetWorth
                    });
                }
            }
        }
        else
        {
            // If no snapshots, generate dates based on transaction history
            var transactionsQuery = _context.Transactions.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
            {
                transactionsQuery = transactionsQuery.Where(t => t.UserId == userId);
            }

            var monthlyGroups = await transactionsQuery
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)
                .Take(limit)
                .ToListAsync();

            foreach (var group in monthlyGroups)
            {
                var date = new DateTime(group.Year, group.Month, DateTime.DaysInMonth(group.Year, group.Month));
                var monthName = new System.Globalization.CultureInfo("sv-SE")
                    .DateTimeFormat.GetMonthName(group.Month);
                keyDates.Add(new TimelineKeyDate
                {
                    Date = date,
                    Description = $"{monthName} {group.Year} ({group.Count} transaktioner)",
                    EventType = "MonthEnd"
                });
            }
        }

        return keyDates
            .OrderByDescending(k => k.Date)
            .Take(limit)
            .ToList();
    }

    public async Task<JourneyStartInfo?> GetJourneyStartInfoAsync(string? userId = null)
    {
        var transactionsQuery = _context.Transactions.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
        {
            transactionsQuery = transactionsQuery.Where(t => t.UserId == userId);
        }

        var earliestTransaction = await transactionsQuery
            .OrderBy(t => t.Date)
            .FirstOrDefaultAsync();

        if (earliestTransaction == null)
        {
            return null;
        }

        var totalTransactions = await transactionsQuery.CountAsync();

        return new JourneyStartInfo
        {
            StartDate = earliestTransaction.Date,
            DaysTracked = (int)(DateTime.Today - earliestTransaction.Date.Date).TotalDays,
            TotalTransactions = totalTransactions
        };
    }
}
