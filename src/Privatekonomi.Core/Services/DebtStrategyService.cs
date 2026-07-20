using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;
using System.Text;

namespace Privatekonomi.Core.Services;

public class DebtStrategyService : IDebtStrategyService
{
    private readonly PrivatekonomyContext _context;

    public DebtStrategyService(PrivatekonomyContext context)
    {
        _context = context;
    }

    public List<AmortizationScheduleEntry> GenerateAmortizationSchedule(Loan loan, decimal extraMonthlyPayment = 0)
    {
        var schedule = new List<AmortizationScheduleEntry>();
        
        if (loan.Amount <= 0)
            return schedule;

        var balance = loan.Amount;
        var monthlyRate = loan.InterestRate / 100 / 12;
        var regularPayment = loan.Amortization + (loan.Amount * monthlyRate);
        var totalPayment = regularPayment + extraMonthlyPayment;
        var currentDate = loan.StartDate ?? DateTime.Today;
        var paymentNumber = 1;
        var totalInterest = 0m;

        while (balance > 0 && paymentNumber <= 600) // Max 50 years
        {
            var interestPayment = balance * monthlyRate;
            var principalPayment = Math.Min(totalPayment - interestPayment, balance);
            
            // Ensure we don't overpay
            if (principalPayment + interestPayment > balance + interestPayment)
            {
                principalPayment = balance;
            }

            var actualPayment = principalPayment + interestPayment;
            balance -= principalPayment;
            totalInterest += interestPayment;

            schedule.Add(new AmortizationScheduleEntry
            {
                PaymentNumber = paymentNumber,
                PaymentDate = currentDate.AddMonths(paymentNumber - 1),
                BeginningBalance = balance + principalPayment,
                Payment = actualPayment,
                Principal = principalPayment,
                Interest = interestPayment,
                ExtraPayment = extraMonthlyPayment,
                EndingBalance = balance,
                TotalInterestPaid = totalInterest
            });

            paymentNumber++;
        }

        return schedule;
    }

    public async Task<DebtPayoffStrategy> CalculateSnowballStrategy(decimal availableMonthlyPayment)
    {
        var loans = await _context.Loans.OrderBy(l => l.Amount).ToListAsync();
        return CalculatePayoffStrategy(loans, availableMonthlyPayment, "Snöboll", 
            "Betala av minsta skulden först för psykologiska vinster och momentum");
    }

    public async Task<DebtPayoffStrategy> CalculateAvalancheStrategy(decimal availableMonthlyPayment)
    {
        var loans = await _context.Loans.OrderByDescending(l => l.InterestRate).ToListAsync();
        return CalculatePayoffStrategy(loans, availableMonthlyPayment, "Lavin", 
            "Betala av skulden med högst ränta först för att minimera totala räntekostnader");
    }

    public ExtraPaymentAnalysis AnalyzeExtraPayment(Loan loan, decimal extraMonthlyPayment)
    {
        var originalSchedule = GenerateAmortizationSchedule(loan, 0);
        var newSchedule = GenerateAmortizationSchedule(loan, extraMonthlyPayment);

        return new ExtraPaymentAnalysis
        {
            ExtraMonthlyPayment = extraMonthlyPayment,
            OriginalPayoffDate = originalSchedule.LastOrDefault()?.PaymentDate ?? DateTime.Today,
            NewPayoffDate = newSchedule.LastOrDefault()?.PaymentDate ?? DateTime.Today,
            MonthsSaved = originalSchedule.Count - newSchedule.Count,
            OriginalTotalInterest = originalSchedule.LastOrDefault()?.TotalInterestPaid ?? 0,
            NewTotalInterest = newSchedule.LastOrDefault()?.TotalInterestPaid ?? 0,
            InterestSavings = (originalSchedule.LastOrDefault()?.TotalInterestPaid ?? 0) - 
                            (newSchedule.LastOrDefault()?.TotalInterestPaid ?? 0),
            TotalExtraPayments = extraMonthlyPayment * newSchedule.Count,
            NetSavings = ((originalSchedule.LastOrDefault()?.TotalInterestPaid ?? 0) - 
                         (newSchedule.LastOrDefault()?.TotalInterestPaid ?? 0)) - 
                         (extraMonthlyPayment * newSchedule.Count)
        };
    }

