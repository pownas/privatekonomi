using Privatekonomi.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BudgetCategoryTests
{
    [TestMethod]
    public void MonthlyAmountWithMonthlyPeriodReturnsSameAsPlannedAmount()
    {
        // Arrange
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 1000m,
            RecurrencePeriodMonths = 1
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(1000m, monthlyAmount);
    }

    [TestMethod]
    public void MonthlyAmountWithBiMonthlyPeriodReturnsHalfOfPlannedAmount()
    {
        // Arrange
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 1300m, // Example from issue: Avfall och vatten 1 300 kr/varannan månad
            RecurrencePeriodMonths = 2
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(650m, monthlyAmount); // 1300 / 2 = 650 kr/månad
    }

    [TestMethod]
    public void MonthlyAmountWithQuarterlyPeriodReturnsThirdOfPlannedAmount()
    {
        // Arrange
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 900m,
            RecurrencePeriodMonths = 3
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(300m, monthlyAmount); // 900 / 3 = 300 kr/månad
    }

    [TestMethod]
    public void MonthlyAmountWithSemiAnnualPeriodReturnsSixthOfPlannedAmount()
    {
        // Arrange
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 850m, // Example from issue: Danskurs 850 kr/halvår
            RecurrencePeriodMonths = 6
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(141.666666666666666666666666667m, monthlyAmount); // 850 / 6 ≈ 141.67 kr/månad
    }

    [TestMethod]
    public void MonthlyAmountWithAnnualPeriodReturnsTwelfthOfPlannedAmount()
    {
        // Arrange
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 2400m, // Example from issue: Fritidsaktivitet 2 400 kr/år
            RecurrencePeriodMonths = 12
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(200m, monthlyAmount); // 2400 / 12 = 200 kr/månad
    }

    [TestMethod]
    public void MonthlyAmountWithAnnualGymMembershipReturns150PerMonth()
    {
        // Arrange - Example from issue: Gymkort 1 800 kr/år
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 1800m,
            RecurrencePeriodMonths = 12
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(150m, monthlyAmount); // 1800 / 12 = 150 kr/månad
    }

    [TestMethod]
    public void MonthlyAmountWithZeroPeriodReturnsPlannedAmount()
    {
        // Arrange - Edge case: invalid period should default to planned amount
        var budgetCategory = new BudgetCategory
        {
            PlannedAmount = 1000m,
            RecurrencePeriodMonths = 0
        };

        // Act
        var monthlyAmount = budgetCategory.MonthlyAmount;

        // Assert
        Assert.AreEqual(1000m, monthlyAmount);
    }

    [TestMethod]
    public void RecurrencePeriodMonthsDefaultValueIsOne()
    {
        // Arrange & Act
        var budgetCategory = new BudgetCategory();

        // Assert
        Assert.AreEqual(1, budgetCategory.RecurrencePeriodMonths);
    }
}
