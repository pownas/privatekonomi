using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Adds model tables missing from existing relational databases created with EnsureCreated
/// and normalizes legacy CSN column names.
/// </summary>
public static class MissingTableSchemaInitializer
{
    private static readonly (string LegacyName, string CurrentName)[] LegacyCsnColumns =
    [
        ("CSN_LoanType", "CsnLoanType"),
        ("CSN_StudyYear", "CsnStudyYear"),
        ("CSN_MonthlyPayment", "CsnMonthlyPayment"),
        ("CSN_RemainingAmount", "CsnRemainingAmount"),
        ("CSN_LastUpdate", "CsnLastUpdate")
    ];

    public static async Task EnsureTablesAsync(
        PrivatekonomyContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
            return;

        var tableQuery = context.Database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" =>
                "SELECT name FROM sqlite_master WHERE type = 'table'",
            "Microsoft.EntityFrameworkCore.SqlServer" =>
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            "Pomelo.EntityFrameworkCore.MySql" =>
                "SELECT TABLE_NAME FROM information_schema.tables WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE'",
            _ => throw new NotSupportedException(
                $"Missing-table initialization is not supported for provider '{context.Database.ProviderName}'.")
        };

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var databaseLock = await context.GetService<IHistoryRepository>()
                .AcquireDatabaseLockAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = tableQuery;

            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    existingTables.Add(reader.GetString(0));
            }

            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var relationalModel = designTimeModel.GetRelationalModel();
            var missingTables = relationalModel.Tables
                .Where(table => !existingTables.Contains(table.Name))
                .Select(table => table.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (missingTables.Count > 0)
            {
                var missingSchemas = relationalModel.Tables
                    .Where(table => missingTables.Contains(table.Name))
                    .Select(table => table.Schema)
                    .OfType<string>()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var operations = context.GetService<IMigrationsModelDiffer>()
                    .GetDifferences(source: null, target: relationalModel)
                    .Where(operation => operation switch
                    {
                        CreateTableOperation table => missingTables.Contains(table.Name),
                        CreateIndexOperation index => missingTables.Contains(index.Table),
                        AddForeignKeyOperation foreignKey => missingTables.Contains(foreignKey.Table),
                        EnsureSchemaOperation schema => missingSchemas.Contains(schema.Name),
                        _ => false
                    })
                    .ToArray();

                var generatedTables = operations
                    .OfType<CreateTableOperation>()
                    .Select(table => table.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var ungeneratedTables = missingTables.Except(generatedTables, StringComparer.OrdinalIgnoreCase).ToArray();
                if (ungeneratedTables.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Could not generate schema operations for missing tables: {string.Join(", ", ungeneratedTables)}.");
                }

                var sqlCommands = context.GetService<IMigrationsSqlGenerator>().Generate(operations, designTimeModel);
                foreach (var sqlCommand in sqlCommands)
                    await context.Database.ExecuteSqlRawAsync(sqlCommand.CommandText, cancellationToken);
            }

            var loanTable = relationalModel.Tables.FirstOrDefault(table =>
                table.Columns.Any(column => column.Name == "CsnLastUpdate"));
            if (loanTable is not null && existingTables.Contains(loanTable.Name))
            {
                var existingColumns = await ReadColumnNamesAsync(
                    command,
                    context.Database.ProviderName!,
                    loanTable.Name,
                    loanTable.Schema,
                    cancellationToken);
                var renameOperations = LegacyCsnColumns
                    .Where(column => existingColumns.Contains(column.LegacyName)
                        && !existingColumns.Contains(column.CurrentName))
                    .Select(column => new RenameColumnOperation
                    {
                        Name = column.LegacyName,
                        NewName = column.CurrentName,
                        Table = loanTable.Name,
                        Schema = loanTable.Schema
                    })
                    .ToArray();

                var renameCommands = context.GetService<IMigrationsSqlGenerator>()
                    .Generate(renameOperations, designTimeModel);
                foreach (var sqlCommand in renameCommands)
                    await context.Database.ExecuteSqlRawAsync(sqlCommand.CommandText, cancellationToken);

                foreach (var operation in renameOperations)
                {
                    existingColumns.Remove(operation.Name);
                    existingColumns.Add(operation.NewName);
                }

                var addOperations = LegacyCsnColumns
                    .Where(column => !existingColumns.Contains(column.CurrentName)
                        && !existingColumns.Contains(column.LegacyName))
                    .Select(column =>
                    {
                        var modelColumn = loanTable.Columns.Single(modelColumn =>
                            modelColumn.Name == column.CurrentName);
                        return new AddColumnOperation
                        {
                            Name = column.CurrentName,
                            Table = loanTable.Name,
                            Schema = loanTable.Schema,
                            ClrType = modelColumn.PropertyMappings.First().Property.ClrType,
                            ColumnType = modelColumn.StoreType,
                            IsNullable = modelColumn.IsNullable
                        };
                    })
                    .ToArray();
                var addCommands = context.GetService<IMigrationsSqlGenerator>()
                    .Generate(addOperations, designTimeModel);
                foreach (var sqlCommand in addCommands)
                    await context.Database.ExecuteSqlRawAsync(sqlCommand.CommandText, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<HashSet<string>> ReadColumnNamesAsync(
        System.Data.Common.DbCommand command,
        string providerName,
        string tableName,
        string? schema,
        CancellationToken cancellationToken)
    {
        command.Parameters.Clear();
        command.CommandText = providerName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" =>
                $"PRAGMA table_info('{tableName.Replace("'", "''", StringComparison.Ordinal)}')",
            "Microsoft.EntityFrameworkCore.SqlServer" =>
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " +
                (schema is null
                    ? "WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = SCHEMA_NAME()"
                    : "WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = @schema"),
            "Pomelo.EntityFrameworkCore.MySql" =>
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName",
            _ => throw new NotSupportedException(
                $"Column initialization is not supported for provider '{providerName}'.")
        };

        if (providerName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            AddParameter(command, "@tableName", tableName);
            if (providerName == "Microsoft.EntityFrameworkCore.SqlServer" && schema is not null)
                AddParameter(command, "@schema", schema);
        }

        var columnNameOrdinal = providerName == "Microsoft.EntityFrameworkCore.Sqlite" ? 1 : 0;
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add(reader.GetString(columnNameOrdinal));

        return columns;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}