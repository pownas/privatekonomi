using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Adds balance-sheet columns to existing databases, including those created without migration history.
/// </summary>
public static class BalanceSheetSchemaInitializer
{
    public static async Task EnsureColumnsAsync(
        PrivatekonomyContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
            return;

        var columnCheckSql = context.Database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" =>
                "SELECT 1 FROM pragma_table_info(@table) WHERE name = @column",
            "Microsoft.EntityFrameworkCore.SqlServer" =>
                "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = @table AND COLUMN_NAME = @column",
            "Pomelo.EntityFrameworkCore.MySql" =>
                "SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column",
            _ => throw new NotSupportedException("Schema upgrades are not supported for this database provider.")
        };
        var columns = new[]
        {
            new AddColumnOperation { Table = "Transactions", Name = "AccountNumber", ClrType = typeof(string), IsNullable = true },
            new AddColumnOperation { Table = "Transactions", Name = "ClearingNumber", ClrType = typeof(string), IsNullable = true },
            new AddColumnOperation { Table = "Transactions", Name = "IsInternalTransfer", ClrType = typeof(bool), DefaultValue = false },
            new AddColumnOperation { Table = "Categories", Name = "IsInternalTransferCategory", ClrType = typeof(bool), DefaultValue = false },
            new AddColumnOperation { Table = "Assets", Name = "ChartOfAccountsCode", ClrType = typeof(string), IsNullable = true, MaxLength = 20 },
            new AddColumnOperation { Table = "Investments", Name = "ChartOfAccountsCode", ClrType = typeof(string), IsNullable = true, MaxLength = 20 },
            new AddColumnOperation { Table = "Loans", Name = "ChartOfAccountsCode", ClrType = typeof(string), IsNullable = true, MaxLength = 20 },
            new AddColumnOperation { Table = "CurrencyAccounts", Name = "AccountNumber", ClrType = typeof(string), IsNullable = true, MaxLength = 20 },
            new AddColumnOperation { Table = "Households", Name = "ShareBalanceSheet", ClrType = typeof(bool), DefaultValue = false },
            new AddColumnOperation { Table = "HouseholdMembers", Name = "ShareBalanceSheetConsent", ClrType = typeof(bool), DefaultValue = false },
            new AddColumnOperation { Table = "Loans", Name = "ExcludeFromBalanceSheet", ClrType = typeof(bool), DefaultValue = false }
        };

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var databaseLock = await context.GetService<IHistoryRepository>()
                .AcquireDatabaseLockAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var missing = new List<MigrationOperation>();
            foreach (var column in columns)
            {
                await using var check = context.Database.GetDbConnection().CreateCommand();
                check.Transaction = transaction.GetDbTransaction();
                check.CommandText = columnCheckSql;
                var tableParameter = check.CreateParameter();
                tableParameter.ParameterName = "@table";
                tableParameter.Value = column.Table;
                check.Parameters.Add(tableParameter);
                var columnParameter = check.CreateParameter();
                columnParameter.ParameterName = "@column";
                columnParameter.Value = column.Name;
                check.Parameters.Add(columnParameter);
                if (await check.ExecuteScalarAsync(cancellationToken) is null)
                    missing.Add(column);
            }

            if (missing.OfType<AddColumnOperation>().Any(column => column.Name == "ShareBalanceSheetConsent"))
            {
                // Existing sharing must not survive the introduction of individual consent.
                missing.Add(new SqlOperation { Sql = "UPDATE Households SET ShareBalanceSheet = 0 WHERE ShareBalanceSheet <> 0" });
            }

            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(missing, context.Model);
            foreach (var command in commands)
                await context.Database.ExecuteSqlRawAsync(command.CommandText, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (!await context.Categories.AnyAsync(c => c.Name == "Överföringar", cancellationToken))
            {
                var category = await context.Categories.FindAsync(new object[] { 35 }, cancellationToken);
                if (category == null)
                {
                    context.Categories.Add(new Category
                    {
                        CategoryId = 35,
                        Name = "Överföringar",
                        AccountNumber = "8999",
                        Color = "#607D8B",
                        IsSystemCategory = true,
                        IsInternalTransferCategory = true,
                        OriginalName = "Överföringar",
                        OriginalColor = "#607D8B",
                        OriginalAccountNumber = "8999",
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    category.IsInternalTransferCategory = true;
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
