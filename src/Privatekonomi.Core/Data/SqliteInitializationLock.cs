using Microsoft.EntityFrameworkCore;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Serializes first-time schema initialization when multiple app processes share a SQLite file.
/// </summary>
public static class SqliteInitializationLock
{
    public static IDisposable Acquire(PrivatekonomyContext context)
    {
        if (!context.Database.IsSqlite())
        {
            return NoopDisposable.Instance;
        }

        var dataSource = context.Database.GetDbConnection().DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
        {
            return NoopDisposable.Instance;
        }

        var databasePath = Path.GetFullPath(dataSource);
        var lockPath = $"{databasePath}.init.lock";
        var lockDirectory = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrEmpty(lockDirectory))
        {
            Directory.CreateDirectory(lockDirectory);
        }

        var timeout = DateTime.UtcNow.AddMinutes(2);
        while (true)
        {
            try
            {
                // Keep the lock file in place: deleting it can let another process lock a new inode.
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTime.UtcNow < timeout)
            {
                Thread.Sleep(100);
            }
            catch (IOException exception)
            {
                throw new TimeoutException(
                    $"Timed out waiting to initialize the SQLite database '{databasePath}'.",
                    exception);
            }
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