    public async Task<DateTime?> CalculateDebtFreeDate()
    {
        var loans = await _context.Loans.ToListAsync();
        
        if (!loans.Any())
            return null;

        var latestPayoffDate = DateTime.Today;

        foreach (var loan in loans)
        {
            var schedule = GenerateAmortizationSchedule(loan, loan.ExtraMonthlyPayment ?? 0);
            var payoffDate = schedule.LastOrDefault()?.PaymentDate;
            
            if (payoffDate.HasValue && payoffDate.Value > latestPayoffDate)
            {
                latestPayoffDate = payoffDate.Value;
            }
        }

        return latestPayoffDate;
    }

    public async Task<(DebtPayoffStrategy Snowball, DebtPayoffStrategy Avalanche)> CompareStrategies(decimal availableMonthlyPayment)
    {
        var snowball = await CalculateSnowballStrategy(availableMonthlyPayment);
        var avalanche = await CalculateAvalancheStrategy(availableMonthlyPayment);
        
        return (snowball, avalanche);
    }

    private DebtPayoffStrategy CalculatePayoffStrategy(List<Loan> loans, decimal availableMonthlyPayment, 
        string strategyName, string description)
    {
        var strategy = new DebtPayoffStrategy
        {
            StrategyName = strategyName,
            Description = description,
            PayoffPlans = new List<DebtPayoffPlan>()
        };

        if (!loans.Any())
            return strategy;

        // Clone loans to simulate payoff
        var remainingLoans = loans.Select(l => new LoanSimulation
        {
            Loan = l,
            Balance = l.Amount,
            MinPayment = l.Amortization
        }).ToList();

        var currentDate = DateTime.Today;
        var totalInterest = 0m;
        var payoffOrder = 1;
        var availableExtra = availableMonthlyPayment - remainingLoans.Sum(l => l.MinPayment);

        if (availableExtra < 0)
        {
            // Not enough money to cover minimum payments
            strategy.DebtFreeDate = DateTime.MaxValue;
            return strategy;
        }

        while (remainingLoans.Any())
        {
            var targetLoan = remainingLoans.First();
            var monthlyRate = targetLoan.Loan.InterestRate / 100 / 12;

            // Apply minimum payments to all loans
            foreach (var loan in remainingLoans)
            {
                var interest = loan.Balance * (loan.Loan.InterestRate / 100 / 12);
                var principal = Math.Min(loan.MinPayment, loan.Balance);
                loan.Balance -= principal;
                totalInterest += interest;
            }

            // Apply extra payment to target loan
            if (availableExtra > 0 && targetLoan.Balance > 0)
            {
                var interest = targetLoan.Balance * monthlyRate;
                var principal = Math.Min(availableExtra, targetLoan.Balance);
                targetLoan.Balance -= principal;
                totalInterest += interest;
            }

            // Check if target loan is paid off
            if (targetLoan.Balance <= 0.01m) // Small tolerance for rounding
            {
                strategy.PayoffPlans.Add(new DebtPayoffPlan
                {
                    LoanId = targetLoan.Loan.LoanId,
                    LoanName = targetLoan.Loan.Name,
                    PayoffOrder = payoffOrder++,
                    PayoffDate = currentDate,
                    TotalInterestPaid = totalInterest,
                    MonthsToPayoff = (currentDate.Year - DateTime.Today.Year) * 12 + 
                                    (currentDate.Month - DateTime.Today.Month)
                });

                availableExtra += targetLoan.MinPayment; // Add freed up money to extra
                remainingLoans.Remove(targetLoan);
            }

            currentDate = currentDate.AddMonths(1);

            // Safety check to prevent infinite loop
            if (currentDate > DateTime.Today.AddYears(50))
            {
                break;
            }
        }

        strategy.DebtFreeDate = currentDate;
        strategy.TotalInterestPaid = totalInterest;
        strategy.TotalAmountPaid = loans.Sum(l => l.Amount) + totalInterest;
        strategy.TotalMonths = (currentDate.Year - DateTime.Today.Year) * 12 + 
                              (currentDate.Month - DateTime.Today.Month);

        return strategy;
    }

