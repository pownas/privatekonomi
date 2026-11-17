using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BalanceSheetSchemaInitializerTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LegacySchemaIsUpgradedWithoutLosingData(bool partiallyUpgraded)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>().UseSqlite(connection).Options;
        await using var context = new PrivatekonomyContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Assets.Add(new Asset { Name = "Existing asset", CurrentValue = 1200m });
        context.Investments.Add(new Investment { Name = "Existing investment" });
        context.Loans.Add(new Loan { Name = "Existing loan", Amount = 500m });
        context.Households.Add(new Household
        {
            Name = "Existing household",
            ShareBalanceSheet = true,
            Members = [new HouseholdMember { Name = "Existing member", ShareBalanceSheetConsent = true }]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE HouseholdMembers DROP COLUMN ShareBalanceSheetConsent;
            ALTER TABLE Loans DROP COLUMN ExcludeFromBalanceSheet;
            ALTER TABLE Loans DROP COLUMN ChartOfAccountsCode;
            ALTER TABLE Investments DROP COLUMN ChartOfAccountsCode;
            ALTER TABLE Assets DROP COLUMN ChartOfAccountsCode;
            """);
        if (!partiallyUpgraded)
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Households DROP COLUMN ShareBalanceSheet");

        await BalanceSheetSchemaInitializer.EnsureColumnsAsync(context);

        Assert.AreEqual(1200m, (await context.Assets.SingleAsync()).CurrentValue);
        Assert.IsNull((await context.Assets.SingleAsync()).ChartOfAccountsCode);
        Assert.IsNull((await context.Investments.SingleAsync()).ChartOfAccountsCode);
        var loan = await context.Loans.SingleAsync();
        Assert.AreEqual(500m, loan.Amount);
        Assert.IsFalse(loan.ExcludeFromBalanceSheet);
        Assert.IsNull(loan.ChartOfAccountsCode);
        Assert.IsFalse((await context.Households.SingleAsync()).ShareBalanceSheet);
        Assert.IsFalse((await context.HouseholdMembers.SingleAsync()).ShareBalanceSheetConsent);

        loan.ExcludeFromBalanceSheet = true;
        loan.ChartOfAccountsCode = "2351";
        (await context.HouseholdMembers.SingleAsync()).ShareBalanceSheetConsent = true;
        (await context.Households.SingleAsync()).ShareBalanceSheet = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await BalanceSheetSchemaInitializer.EnsureColumnsAsync(context);

        Assert.IsTrue((await context.Households.SingleAsync()).ShareBalanceSheet);
        Assert.IsTrue((await context.HouseholdMembers.SingleAsync()).ShareBalanceSheetConsent);
        Assert.IsTrue((await context.Loans.SingleAsync()).ExcludeFromBalanceSheet);
        Assert.AreEqual("2351", (await context.Loans.SingleAsync()).ChartOfAccountsCode);
    }

    [TestMethod]
    public async Task MissingCurrencyAccountNumberIsAddedWithoutLosingData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>().UseSqlite(connection).Options;
        await using var context = new PrivatekonomyContext(options);
        await context.Database.EnsureCreatedAsync();
        context.CurrencyAccounts.Add(new CurrencyAccount
        {
            Currency = "EUR",
            Balance = 100m,
            ExchangeRate = 11.25m,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync("ALTER TABLE CurrencyAccounts DROP COLUMN AccountNumber");

        await BalanceSheetSchemaInitializer.EnsureColumnsAsync(context);

        var account = await context.CurrencyAccounts.SingleAsync();
        Assert.AreEqual("EUR", account.Currency);
        Assert.AreEqual(100m, account.Balance);
        Assert.IsNull(account.AccountNumber);

        account.AccountNumber = "1921";
        await context.SaveChangesAsync();
        await BalanceSheetSchemaInitializer.EnsureColumnsAsync(context);
        context.ChangeTracker.Clear();

        Assert.AreEqual("1921", (await context.CurrencyAccounts.SingleAsync()).AccountNumber);
    }

    [TestMethod]
    public async Task NewDatabaseAndNonRelationalProviderNeedNoUpgrade()
    {
        await using var context = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await BalanceSheetSchemaInitializer.EnsureColumnsAsync(context);

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var relational = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseSqlite(connection).Options);
        await relational.Database.EnsureCreatedAsync();
        await BalanceSheetSchemaInitializer.EnsureColumnsAsync(relational);
        Assert.AreEqual(0, await relational.Loans.CountAsync());
    }
}
