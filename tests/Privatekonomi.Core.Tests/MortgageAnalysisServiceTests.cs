using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class MortgageAnalysisServiceTests
{
    private PrivatekonomyContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PrivatekonomyContext(options);
    }

    #region Amortization Requirement Tests

    [TestMethod]
    public void CalculateAmortizationRequirementLtvOver70Requires2PercentAnnual()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000, // 3 million SEK
            PropertyValue = 4000000, // 4 million SEK
            InterestRate = 4.5m,
            Amortization = 3000 // Less than required
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.AreEqual(75m, result.LoanToValueRatio); // 3M/4M = 75%
        Assert.AreEqual(AmortizationRule.TwoPercentAnnual, result.ApplicableRule);
        Assert.AreEqual(60000m, result.RequiredAnnualAmortization); // 2% of 3M
        Assert.AreEqual(5000m, result.RequiredMonthlyAmortization); // 60000 / 12
        Assert.IsFalse(result.MeetsRequirement); // 3000 < 5000
        Assert.AreEqual(2000m, result.MonthlyShortage); // 5000 - 3000
    }

    [TestMethod]
    public void CalculateAmortizationRequirementLtvBetween50And70Requires1PercentAnnual()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 2400000, // 2.4 million SEK
            PropertyValue = 4000000, // 4 million SEK
            InterestRate = 4.0m,
            Amortization = 2500 // More than required
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.AreEqual(60m, result.LoanToValueRatio); // 2.4M/4M = 60%
        Assert.AreEqual(AmortizationRule.OnePercentAnnual, result.ApplicableRule);
        Assert.AreEqual(24000m, result.RequiredAnnualAmortization); // 1% of 2.4M
        Assert.AreEqual(2000m, result.RequiredMonthlyAmortization); // 24000 / 12
        Assert.IsTrue(result.MeetsRequirement); // 2500 >= 2000
        Assert.AreEqual(0m, result.MonthlyShortage);
    }

    [TestMethod]
    public void CalculateAmortizationRequirementLtvUnder50NoRequirement()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 1500000, // 1.5 million SEK
            PropertyValue = 4000000, // 4 million SEK
            InterestRate = 3.5m,
            Amortization = 0 // No amortization
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.AreEqual(37.5m, result.LoanToValueRatio); // 1.5M/4M = 37.5%
        Assert.AreEqual(AmortizationRule.NoRequirement, result.ApplicableRule);
        Assert.AreEqual(0m, result.RequiredAnnualAmortization);
        Assert.AreEqual(0m, result.RequiredMonthlyAmortization);
        Assert.IsTrue(result.MeetsRequirement);
        Assert.AreEqual(0m, result.MonthlyShortage);
    }

    [TestMethod]
    public void CalculateAmortizationRequirementWithExtraPaymentIncludesInCalculation()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000,
            PropertyValue = 4000000,
            InterestRate = 4.5m,
            Amortization = 3000,
            ExtraMonthlyPayment = 2000 // Extra payment
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.AreEqual(5000m, result.CurrentMonthlyAmortization); // 3000 + 2000
        Assert.IsTrue(result.MeetsRequirement); // 5000 >= 5000
        Assert.AreEqual(0m, result.MonthlyShortage);
    }

    [TestMethod]
    public void CalculateAmortizationRequirementNonMortgageLoanNoRequirement()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Personal Loan",
            Type = "Privatlån",
            Amount = 100000,
            InterestRate = 6.5m,
            Amortization = 2000
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.AreEqual(AmortizationRule.NoRequirement, result.ApplicableRule);
        Assert.AreEqual(0m, result.RequiredAnnualAmortization);
        Assert.IsTrue(result.MeetsRequirement);
        StringAssert.Contains(result.RuleDescription, "endast för bolån");
    }

    [TestMethod]
    public void CalculateAmortizationRequirementCalculatesYearsToPayoff()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 2400000,
            PropertyValue = 4000000,
            InterestRate = 4.0m,
            Amortization = 10000 // 10000 * 12 = 120000/year, 2400000/120000 = 20 years
        };

        // Act
        var result = service.CalculateAmortizationRequirement(loan);

        // Assert
        Assert.IsNotNull(result.YearsToPayoff);
        Assert.AreEqual(20m, result.YearsToPayoff.Value);
    }

    #endregion

    #region Interest Rate Risk Analysis Tests

    [TestMethod]
    public void AnalyzeInterestRateRiskCreatesScenarios()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(24)
        };
        var scenarios = new[] { 0m, 1m, 2m, 3m }; // Current, +1%, +2%, +3%

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, scenarios);

        // Assert
        Assert.AreEqual(4, result.Scenarios.Count);
        Assert.AreEqual(4.0m, result.CurrentInterestRate);
        
        // Check +1% scenario
        var scenario1 = result.Scenarios[1];
        Assert.AreEqual("+1,0%", scenario1.ScenarioName);
        Assert.AreEqual(5.0m, scenario1.InterestRate);
        Assert.IsTrue(scenario1.MonthlyIncrease > 0);
        
        // Check +2% scenario
        var scenario2 = result.Scenarios[2];
        Assert.AreEqual("+2,0%", scenario2.ScenarioName);
        Assert.AreEqual(6.0m, scenario2.InterestRate);
        Assert.IsTrue(scenario2.MonthlyIncrease > scenario1.MonthlyIncrease);
    }

    [TestMethod]
    public void AnalyzeInterestRateRiskVariableRateHighRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3500000,
            PropertyValue = 4000000, // LTV = 87.5% (high)
            InterestRate = 5.0m,
            Amortization = 5000,
            IsFixedRate = false // Variable rate
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m });

        // Assert
        Assert.IsFalse(result.IsFixedRate);
        Assert.IsNull(result.RateResetDate);
        Assert.IsNull(result.MonthsUntilRateReset);
        Assert.AreEqual(RiskLevel.High, result.RiskLevel);
        StringAssert.Contains(result.RiskDescription, "Hög risk");
        StringAssert.Contains(result.RiskDescription, "Rörlig ränta");
    }

    [TestMethod]
    public void AnalyzeInterestRateRiskLongFixedPeriodLowRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 2000000,
            PropertyValue = 5000000, // LTV = 40% (low)
            InterestRate = 3.5m,
            Amortization = 5000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(48) // 4 years
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m });

        // Assert
        Assert.IsTrue(result.IsFixedRate);
        Assert.IsNotNull(result.RateResetDate);
        Assert.IsNotNull(result.MonthsUntilRateReset);
        Assert.IsTrue(result.MonthsUntilRateReset >= 48);
        Assert.AreEqual(RiskLevel.Low, result.RiskLevel);
        StringAssert.Contains(result.RiskDescription, "Låg risk");
    }

    [TestMethod]
    public void AnalyzeInterestRateRiskShortFixedPeriodHighLtvHighRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3600000,
            PropertyValue = 4000000, // LTV = 90% (very high)
            InterestRate = 4.5m,
            Amortization = 6000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(6) // Only 6 months
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m, 3m });

        // Assert
        Assert.IsTrue(result.IsFixedRate);
        Assert.AreEqual(RiskLevel.High, result.RiskLevel);
        StringAssert.Contains(result.RiskDescription, "Hög risk");
        StringAssert.Contains(result.RiskDescription, "löper ut snart");
    }

    [TestMethod]
    public void AnalyzeInterestRateRiskMediumPeriodMediumLtvMediumRisk()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Type = "Bolån",
            Amount = 3000000,
            PropertyValue = 4000000, // LTV = 75% (high)
            InterestRate = 4.0m,
            Amortization = 4000,
            IsFixedRate = true,
            RateResetDate = DateTime.Today.AddMonths(18) // 1.5 years
        };

        // Act
        var result = service.AnalyzeInterestRateRisk(loan, new[] { 0m, 1m, 2m });

        // Assert
        // With LTV > 70% and 12-36 months until reset, should be Medium risk
        Assert.AreEqual(RiskLevel.Medium, result.RiskLevel);
        StringAssert.Contains(result.RiskDescription, "Måttlig risk");
    }

    #endregion

    #region Monthly Cost Calculation Tests

    [TestMethod]
    public void CalculateMonthlyCostReturnsCorrectBreakdown()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000
        };

        // Act
        var result = service.CalculateMonthlyCost(loan);

        // Assert
        Assert.AreEqual(3000000m, result.Principal);
        Assert.AreEqual(4.0m, result.InterestRate);
        
        // Monthly interest = (3000000 * 4.0 / 100) / 12 = 10000
        Assert.AreEqual(10000m, result.MonthlyInterest);
        Assert.AreEqual(5000m, result.MonthlyAmortization);
        Assert.AreEqual(15000m, result.TotalMonthlyPayment); // 10000 + 5000
        
        Assert.AreEqual(120000m, result.AnnualInterestCost); // 10000 * 12
        Assert.AreEqual(60000m, result.AnnualAmortization); // 5000 * 12
        Assert.AreEqual(180000m, result.TotalAnnualPayment); // 15000 * 12
    }

    [TestMethod]
    public void CalculateMonthlyCostWithCustomRateUsesCustomRate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000
        };

        // Act
        var result = service.CalculateMonthlyCost(loan, 6.0m); // Custom 6% rate

        // Assert
        Assert.AreEqual(6.0m, result.InterestRate);
        
        // Monthly interest = (3000000 * 6.0 / 100) / 12 = 15000
        Assert.AreEqual(15000m, result.MonthlyInterest);
        Assert.AreEqual(20000m, result.TotalMonthlyPayment); // 15000 + 5000
    }

    [TestMethod]
    public void CalculateMonthlyCostIncludesExtraPayment()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        var loan = new Loan
        {
            LoanId = 1,
            Name = "Test Mortgage",
            Amount = 3000000,
            InterestRate = 4.0m,
            Amortization = 5000,
            ExtraMonthlyPayment = 2000
        };

        // Act
        var result = service.CalculateMonthlyCost(loan);

        // Assert
        Assert.AreEqual(7000m, result.MonthlyAmortization); // 5000 + 2000
        Assert.AreEqual(17000m, result.TotalMonthlyPayment); // 10000 interest + 7000 amortization
    }

    #endregion

    #region Upcoming Rate Resets Tests

    [TestMethod]
    public async Task GetUpcomingRateResetsAsyncReturnsOnlyUpcomingResets()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        
        var loans = new[]
        {
            new Loan 
            { 
                LoanId = 1, 
                Name = "Soon Reset",
                Type = "Bolån",
                Amount = 3000000,
                InterestRate = 4.0m,
                Amortization = 5000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(3)
            },
            new Loan 
            { 
                LoanId = 2, 
                Name = "Later Reset",
                Type = "Bolån",
                Amount = 2000000,
                InterestRate = 3.5m,
                Amortization = 4000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(8) // Outside default 6 months
            },
            new Loan 
            { 
                LoanId = 3, 
                Name = "Past Reset",
                Type = "Bolån",
                Amount = 2500000,
                InterestRate = 4.5m,
                Amortization = 4500,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(-2) // Already passed
            },
            new Loan 
            { 
                LoanId = 4, 
                Name = "Variable Rate",
                Type = "Bolån",
                Amount = 1500000,
                InterestRate = 5.0m,
                Amortization = 3000,
                IsFixedRate = false,
                RateResetDate = null
            }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingRateResetsAsync(6);

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Soon Reset", result.First().Name);
    }

    [TestMethod]
    public async Task GetUpcomingRateResetsAsyncWithCustomMonthsRespectsParameter()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        
        var loans = new[]
        {
            new Loan 
            { 
                LoanId = 1, 
                Name = "Soon Reset",
                Type = "Bolån",
                Amount = 3000000,
                InterestRate = 4.0m,
                Amortization = 5000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(3)
            },
            new Loan 
            { 
                LoanId = 2, 
                Name = "Later Reset",
                Type = "Bolån",
                Amount = 2000000,
                InterestRate = 3.5m,
                Amortization = 4000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(8)
            }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingRateResetsAsync(12); // 12 months

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task GetUpcomingRateResetsAsyncOrdersByRateResetDate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new MortgageAnalysisService(context);
        
        var loans = new[]
        {
            new Loan 
            { 
                LoanId = 1, 
                Name = "Later",
                Type = "Bolån",
                Amount = 3000000,
                InterestRate = 4.0m,
                Amortization = 5000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(5)
            },
            new Loan 
            { 
                LoanId = 2, 
                Name = "Earlier",
                Type = "Bolån",
                Amount = 2000000,
                InterestRate = 3.5m,
                Amortization = 4000,
                IsFixedRate = true,
                RateResetDate = DateTime.Today.AddMonths(2)
            }
        };
        
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingRateResetsAsync(6);

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.AreEqual("Earlier", result.First().Name);
        Assert.AreEqual("Later", result.Last().Name);
    }

    #endregion
}
