using System.Reflection;
using Privatekonomi.Core.Models;
using Privatekonomi.Web.Components.Pages;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BalanceSheetTests
{
    [TestMethod]
    public async Task EmptyViewClearsPreviousMonthlyFigures()
    {
        var page = new BalanceSheet();
        var calculate = typeof(BalanceSheet).GetMethod("CalculateMonthlyAverages", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var income = typeof(BalanceSheet).GetField("_monthlyIncome", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var expenses = typeof(BalanceSheet).GetField("_monthlyExpenses", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var savings = typeof(BalanceSheet).GetField("_monthlySavings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var transactions = new[]
        {
            new Transaction { Date = DateTime.Now, IsIncome = true, Amount = 900m },
            new Transaction { Date = DateTime.Now, IsIncome = false, Amount = 300m }
        };

        await (Task)calculate.Invoke(page, [transactions])!;
        Assert.AreEqual(300m, income.GetValue(page));
        Assert.AreEqual(100m, expenses.GetValue(page));
        Assert.AreEqual(200m, savings.GetValue(page));

        await (Task)calculate.Invoke(page, [Array.Empty<Transaction>()])!;
        Assert.AreEqual(0m, income.GetValue(page));
        Assert.AreEqual(0m, expenses.GetValue(page));
        Assert.AreEqual(0m, savings.GetValue(page));
    }
}
