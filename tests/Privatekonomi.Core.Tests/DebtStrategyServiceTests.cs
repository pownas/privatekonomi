using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class DebtStrategyServiceTests
{
    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    [TestMethod]
    public void GenerateAmortizationScheduleWithValidLoanReturnsSchedule()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Loan",
            Amount = 100000,
            InterestRate = 5.0m,
            Amortization = 2000,
            StartDate = DateTime.Today
        };

        // Act
        var schedule = service.GenerateAmortizationSchedule(loan, 0);

        // Assert
        Assert.AreNotEqual(0, schedule.Count());
        Assert.IsTrue(schedule.Count > 0);
        Assert.AreEqual(1, schedule.First().PaymentNumber);
        Assert.IsTrue(schedule.Last().EndingBalance <= 0.01m); // Should be paid off
    }

    [TestMethod]
    public void GenerateAmortizationScheduleWithExtraPaymentReducesMonths()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Loan",
            Amount = 100000,
            InterestRate = 5.0m,
            Amortization = 2000,
            StartDate = DateTime.Today
        };

        // Act
        var scheduleWithoutExtra = service.GenerateAmortizationSchedule(loan, 0);
        var scheduleWithExtra = service.GenerateAmortizationSchedule(loan, 500);

        // Assert
        Assert.IsTrue(scheduleWithExtra.Count < scheduleWithoutExtra.Count);
        Assert.IsTrue(scheduleWithExtra.Last().TotalInterestPaid < scheduleWithoutExtra.Last().TotalInterestPaid);
    }

    [TestMethod]
    public async Task CalculateSnowballStrategyOrdersBySmallestBalance()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new[]
        {
            new Loan { LoanId = 1, Name = "Large Loan", Amount = 50000, InterestRate = 3.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "Small Loan", Amount = 5000, InterestRate = 8.0m, Amortization = 200 },
            new Loan { LoanId = 3, Name = "Medium Loan", Amount = 20000, InterestRate = 5.0m, Amortization = 500 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var strategy = await service.CalculateSnowballStrategy(2000);

        // Assert
        Assert.AreEqual("Snöboll", strategy.StrategyName);
        Assert.AreNotEqual(0, strategy.PayoffPlans.Count());
        
        // Small loan should be paid off first
        var firstPayoff = strategy.PayoffPlans.OrderBy(p => p.PayoffOrder).First();
        Assert.AreEqual("Small Loan", firstPayoff.LoanName);
    }

    [TestMethod]
    public async Task CalculateAvalancheStrategyOrdersByHighestInterest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new[]
        {
            new Loan { LoanId = 1, Name = "Low Interest", Amount = 50000, InterestRate = 3.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "High Interest", Amount = 5000, InterestRate = 8.0m, Amortization = 200 },
            new Loan { LoanId = 3, Name = "Medium Interest", Amount = 20000, InterestRate = 5.0m, Amortization = 500 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var strategy = await service.CalculateAvalancheStrategy(2000);

        // Assert
        Assert.AreEqual("Lavin", strategy.StrategyName);
        Assert.AreNotEqual(0, strategy.PayoffPlans.Count());
        
        // High interest loan should be paid off first
        var firstPayoff = strategy.PayoffPlans.OrderBy(p => p.PayoffOrder).First();
        Assert.AreEqual("High Interest", firstPayoff.LoanName);
    }

    [TestMethod]
    public void AnalyzeExtraPaymentShowsInterestSavings()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Loan",
            Amount = 100000,
            InterestRate = 5.0m,
            Amortization = 2000,
            StartDate = DateTime.Today
        };

        // Act
        var analysis = service.AnalyzeExtraPayment(loan, 500);

        // Assert
        Assert.AreEqual(500, analysis.ExtraMonthlyPayment);
        Assert.IsTrue(analysis.MonthsSaved > 0);
        Assert.IsTrue(analysis.InterestSavings > 0);
        Assert.IsTrue(analysis.NewPayoffDate < analysis.OriginalPayoffDate);
    }

    [TestMethod]
    public async Task CompareStrategiesReturnsSnowballAndAvalanche()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new[]
        {
            new Loan { LoanId = 1, Name = "Loan 1", Amount = 50000, InterestRate = 3.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "Loan 2", Amount = 5000, InterestRate = 8.0m, Amortization = 200 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var (snowball, avalanche) = await service.CompareStrategies(2000);

        // Assert
        Assert.AreEqual("Snöboll", snowball.StrategyName);
        Assert.AreEqual("Lavin", avalanche.StrategyName);
        Assert.AreNotEqual(0, snowball.PayoffPlans.Count());
        Assert.AreNotEqual(0, avalanche.PayoffPlans.Count());
    }

    [TestMethod]
    public void ExportAmortizationScheduleToCsvReturnsValidCsv()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Loan",
            Amount = 50000,
            InterestRate = 5.0m,
            Amortization = 1000,
            StartDate = DateTime.Today
        };

        // Act
        var csv = service.ExportAmortizationScheduleToCsv(loan, 0);

        // Assert
        Assert.IsNotNull(csv);
        Assert.IsTrue(csv.Length > 0);
        
        var content = System.Text.Encoding.UTF8.GetString(csv);
        StringAssert.Contains(content, "Amorteringsplan för: Test Loan");
        StringAssert.Contains(content, "Lånebelopp: 50");
        StringAssert.Contains(content, "Betalning,Datum,Ingående Saldo");
    }

    [TestMethod]
    public async Task ExportStrategyToCsvReturnsValidCsv()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new List<Loan>
        {
            new Loan { LoanId = 1, Name = "Loan 1", Amount = 50000, InterestRate = 3.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "Loan 2", Amount = 5000, InterestRate = 8.0m, Amortization = 200 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        var strategy = await service.CalculateSnowballStrategy(2000);

        // Act
        var csv = service.ExportStrategyToCsv(strategy, loans);

        // Assert
        Assert.IsNotNull(csv);
        Assert.IsTrue(csv.Length > 0);
        
        var content = System.Text.Encoding.UTF8.GetString(csv);
        StringAssert.Contains(content, "Avbetalningsstrategi: Snöboll");
        StringAssert.Contains(content, "Ordning,Lån,Belopp,Ränta");
    }

    [TestMethod]
    public async Task GenerateDetailedStrategySnowballReturnsMonthlySchedule()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new[]
        {
            new Loan { LoanId = 1, Name = "Large Loan", Amount = 50000, InterestRate = 3.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "Small Loan", Amount = 5000, InterestRate = 8.0m, Amortization = 200 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var detailedStrategy = await service.GenerateDetailedStrategy("Snowball", 2000);

        // Assert
        Assert.AreEqual("Snowball", detailedStrategy.StrategyName);
        Assert.AreNotEqual(0, detailedStrategy.MonthlySchedule.Count());
        Assert.AreNotEqual(0, detailedStrategy.LoanSummaries.Count());
        Assert.AreEqual(2, detailedStrategy.LoanSummaries.Count);
        
        // First month should have payments for both loans
        var firstMonth = detailedStrategy.MonthlySchedule.First();
        Assert.AreEqual(1, firstMonth.MonthNumber);
        Assert.AreEqual(2, firstMonth.LoanPayments.Count);
        
        // Small loan should be focus loan (payoff order 1)
        var smallLoanSummary = detailedStrategy.LoanSummaries.First(s => s.LoanName == "Small Loan");
        Assert.AreEqual(1, smallLoanSummary.PayoffOrder);
    }

    [TestMethod]
    public async Task GenerateDetailedStrategyAvalancheReturnsMonthlySchedule()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new[]
        {
            new Loan { LoanId = 1, Name = "Low Interest", Amount = 50000, InterestRate = 3.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "High Interest", Amount = 5000, InterestRate = 8.0m, Amortization = 200 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var detailedStrategy = await service.GenerateDetailedStrategy("Avalanche", 2000);

        // Assert
        Assert.AreEqual("Avalanche", detailedStrategy.StrategyName);
        Assert.AreNotEqual(0, detailedStrategy.MonthlySchedule.Count());
        
        // High interest loan should be focus loan (payoff order 1)
        var highInterestSummary = detailedStrategy.LoanSummaries.First(s => s.LoanName == "High Interest");
        Assert.AreEqual(1, highInterestSummary.PayoffOrder);
    }

    [TestMethod]
    public async Task GenerateDetailedStrategyTracksInterestCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loan = new Loan 
        { 
            LoanId = 1, 
            Name = "Test Loan", 
            Amount = 10000, 
            InterestRate = 6.0m, 
            Amortization = 500 
        };
        
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        // Act
        var detailedStrategy = await service.GenerateDetailedStrategy("Snowball", 1000);

        // Assert
        Assert.IsTrue(detailedStrategy.TotalInterestPaid > 0);
        
        var loanSummary = detailedStrategy.LoanSummaries.First();
        Assert.AreEqual(loan.Amount + loanSummary.TotalInterestPaid, loanSummary.TotalAmountPaid);
    }

    [TestMethod]
    public async Task CalculateDebtFreeDateWithNoLoansReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);

        // Act
        var debtFreeDate = await service.CalculateDebtFreeDate();

        // Assert
        Assert.IsNull(debtFreeDate);
    }

    [TestMethod]
    public async Task CalculateDebtFreeDateWithLoansReturnsLatestPayoffDate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new DebtStrategyService(context);
        
        var loans = new[]
        {
            new Loan { LoanId = 1, Name = "Quick Loan", Amount = 5000, InterestRate = 5.0m, Amortization = 1000 },
            new Loan { LoanId = 2, Name = "Long Loan", Amount = 50000, InterestRate = 3.0m, Amortization = 500 }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var debtFreeDate = await service.CalculateDebtFreeDate();

        // Assert
        Assert.IsNotNull(debtFreeDate);
        Assert.IsTrue(debtFreeDate.Value > DateTime.Today);
    }
}
