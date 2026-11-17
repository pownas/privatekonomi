using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Core.Tests;

[TestClass]
public sealed class SqliteInitializationLockTests
{
    [TestMethod]
    public async Task Acquire_SerializesInitializationForSharedDatabase()
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
            var secondAcquired = false;
            var secondTask = Task.Run(() =>
            {
                using var secondLock = SqliteInitializationLock.Acquire(context);
                secondAcquired = true;
            });

            try
            {
                await Task.Delay(250);
                Assert.IsFalse(secondAcquired, "A second initializer acquired the lock before the first released it.");
            }
            finally
            {
                firstLock.Dispose();
            }

            await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(secondAcquired);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
