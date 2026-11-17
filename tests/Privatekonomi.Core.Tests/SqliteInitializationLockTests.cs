using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Core.Tests;

[TestClass]
public sealed class SqliteInitializationLockTests
{
    [TestMethod]
    public async Task AcquireSerializesInitializationForSharedDatabase()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"privatekonomi-lock-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var databasePath = Path.Combine(directory, "shared.db");
            var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            using var context = new PrivatekonomyContext(options);
            var firstLock = SqliteInitializationLock.Acquire(context);
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondTask = Task.Run(() =>
            {
                secondStarted.SetResult();
                using var secondLock = SqliteInitializationLock.Acquire(context);
            });

            try
            {
                await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await Task.Delay(250);
                Assert.IsFalse(secondTask.IsCompleted, "A second initializer acquired the lock before the first released it.");
            }
            finally
            {
                firstLock.Dispose();
            }
            await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
            await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
