using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Adds account profile columns to legacy databases without rebuilding Identity tables or resetting accounts.
/// </summary>
public static class IdentitySchemaInitializer
{
    /// <summary>
    /// Upgrades account columns safely, including databases originally created with EnsureCreated.
    /// </summary>
    public static async Task EnsureColumnsAsync(PrivatekonomyContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
            return;

        var checkSql = context.Database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" =>
                "SELECT 1 FROM pragma_table_info('AspNetUsers') WHERE name = @column",
            "Microsoft.EntityFrameworkCore.SqlServer" =>
                "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = @column",
            "Pomelo.EntityFrameworkCore.MySql" =>
                "SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AspNetUsers' AND column_name = @column",
            _ => throw new NotSupportedException("Account schema upgrades are not supported for this database provider.")
        };
        var columns = new[]
        {
            new AddColumnOperation { Table = "AspNetUsers", Name = "FirstName", ClrType = typeof(string), IsNullable = true },
            new AddColumnOperation { Table = "AspNetUsers", Name = "LastName", ClrType = typeof(string), IsNullable = true },
            new AddColumnOperation { Table = "AspNetUsers", Name = "CreatedAt", ClrType = typeof(DateTime), DefaultValue = DateTime.UnixEpoch },
            new AddColumnOperation { Table = "AspNetUsers", Name = "LastLoginAt", ClrType = typeof(DateTime), IsNullable = true },
            new AddColumnOperation { Table = "AspNetUsers", Name = "OnboardingCompleted", ClrType = typeof(bool), DefaultValue = false },
            new AddColumnOperation { Table = "AspNetUsers", Name = "OnboardingCompletedAt", ClrType = typeof(DateTime), IsNullable = true },
            new AddColumnOperation { Table = "AspNetUsers", Name = "IsSystemAdmin", ClrType = typeof(bool), DefaultValue = false },
            new AddColumnOperation { Table = "AspNetUsers", Name = "HouseholdMemberId", ClrType = typeof(int), IsNullable = true }
        };

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var databaseLock = await context.GetService<IHistoryRepository>().AcquireDatabaseLockAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var missing = new List<MigrationOperation>();
            foreach (var column in columns)
            {
                await using var check = context.Database.GetDbConnection().CreateCommand();
                check.Transaction = transaction.GetDbTransaction();
                check.CommandText = checkSql;
                var parameter = check.CreateParameter();
                parameter.ParameterName = "@column";
                parameter.Value = column.Name;
                check.Parameters.Add(parameter);
                if (await check.ExecuteScalarAsync(cancellationToken) is null)
                    missing.Add(column);
            }

            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(missing, context.Model);
            foreach (var command in commands)
                await context.Database.ExecuteSqlRawAsync(command.CommandText, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
