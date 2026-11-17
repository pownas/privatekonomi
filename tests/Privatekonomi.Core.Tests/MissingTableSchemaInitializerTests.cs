using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;

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
    public async Task EnsureTablesAsync_IgnoresNonRelationalProvider()
    {
        await using var context = new PrivatekonomyContext(
            new DbContextOptionsBuilder<PrivatekonomyContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        await MissingTableSchemaInitializer.EnsureTablesAsync(context);
    }
}