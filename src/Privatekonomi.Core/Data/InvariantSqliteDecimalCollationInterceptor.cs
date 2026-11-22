using System.Data.Common;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Privatekonomi.Core.Data;

internal sealed class InvariantSqliteDecimalCollationInterceptor : DbConnectionInterceptor
{
    public static InvariantSqliteDecimalCollationInterceptor Instance { get; } = new();

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ConfigureDecimalCollation(connection);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ConfigureDecimalCollation(connection);
        return Task.CompletedTask;
    }

    private static void ConfigureDecimalCollation(DbConnection connection)
    {
        if (connection is SqliteConnection sqliteConnection)
        {
            sqliteConnection.CreateCollation(
                "EF_DECIMAL",
                (left, right) => decimal.Compare(
                    decimal.Parse(left, CultureInfo.InvariantCulture),
                    decimal.Parse(right, CultureInfo.InvariantCulture)));
        }
    }
}