    public byte[] ExportAmortizationScheduleToCsv(Loan loan, decimal extraMonthlyPayment = 0)
    {
        var schedule = GenerateAmortizationSchedule(loan, extraMonthlyPayment);
        var csv = new StringBuilder();
        
        // Header with loan information
        csv.AppendLine(CultureInfo.InvariantCulture, $"Amorteringsplan för: {loan.Name}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Lånebelopp: {loan.Amount:N2} kr");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Ränta: {loan.InterestRate:F2}%");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Månatlig amortering: {loan.Amortization:N2} kr");
        if (extraMonthlyPayment > 0)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"Extra månatlig amortering: {extraMonthlyPayment:N2} kr");
        }
        csv.AppendLine();
        
        // Column headers
        csv.AppendLine("Betalning,Datum,Ingående Saldo,Betalning,Ränta,Amortering,Utgående Saldo,Total Ränta");
        
        // Data rows
        foreach (var entry in schedule)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"{entry.PaymentNumber}," +
                          $"{entry.PaymentDate:yyyy-MM-dd}," +
                          $"{entry.BeginningBalance:F2}," +
                          $"{entry.Payment:F2}," +
                          $"{entry.Interest:F2}," +
                          $"{entry.Principal:F2}," +
                          $"{entry.EndingBalance:F2}," +
                          $"{entry.TotalInterestPaid:F2}");
        }
        
        // Summary
        csv.AppendLine();
        csv.AppendLine("Sammanfattning");
        var lastEntry = schedule.LastOrDefault();
        if (lastEntry != null)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"Antal betalningar,{schedule.Count}");
            csv.AppendLine(CultureInfo.InvariantCulture, $"Total ränta,{lastEntry.TotalInterestPaid:N2} kr");
            csv.AppendLine(CultureInfo.InvariantCulture, $"Total kostnad,{(loan.Amount + lastEntry.TotalInterestPaid):N2} kr");
            csv.AppendLine(CultureInfo.InvariantCulture, $"Betalt datum,{lastEntry.PaymentDate:yyyy-MM-dd}");
        }
        
        // Use UTF-8 with BOM for proper Excel compatibility with Swedish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public byte[] ExportStrategyToCsv(DebtPayoffStrategy strategy, List<Loan> loans)
    {
        var csv = new StringBuilder();
        
        // Header with strategy information
        csv.AppendLine(CultureInfo.InvariantCulture, $"Avbetalningsstrategi: {strategy.StrategyName}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Beskrivning: {strategy.Description}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Skuldfri datum: {strategy.DebtFreeDate:yyyy-MM-dd}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Total ränta: {strategy.TotalInterestPaid:N2} kr");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Total kostnad: {strategy.TotalAmountPaid:N2} kr");
        csv.AppendLine(CultureInfo.InvariantCulture, $"Antal månader: {strategy.TotalMonths}");
        csv.AppendLine();
        
        // Payoff order
        csv.AppendLine("Avbetalningsordning");
        csv.AppendLine("Ordning,Lån,Belopp,Ränta,Betalt datum,Månader,Total ränta");
        foreach (var plan in strategy.PayoffPlans.OrderBy(p => p.PayoffOrder))
        {
            var loan = loans.FirstOrDefault(l => l.LoanId == plan.LoanId);
            var amount = loan?.Amount ?? 0;
            var interestRate = loan?.InterestRate ?? 0;
            
            csv.AppendLine(CultureInfo.InvariantCulture, $"{plan.PayoffOrder}," +
                          $"\"{plan.LoanName}\"," +
                          $"{amount:F2}," +
                          $"{interestRate:F2}%," +
                          $"{plan.PayoffDate:yyyy-MM-dd}," +
                          $"{plan.MonthsToPayoff}," +
                          $"{plan.TotalInterestPaid:F2}");
        }
        
        // Use UTF-8 with BOM for proper Excel compatibility with Swedish characters
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    public async Task<DetailedDebtPayoffStrategy> GenerateDetailedStrategy(string strategy, decimal availableMonthlyPayment)
    {
        // Get loans ordered by strategy
        var loans = string.Equals(strategy, "snowball", StringComparison.OrdinalIgnoreCase)
            ? await _context.Loans.OrderBy(l => l.Amount).ToListAsync()
            : await _context.Loans.OrderByDescending(l => l.InterestRate).ToListAsync();

        var detailedStrategy = new DetailedDebtPayoffStrategy
        {
            StrategyName = strategy,
            Description = string.Equals(strategy, "snowball", StringComparison.OrdinalIgnoreCase)
                ? "Betala av minsta skulden först för psykologiska vinster och momentum"
                : "Betala av skulden med högst ränta först för att minimera totala räntekostnader"
        };

        if (!loans.Any())
            return detailedStrategy;

        // Clone loans for simulation
        var remainingLoans = loans.Select(l => new LoanSimulation
        {
            Loan = l,
            Balance = l.Amount,
            MinPayment = l.Amortization,
            TotalInterestPaid = 0
        }).ToList();

        var currentDate = DateTime.Today;
        var monthNumber = 1;
        var totalInterestPaid = 0m;
        var payoffOrder = 1;
        var loanSummaries = new Dictionary<int, DetailedLoanSummary>();
        
        // Initialize loan summaries
        foreach (var loan in loans)
        {
            loanSummaries[loan.LoanId] = new DetailedLoanSummary
            {
                LoanId = loan.LoanId,
                LoanName = loan.Name,
                OriginalAmount = loan.Amount,
                InterestRate = loan.InterestRate,
                PayoffOrder = 0,
                TotalInterestPaid = 0,
                TotalAmountPaid = 0
            };
        }

        // Calculate available extra payment
        var totalMinPayment = remainingLoans.Sum(l => l.MinPayment);
        var availableExtra = availableMonthlyPayment - totalMinPayment;

        if (availableExtra < 0)
        {
            detailedStrategy.DebtFreeDate = DateTime.MaxValue;
            return detailedStrategy;
        }

        // Simulate month by month
        while (remainingLoans.Any() && monthNumber <= 600) // Max 50 years
        {
            var monthlyPayment = new MonthlyStrategyPayment
            {
                MonthNumber = monthNumber,
                PaymentDate = currentDate,
                LoanPayments = new List<LoanPaymentDetail>()
            };

            var targetLoan = remainingLoans.First();
            var loansToRemove = new List<LoanSimulation>();

            // Process each loan
            foreach (var loanSim in remainingLoans)
            {
                var monthlyRate = loanSim.Loan.InterestRate / 100 / 12;
                var interest = loanSim.Balance * monthlyRate;
                var isFocusLoan = loanSim == targetLoan;
                
                // Calculate payment
                decimal payment;
                decimal principal;
                
                if (isFocusLoan && availableExtra > 0)
                {
                    // Target loan gets minimum payment + extra
                    payment = Math.Min(loanSim.MinPayment + availableExtra + interest, loanSim.Balance + interest);
                    principal = Math.Min(payment - interest, loanSim.Balance);
                }
                else
                {
                    // Other loans get minimum payment only
                    payment = Math.Min(loanSim.MinPayment + interest, loanSim.Balance + interest);
                    principal = Math.Min(payment - interest, loanSim.Balance);
                }

                var beginningBalance = loanSim.Balance;
                loanSim.Balance -= principal;
                loanSim.TotalInterestPaid += interest;
                totalInterestPaid += interest;

                var loanPayment = new LoanPaymentDetail
                {
                    LoanId = loanSim.Loan.LoanId,
                    LoanName = loanSim.Loan.Name,
                    BeginningBalance = beginningBalance,
                    Payment = payment,
                    Principal = principal,
                    Interest = interest,
                    EndingBalance = loanSim.Balance,
                    IsFocusLoan = isFocusLoan,
                    IsPaidOff = false
                };

                // Update loan summary
                var summary = loanSummaries[loanSim.Loan.LoanId];
                summary.TotalInterestPaid += interest;
                summary.TotalAmountPaid += payment;

                // Check if loan is paid off
                if (loanSim.Balance <= 0.01m)
                {
                    loanPayment.IsPaidOff = true;
                    summary.PayoffOrder = payoffOrder++;
                    summary.PayoffDate = currentDate;
                    summary.MonthsToPayoff = monthNumber;
                    loansToRemove.Add(loanSim);
                    
                    // Free up this loan's minimum payment for extra payments
                    availableExtra += loanSim.MinPayment;
                }

                monthlyPayment.LoanPayments.Add(loanPayment);
                monthlyPayment.TotalPayment += payment;
                monthlyPayment.TotalPrincipal += principal;
                monthlyPayment.TotalInterest += interest;
            }

            monthlyPayment.RemainingTotalDebt = remainingLoans.Sum(l => l.Balance);
            detailedStrategy.MonthlySchedule.Add(monthlyPayment);

            // Remove paid off loans
            foreach (var loan in loansToRemove)
            {
                remainingLoans.Remove(loan);
            }

            currentDate = currentDate.AddMonths(1);
            monthNumber++;
        }

        // Finalize strategy
        detailedStrategy.DebtFreeDate = currentDate;
        detailedStrategy.TotalInterestPaid = totalInterestPaid;
        detailedStrategy.TotalAmountPaid = loans.Sum(l => l.Amount) + totalInterestPaid;
        detailedStrategy.TotalMonths = monthNumber - 1;
        detailedStrategy.LoanSummaries = loanSummaries.Values.OrderBy(s => s.PayoffOrder).ToList();

        return detailedStrategy;
    }

    private sealed class LoanSimulation
    {
        public Loan Loan { get; set; } = null!;
        public decimal Balance { get; set; }
        public decimal MinPayment { get; set; }
        public decimal TotalInterestPaid { get; set; }
    }
}
