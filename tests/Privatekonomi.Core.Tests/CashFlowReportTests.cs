using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class CashFlowReportTests
{
    [TestMethod]
    [DataRow("month")]
    [DataRow("week")]
    public async Task CashFlowUsesPositiveAmountsAndIncludesBothBoundaryDates(string groupBy)
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new PrivatekonomyContext(options);
        context.Transactions.AddRange(
            new Transaction { Date = new DateTime(2026, 1, 5), IsIncome = true, Amount = 1000m },
            new Transaction { Date = new DateTime(2026, 1, 5), IsIncome = false, Amount = -300m },
            new Transaction { Date = new DateTime(2026, 1, 11), IsIncome = false, Amount = 100m },
            new Transaction { Date = new DateTime(2026, 1, 4), IsIncome = true, Amount = 9000m },
            new Transaction { Date = new DateTime(2026, 1, 12), IsIncome = true, Amount = 9000m });
        await context.SaveChangesAsync();

        var report = await new ReportService(context).GetCashFlowReportAsync(
            new DateTime(2026, 1, 5, 14, 30, 0), new DateTime(2026, 1, 11, 14, 30, 0), groupBy);

        Assert.AreEqual(1000m, report.TotalIncome);
        Assert.AreEqual(400m, report.TotalExpenses);
        Assert.AreEqual(600m, report.NetCashFlow);
        Assert.AreEqual(1, report.Periods.Count);
        Assert.AreEqual(1000m, report.Periods[0].Income);
        Assert.AreEqual(400m, report.Periods[0].Expenses);
        Assert.AreEqual(600m, report.Periods[0].NetFlow);
    }
}