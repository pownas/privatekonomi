using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for cash flow scenario analysis and compound interest calculations
/// </summary>
public class CashFlowScenarioService : ICashFlowScenarioService
{
    private readonly ITransactionService _transactionService;
    private readonly IGoalService _goalService;
    private readonly ICurrentUserService _currentUserService;

    public CashFlowScenarioService(
        ITransactionService transactionService,
        IGoalService goalService,
        ICurrentUserService currentUserService)
    {
        _transactionService = transactionService;
        _goalService = goalService;
        _currentUserService = currentUserService;
    }

    public Task<ScenarioProjection> CalculateScenarioAsync(CashFlowScenario scenario)
    {
        var projection = new ScenarioProjection
        {
            ScenarioName = scenario.Name
        };

        var monthlyInterestRate = scenario.AnnualInterestRate / 100 / 12;
        var monthlyInflationRate = scenario.InflationRate.HasValue 
            ? scenario.InflationRate.Value / 100 / 12 
            : 0;

        var currentBalance = scenario.InitialAmount;
        var totalContributions = scenario.InitialAmount;
        var totalInterest = 0m;
        var currentMonthlySavings = scenario.MonthlySavings;

        var startDate = DateTime.Today;
        var totalMonths = scenario.Years * 12;

        // Add initial state
        projection.MonthlyData.Add(new MonthlyProjection
        {
            Month = 0,
            Date = startDate,
            Balance = currentBalance,
            CumulativeContributions = totalContributions,
            CumulativeInterest = 0,
            MonthlyContribution = 0,
            InterestThisMonth = 0,
            RealValue = scenario.InflationRate.HasValue ? currentBalance : null
        });

        for (int month = 1; month <= totalMonths; month++)
        {
            var date = startDate.AddMonths(month);
            
            // Adjust monthly savings if annual increase is specified
            if (scenario.AnnualSavingsIncrease.HasValue && month > 1 && (month - 1) % 12 == 0)
            {
                currentMonthlySavings *= (1 + scenario.AnnualSavingsIncrease.Value / 100);
            }

            // Add monthly contribution
            var monthlyContribution = currentMonthlySavings;
            
            // Add extra contribution if specified for this month
            if (scenario.ExtraContribution.HasValue && 
                scenario.ExtraContributionMonth.HasValue &&
                month == scenario.ExtraContributionMonth.Value)
            {
                monthlyContribution += scenario.ExtraContribution.Value;
            }

            currentBalance += monthlyContribution;
            totalContributions += monthlyContribution;

            // Calculate interest on current balance
            var interestThisMonth = currentBalance * monthlyInterestRate;
            currentBalance += interestThisMonth;
            totalInterest += interestThisMonth;

            // Calculate real value adjusted for inflation
            decimal? realValue = null;
            if (scenario.InflationRate.HasValue)
            {
                var inflationFactor = (decimal)Math.Pow((double)(1 - monthlyInflationRate), month);
                realValue = currentBalance * inflationFactor;
            }

            projection.MonthlyData.Add(new MonthlyProjection
            {
                Month = month,
                Date = date,
                Balance = currentBalance,
                CumulativeContributions = totalContributions,
                CumulativeInterest = totalInterest,
                MonthlyContribution = monthlyContribution,
                InterestThisMonth = interestThisMonth,
                RealValue = realValue
            });
        }

        projection.FinalAmount = currentBalance;
        projection.TotalContributions = totalContributions;
        projection.TotalInterest = totalInterest;
        
        if (scenario.InflationRate.HasValue)
        {
            projection.RealValue = projection.MonthlyData.Last().RealValue;
        }

        return Task.FromResult(projection);
    }

    public async Task<CashFlowScenario> GetUserBasedDefaultsAsync()
    {
        var userId = _currentUserService.UserId;
        
        if (string.IsNullOrEmpty(userId) || !_currentUserService.IsAuthenticated)
        {
            return GetGuestDefaults();
        }

        // Get last 3 months of transactions to calculate average
        var threeMonthsAgo = DateTime.Today.AddMonths(-3);
        var transactions = await _transactionService.GetTransactionsByDateRangeAsync(threeMonthsAgo, DateTime.Today);
        
        // Calculate average monthly income
        var monthlyIncome = transactions
            .WhereIncludedInTotals()
            .Where(t => t.IsIncome)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(t => t.Amount))
            .DefaultIfEmpty(0)
            .Average();
        
        // Calculate average monthly expenses
        var monthlyExpenses = transactions
            .WhereIncludedInTotals()
            .Where(t => !t.IsIncome)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(t => t.Amount))
            .DefaultIfEmpty(0)
            .Average();
        
        // Calculate average monthly savings (income - expenses)
        var averageMonthlySavings = Math.Max(0, monthlyIncome - monthlyExpenses);
        
        // Get current savings from active goals
        var goals = await _goalService.GetActiveGoalsAsync();
        var currentSavings = goals.Sum(g => g.CurrentAmount);

        return new CashFlowScenario
        {
            Name = "Mitt scenario",
            InitialAmount = currentSavings,
            MonthlySavings = Math.Round(averageMonthlySavings, 0),
            AnnualInterestRate = 3.0m, // Default reasonable interest rate
            Years = 10,
            InflationRate = 2.0m, // Default Swedish inflation target
            AnnualSavingsIncrease = null,
            ExtraContribution = null,
            ExtraContributionMonth = null
        };
    }

    public CashFlowScenario GetGuestDefaults()
    {
        return new CashFlowScenario
        {
            Name = "Exempel scenario",
            InitialAmount = 50000m,
            MonthlySavings = 3000m,
            AnnualInterestRate = 3.0m,
            Years = 10,
            InflationRate = 2.0m,
            AnnualSavingsIncrease = null,
            ExtraContribution = null,
            ExtraContributionMonth = null
        };
    }

    public async Task<List<ScenarioProjection>> CompareMultipleScenariosAsync(List<CashFlowScenario> scenarios)
    {
        var projections = new List<ScenarioProjection>();
        
        foreach (var scenario in scenarios)
        {
            var projection = await CalculateScenarioAsync(scenario);
            projections.Add(projection);
        }
        
        return projections;
    }
}
