using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Privatekonomi.Core.Migrations;

namespace Privatekonomi.Core.Data;

public static class PasskeySchemaInitializer
{
    public static async Task EnsurePasskeyTableAsync(
        PrivatekonomyContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
        {
            return;
        }

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var tableCheckSql = GetTableCheckSql(context);
            if (tableCheckSql is null)
            {
                return;
            }

            await using var checkCommand = context.Database.GetDbConnection().CreateCommand();
            checkCommand.CommandText = tableCheckSql;
            if (await checkCommand.ExecuteScalarAsync(cancellationToken) is not null)
            {
                return;
            }

            var operations = AddPasskeySupport.GetUpOperations(
                context.Database.ProviderName ?? throw new InvalidOperationException("Database provider is unavailable."));
            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(operations, context.Model);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            foreach (var command in commands)
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static string? GetTableCheckSql(PrivatekonomyContext context)
    {
        if (context.Database.IsSqlite())
        {
            return "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'AspNetUserPasskeys' LIMIT 1";
        }

        return context.Database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" =>
                "SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserPasskeys'",
            "Pomelo.EntityFrameworkCore.MySql" =>
                "SELECT 1 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'AspNetUserPasskeys' LIMIT 1",
            _ => null
        };
    }
}
