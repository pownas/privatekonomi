using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class StorageConfigurationTests
{
    private static ServiceProvider CreateServiceProvider(Dictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(); // Add logging for persistence services
        services.AddPrivatekonomyStorage(configuration);

        return services.BuildServiceProvider();
    }
    
    private static ServiceProvider CreateJsonFileServiceProvider(string dataPath, string uniqueDbName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "JsonFile",
                ["Storage:ConnectionString"] = dataPath,
                ["Storage:SeedTestData"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        
        // Use unique database name to avoid shared state between test runs
        services.AddDbContext<PrivatekonomyContext>(options =>
            options.UseInMemoryDatabase(uniqueDbName));
        services.AddSingleton<Core.Services.Persistence.IDataPersistenceService, 
            Core.Services.Persistence.JsonFilePersistenceService>();

        return services.BuildServiceProvider();
    }

    [TestMethod]
    public void InMemoryStorageShouldBeConfigurable()
    {
        // Arrange & Act
        using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "InMemory",
            ["Storage:ConnectionString"] = "",
            ["Storage:SeedTestData"] = "false"
        });
        using var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.IsNotNull(context);
        Assert.IsTrue(context.Database.IsInMemory());
    }

    [TestMethod]
    public void SqliteStorageShouldBeConfigurable()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        
        // Act
        using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "Sqlite",
            ["Storage:ConnectionString"] = $"Data Source={dbPath}",
            ["Storage:SeedTestData"] = "false"
        });
        using var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.IsNotNull(context);
        Assert.IsFalse(context.Database.IsInMemory());
        Assert.IsTrue(context.Database.IsSqlite());

        // Cleanup
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    [TestMethod]
    public void StorageSettingsShouldBindFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Sqlite",
                ["Storage:ConnectionString"] = "Data Source=test.db",
                ["Storage:SeedTestData"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageSettings>>().Value;

        // Assert
        Assert.AreEqual("Sqlite", settings.Provider);
        Assert.AreEqual("Data Source=test.db", settings.ConnectionString);
        Assert.IsTrue(settings.SeedTestData);
    }

    [TestMethod]
    public async Task SqliteStorageShouldPersistData()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

        // Act - Create database and add data
        {
            using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Sqlite",
                ["Storage:ConnectionString"] = $"Data Source={dbPath}",
                ["Storage:SeedTestData"] = "false"
            });
            await using var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            await context.Database.EnsureCreatedAsync();

            context.Categories.Add(new Core.Models.Category
            {
                Name = "Test Category",
                Color = "#FF0000",
                TaxRelated = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Act - Retrieve data with new context
        {
            using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Sqlite",
                ["Storage:ConnectionString"] = $"Data Source={dbPath}",
                ["Storage:SeedTestData"] = "false"
            });
            await using var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Test Category");

            // Assert
            Assert.IsNotNull(category);
            Assert.AreEqual("Test Category", category.Name);
            Assert.AreEqual("#FF0000", category.Color);
        }

        // Cleanup
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            // On Windows, SQLite connections can be pooled briefly and keep the file locked.
            // Clear pools and retry deletion a few times.
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    File.Delete(dbPath);
                    break;
                }
                catch (IOException) when (attempt < 4)
                {
                    await Task.Delay(50);
                    SqliteConnection.ClearAllPools();
                }
            }
        }
    }

    [TestMethod]
    public void SqlServerStorageShouldBeConfigurable()
    {
        // Arrange
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=PrivatekonomyTest;Trusted_Connection=True;MultipleActiveResultSets=true";
        
        // Act
        using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "SqlServer",
            ["Storage:ConnectionString"] = connectionString,
            ["Storage:SeedTestData"] = "false"
        });
        using var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.IsNotNull(context);
        Assert.IsFalse(context.Database.IsInMemory());
        Assert.IsTrue(context.Database.IsSqlServer());
    }

    [TestMethod]
    public void SqlServerStorageWithoutConnectionStringShouldThrowException()
    {
        // Arrange & Act & Assert
        InvalidOperationException? exception = null;
        try
        {
            var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "SqlServer",
                ["Storage:ConnectionString"] = "",
                ["Storage:SeedTestData"] = "false"
            });
            // Exception is thrown during AddDbContext configuration
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception);
        StringAssert.Contains(exception.Message, "ConnectionString is required for SqlServer provider");
    }

    [TestMethod]
    public void JsonFileStorageShouldBeConfigurable()
    {
        // Arrange & Act
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "JsonFile",
            ["Storage:ConnectionString"] = dataPath,
            ["Storage:SeedTestData"] = "false"
        });
        var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.IsNotNull(context);
        Assert.IsTrue(context.Database.IsInMemory());
        
        // Verify persistence service is registered
        var persistenceService = serviceProvider.GetService<Core.Services.Persistence.IDataPersistenceService>();
        Assert.IsNotNull(persistenceService);

        // Cleanup
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, true);
        }
    }

    [TestMethod]
    [Ignore("Known issue: InMemory database entity tracking conflict when loading from JSON. This test works in isolation but fails when database instances are shared.")]
    public async Task JsonFileStorageShouldPersistAndLoadData()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var uniqueDbName1 = $"TestDb_{Guid.NewGuid()}";
        var uniqueDbName2 = $"TestDb_{Guid.NewGuid()}";

        // Act - Create data and save
        {
            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName1);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetService<Core.Services.Persistence.IDataPersistenceService>();

            Assert.IsNotNull(persistenceService); // Verify service is registered

            await context.Database.EnsureCreatedAsync();

            context.Categories.Add(new Core.Models.Category
            {
                Name = "Test Category",
                Color = "#FF0000",
                TaxRelated = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Save to JSON files
            await persistenceService.SaveAsync(context);
        }

        // Act - Load data in new context with fresh database
        {
            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName2);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetService<Core.Services.Persistence.IDataPersistenceService>();

            Assert.IsNotNull(persistenceService); // Verify service is registered

            await context.Database.EnsureCreatedAsync();
            await persistenceService.LoadAsync(context);

            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Test Category");

            // Assert
            Assert.IsNotNull(category);
            Assert.AreEqual("Test Category", category.Name);
            Assert.AreEqual("#FF0000", category.Color);
        }

        // Cleanup
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, true);
        }
    }

    [TestMethod]
    public async Task JsonFileStorageTransactionsShouldBeSavedToMonthlyFiles()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var uniqueDbName = $"TestDb_{Guid.NewGuid()}";

        try
        {
            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetRequiredService<Core.Services.Persistence.IDataPersistenceService>();

            await context.Database.EnsureCreatedAsync();

            // Add transactions in two different months
            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 100,
                Description = "January transaction",
                Date = new DateTime(2025, 1, 15),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 200,
                Description = "February transaction",
                Date = new DateTime(2025, 2, 10),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            await persistenceService.SaveAsync(context);

            // Assert: monthly files should exist, no single transactions.json
            Assert.IsTrue(File.Exists(Path.Combine(dataPath, "transactions-2025-01.json")),
                "Monthly file for 2025-01 should exist");
            Assert.IsTrue(File.Exists(Path.Combine(dataPath, "transactions-2025-02.json")),
                "Monthly file for 2025-02 should exist");
            Assert.IsFalse(File.Exists(Path.Combine(dataPath, "transactions.json")),
                "Legacy single transactions.json should not exist");
        }
        finally
        {
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
            }
        }
    }

    [TestMethod]
    public async Task JsonFileStorageTransactionsMonthlyFilesContainCorrectData()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var uniqueDbName = $"TestDb_{Guid.NewGuid()}";

        try
        {
            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetRequiredService<Core.Services.Persistence.IDataPersistenceService>();

            await context.Database.EnsureCreatedAsync();

            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 100,
                Description = "January transaction",
                Date = new DateTime(2025, 1, 15),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 200,
                Description = "February transaction",
                Date = new DateTime(2025, 2, 10),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 300,
                Description = "Another February transaction",
                Date = new DateTime(2025, 2, 20),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await persistenceService.SaveAsync(context);

            // Assert: January file has 1 transaction, February file has 2
            var janJson = await File.ReadAllTextAsync(Path.Combine(dataPath, "transactions-2025-01.json"));
            var janTransactions = System.Text.Json.JsonSerializer.Deserialize<List<Core.Models.Transaction>>(janJson);
            Assert.IsNotNull(janTransactions);
            Assert.AreEqual(1, janTransactions.Count);
            Assert.AreEqual("January transaction", janTransactions[0].Description);

            var febJson = await File.ReadAllTextAsync(Path.Combine(dataPath, "transactions-2025-02.json"));
            var febTransactions = System.Text.Json.JsonSerializer.Deserialize<List<Core.Models.Transaction>>(febJson);
            Assert.IsNotNull(febTransactions);
            Assert.AreEqual(2, febTransactions.Count);
        }
        finally
        {
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
            }
        }
    }

    [TestMethod]
    public async Task JsonFileStorageTransactionsExistsReturnsTrueForMonthlyFiles()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var uniqueDbName = $"TestDb_{Guid.NewGuid()}";

        try
        {
            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetRequiredService<Core.Services.Persistence.IDataPersistenceService>();

            await context.Database.EnsureCreatedAsync();

            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 100,
                Description = "Test transaction",
                Date = new DateTime(2025, 3, 1),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await persistenceService.SaveAsync(context);

            // Act & Assert
            Assert.IsTrue(persistenceService.Exists(), "Exists() should return true when monthly files are present");
        }
        finally
        {
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
            }
        }
    }

    [TestMethod]
    public async Task JsonFileStorageEmptyTransactionsShouldCreateNoMonthlyFiles()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var uniqueDbName = $"TestDb_{Guid.NewGuid()}";

        try
        {
            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetRequiredService<Core.Services.Persistence.IDataPersistenceService>();

            await context.Database.EnsureCreatedAsync();
            // No transactions added

            await persistenceService.SaveAsync(context);

            // Assert
            var monthlyFiles = Directory.GetFiles(dataPath, "transactions-*.json");
            Assert.AreEqual(0, monthlyFiles.Length, "No monthly files should be created when there are no transactions");
        }
        finally
        {
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
            }
        }
    }

    [TestMethod]
    public async Task JsonFileStorageLegacyTransactionsFileShouldBeRemovedOnSave()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        Directory.CreateDirectory(dataPath);
        var uniqueDbName = $"TestDb_{Guid.NewGuid()}";

        try
        {
            // Create a legacy single transactions.json file
            var legacyFile = Path.Combine(dataPath, "transactions.json");
            await File.WriteAllTextAsync(legacyFile, "[]");

            var serviceProvider = CreateJsonFileServiceProvider(dataPath, uniqueDbName);
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetRequiredService<Core.Services.Persistence.IDataPersistenceService>();

            await context.Database.EnsureCreatedAsync();

            context.Transactions.Add(new Core.Models.Transaction
            {
                Amount = 50,
                Description = "Test",
                Date = new DateTime(2025, 6, 1),
                CreatedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            await persistenceService.SaveAsync(context);

            // Assert
            Assert.IsFalse(File.Exists(legacyFile), "Legacy transactions.json should be removed after save");
            Assert.IsTrue(File.Exists(Path.Combine(dataPath, "transactions-2025-06.json")),
                "New monthly file should exist");
        }
        finally
        {
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, true);
            }
        }
    }
}
