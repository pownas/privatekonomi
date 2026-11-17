using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class MissingTableSchemaInitializerTests
{
    [TestMethod]
    public async Task EnsureTablesAsync_CreatesMissingModelTablesAndIsIdempotent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new PrivatekonomyContext(
            new DbContextOptionsBuilder<PrivatekonomyContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("DROP TABLE BillSchedules");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE BudgetSuggestions");

        await MissingTableSchemaInitializer.EnsureTablesAsync(context);
        await MissingTableSchemaInitializer.EnsureTablesAsync(context);

        Assert.AreEqual(0, await context.BillSchedules.CountAsync());
        Assert.AreEqual(0, await context.BudgetSuggestions.CountAsync());
    }

    [TestMethod]
    public async Task EnsureTablesAsync_RenamesLegacyCsnColumnsAndPreservesData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new PrivatekonomyContext(
            new DbContextOptionsBuilder<PrivatekonomyContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();

        var loan = new Loan
        {
            Name = "CSN",
            Type = "CSN-lån",
            Amount = 100000m,
            InterestRate = 1.2m,
            Amortization = 850m,
            CreatedAt = new DateTime(2026, 1, 1),
            ValidFrom = new DateTime(2026, 1, 1),
            CsnLoanType = "Studiemedel",
            CsnStudyYear = 2024,
            CsnMonthlyPayment = 850m,
            CsnRemainingAmount = 95000m,
            CsnLastUpdate = new DateTime(2026, 1, 1)
        };
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        var legacyColumns = new[]
        {
            ("CsnLoanType", "CSN_LoanType"),
            ("CsnStudyYear", "CSN_StudyYear"),
            ("CsnMonthlyPayment", "CSN_MonthlyPayment"),
            ("CsnRemainingAmount", "CSN_RemainingAmount"),
            ("CsnLastUpdate", "CSN_LastUpdate")
        };
        foreach (var (currentName, legacyName) in legacyColumns)
        {
            await context.Database.ExecuteSqlRawAsync(
                $"ALTER TABLE \"Loans\" RENAME COLUMN \"{currentName}\" TO \"{legacyName}\"");
        }
        context.ChangeTracker.Clear();

        await MissingTableSchemaInitializer.EnsureTablesAsync(context);

        var persistedLoan = await context.Loans.AsNoTracking().SingleAsync();
        Assert.AreEqual("Studiemedel", persistedLoan.CsnLoanType);
        Assert.AreEqual(2024, persistedLoan.CsnStudyYear);
        Assert.AreEqual(850m, persistedLoan.CsnMonthlyPayment);
        Assert.AreEqual(95000m, persistedLoan.CsnRemainingAmount);
        Assert.AreEqual(new DateTime(2026, 1, 1), persistedLoan.CsnLastUpdate);
    }

    [TestMethod]
    public async Task EnsureTablesAsync_AddsMissingCsnColumn()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new PrivatekonomyContext(
            new DbContextOptionsBuilder<PrivatekonomyContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        context.Loans.Add(new Loan
        {
            Name = "CSN",
            Type = "CSN-lån",
            Amount = 100000m,
            InterestRate = 1.2m,
            Amortization = 850m,
            CreatedAt = new DateTime(2026, 1, 1),
            ValidFrom = new DateTime(2026, 1, 1)
        });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Loans\" DROP COLUMN \"CsnLastUpdate\"");
        context.ChangeTracker.Clear();

        await MissingTableSchemaInitializer.EnsureTablesAsync(context);

        var persistedLoan = await context.Loans.AsNoTracking().SingleAsync();
        Assert.IsNull(persistedLoan.CsnLastUpdate);
    }

    [TestMethod]
    public async Task EnsureTablesAsync_IgnoresNonRelationalProvider()
    {
        await using var context = new PrivatekonomyContext(
            new DbContextOptionsBuilder<PrivatekonomyContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        await MissingTableSchemaInitializer.EnsureTablesAsync(context);
    }
}