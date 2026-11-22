using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.DataMigration;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class DataMigrationServiceTests
{
    [TestMethod]
    public async Task MigratesSqliteDataAndApplicationUserBetweenDatabases()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"privatekonomi-source-{Guid.NewGuid():N}.db");
        var targetPath = Path.Combine(Path.GetTempPath(), $"privatekonomi-target-{Guid.NewGuid():N}.db");

        try
        {
            await using (var source = CreateSqliteContext(sourcePath))
            {
                await source.Database.EnsureCreatedAsync();
                source.Categories.Add(new Category
                {
                    Name = "Migrerad kategori",
                    Color = "#123456",
                    TaxRelated = false,
                    CreatedAt = DateTime.UtcNow
                });
                source.Users.Add(new ApplicationUser
                {
                    Id = "migration-user-id",
                    UserName = "migration@example.com",
                    NormalizedUserName = "MIGRATION@EXAMPLE.COM",
                    Email = "migration@example.com",
                    NormalizedEmail = "MIGRATION@EXAMPLE.COM"
                });
                await source.SaveChangesAsync();
            }

            var service = new DataMigrationService();
            var result = await service.MigrateAsync(new DataMigrationRequest
            {
                SourceProvider = "Sqlite",
                SourceConnectionString = $"Data Source={sourcePath}",
                TargetProvider = "Sqlite",
                TargetConnectionString = $"Data Source={targetPath}"
            });

            Assert.IsTrue(result.Success, result.Message);
            await using var target = CreateSqliteContext(targetPath);
            Assert.IsTrue(await target.Categories.AnyAsync(category =>
                category.Name == "Migrerad kategori"));
            Assert.AreEqual("Migrerad kategori",
                await target.Categories.Where(category => !category.IsSystemCategory)
                    .Select(category => category.Name).SingleAsync());
            Assert.AreEqual(1, await target.Users.CountAsync());
            Assert.AreEqual("migration@example.com",
                await target.Users.Select(user => user.Email).SingleAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteFile(sourcePath);
            DeleteFile(targetPath);
        }
    }

    [TestMethod]
    public async Task MigratesSqliteToJsonAndBackToSqlite()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"privatekonomi-json-source-{Guid.NewGuid():N}.db");
        var jsonDirectory = Path.Combine(Path.GetTempPath(), $"privatekonomi-json-{Guid.NewGuid():N}");
        var targetPath = Path.Combine(Path.GetTempPath(), $"privatekonomi-json-target-{Guid.NewGuid():N}.db");

        try
        {
            await using (var source = CreateSqliteContext(sourcePath))
            {
                await source.Database.EnsureCreatedAsync();
                source.Categories.Add(new Category
                {
                    Name = "JSON kategori",
                    Color = "#654321",
                    TaxRelated = true,
                    CreatedAt = DateTime.UtcNow
                });
                await source.SaveChangesAsync();
            }

            var service = new DataMigrationService();
            var exportResult = await service.MigrateAsync(new DataMigrationRequest
            {
                SourceProvider = "Sqlite",
                SourceConnectionString = $"Data Source={sourcePath}",
                TargetProvider = "JsonFile",
                TargetConnectionString = jsonDirectory
            });
            Assert.IsTrue(exportResult.Success, exportResult.Message);
            Assert.IsTrue(File.Exists(Path.Combine(jsonDirectory, DataMigrationSnapshot.FileName)));

            var importResult = await service.MigrateAsync(new DataMigrationRequest
            {
                SourceProvider = "JsonFile",
                SourceConnectionString = jsonDirectory,
                TargetProvider = "Sqlite",
                TargetConnectionString = $"Data Source={targetPath}"
            });
            Assert.IsTrue(importResult.Success, importResult.Message);

            await using var target = CreateSqliteContext(targetPath);
            Assert.IsTrue(await target.Categories.AnyAsync(category =>
                category.Name == "JSON kategori"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteFile(sourcePath);
            DeleteFile(targetPath);
            if (Directory.Exists(jsonDirectory))
                Directory.Delete(jsonDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task RefusesToOverwriteTargetWithExistingData()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"privatekonomi-protected-source-{Guid.NewGuid():N}.db");
        var targetPath = Path.Combine(Path.GetTempPath(), $"privatekonomi-protected-target-{Guid.NewGuid():N}.db");

        try
        {
            await using (var source = CreateSqliteContext(sourcePath))
            {
                await source.Database.EnsureCreatedAsync();
                source.Categories.Add(new Category
                {
                    Name = "Källa",
                    Color = "#111111",
                    TaxRelated = false,
                    CreatedAt = DateTime.UtcNow
                });
                await source.SaveChangesAsync();
            }

            await using (var target = CreateSqliteContext(targetPath))
            {
                await target.Database.EnsureCreatedAsync();
                target.Categories.Add(new Category
                {
                    Name = "Befintlig",
                    Color = "#222222",
                    TaxRelated = false,
                    CreatedAt = DateTime.UtcNow
                });
                await target.SaveChangesAsync();
            }

            var result = await new DataMigrationService().MigrateAsync(new DataMigrationRequest
            {
                SourceProvider = "Sqlite",
                SourceConnectionString = $"Data Source={sourcePath}",
                TargetProvider = "Sqlite",
                TargetConnectionString = $"Data Source={targetPath}"
            });

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "inte tom");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteFile(sourcePath);
            DeleteFile(targetPath);
        }
    }

    [TestMethod]
    public async Task PreviewReportsSourceCountsAndProtectedTarget()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"privatekonomi-preview-source-{Guid.NewGuid():N}.db");
        var targetPath = Path.Combine(Path.GetTempPath(), $"privatekonomi-preview-target-{Guid.NewGuid():N}.db");

        try
        {
            await using (var source = CreateSqliteContext(sourcePath))
            {
                await source.Database.EnsureCreatedAsync();
                source.Categories.Add(new Category
                {
                    Name = "Förhandsgranskad",
                    Color = "#123ABC",
                    TaxRelated = false,
                    CreatedAt = DateTime.UtcNow
                });
                await source.SaveChangesAsync();
            }

            await using (var target = CreateSqliteContext(targetPath))
            {
                await target.Database.EnsureCreatedAsync();
                target.Categories.Add(new Category
                {
                    Name = "Befintlig",
                    Color = "#ABC123",
                    TaxRelated = false,
                    CreatedAt = DateTime.UtcNow
                });
                await target.SaveChangesAsync();
            }

            var preview = await new DataMigrationService().PreviewAsync(new DataMigrationRequest
            {
                SourceProvider = "Sqlite",
                SourceConnectionString = $"Data Source={sourcePath}",
                TargetProvider = "Sqlite",
                TargetConnectionString = $"Data Source={targetPath}"
            });

            Assert.IsFalse(preview.CanMigrate);
            StringAssert.Contains(preview.Message, "inte tom");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteFile(sourcePath);
            DeleteFile(targetPath);
        }
    }

    [TestMethod]
    public async Task PreviewReportsMissingSqliteSource()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"privatekonomi-missing-{Guid.NewGuid():N}.db");
        var preview = await new DataMigrationService().PreviewAsync(new DataMigrationRequest
        {
            SourceProvider = "Sqlite",
            SourceConnectionString = $"Data Source={missingPath}",
            TargetProvider = "Sqlite",
            TargetConnectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"unused-{Guid.NewGuid():N}.db")}"
        });

        Assert.IsFalse(preview.CanMigrate);
        StringAssert.Contains(preview.Message, "finns inte");
        if (File.Exists(missingPath))
            File.Delete(missingPath);
    }

    [TestMethod]
    public async Task JsonSnapshotRejectsTampering()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"privatekonomi-checksum-source-{Guid.NewGuid():N}.db");
        var jsonDirectory = Path.Combine(Path.GetTempPath(), $"privatekonomi-checksum-json-{Guid.NewGuid():N}");
        var targetPath = Path.Combine(Path.GetTempPath(), $"privatekonomi-checksum-target-{Guid.NewGuid():N}.db");

        try
        {
            await using (var source = CreateSqliteContext(sourcePath))
            {
                await source.Database.EnsureCreatedAsync();
                source.Categories.Add(new Category
                {
                    Name = "Original",
                    Color = "#123456",
                    TaxRelated = false,
                    CreatedAt = DateTime.UtcNow
                });
                await source.SaveChangesAsync();
            }

            var exportResult = await new DataMigrationService().MigrateAsync(new DataMigrationRequest
            {
                SourceProvider = "Sqlite",
                SourceConnectionString = $"Data Source={sourcePath}",
                TargetProvider = "JsonFile",
                TargetConnectionString = jsonDirectory
            });
            Assert.IsTrue(exportResult.Success, exportResult.Message);

            var snapshotPath = Path.Combine(jsonDirectory, DataMigrationSnapshot.FileName);
            var snapshot = System.Text.Json.JsonSerializer.Deserialize<DataMigrationSnapshot>(
                await File.ReadAllTextAsync(snapshotPath))!;
            var typeName = typeof(Category).FullName!;
            var categoryRecord = snapshot.Entities[typeName][0];
            categoryRecord["Name"] = System.Text.Json.JsonSerializer.SerializeToElement("Tampered");
            await File.WriteAllTextAsync(snapshotPath,
                System.Text.Json.JsonSerializer.Serialize(snapshot));

            var importResult = await new DataMigrationService().MigrateAsync(new DataMigrationRequest
            {
                SourceProvider = "JsonFile",
                SourceConnectionString = jsonDirectory,
                TargetProvider = "Sqlite",
                TargetConnectionString = $"Data Source={targetPath}"
            });

            Assert.IsFalse(importResult.Success);
            StringAssert.Contains(importResult.Message, "integritetskontroll");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteFile(sourcePath);
            DeleteFile(targetPath);
            if (Directory.Exists(jsonDirectory))
                Directory.Delete(jsonDirectory, recursive: true);
        }
    }

    private static PrivatekonomyContext CreateSqliteContext(string path)
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        return new PrivatekonomyContext(options);
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
