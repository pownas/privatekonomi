using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Adds model tables missing from existing relational databases created with EnsureCreated.
/// </summary>
public static class MissingTableSchemaInitializer
{
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

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}