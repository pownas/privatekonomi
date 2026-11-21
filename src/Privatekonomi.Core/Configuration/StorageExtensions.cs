using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Services.Persistence;

namespace Privatekonomi.Core.Configuration;

/// <summary>
/// Extension methods for configuring storage providers
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Configures the PrivatekonomyContext based on StorageSettings configuration
    /// </summary>
    public static IServiceCollection AddPrivatekonomyStorage(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var storageSettings = new StorageSettings();
        configuration.GetSection("Storage").Bind(storageSettings);
        
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        
        switch (storageSettings.Provider?.ToLowerInvariant())
        {
            case "sqlite":
                services.AddDbContextFactory<PrivatekonomyContext>(options =>
                {
                    var connectionString = storageSettings.ConnectionString;
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        connectionString = "Data Source=privatekonomi.db";
                    }
                    options.UseSqlite(connectionString);
                });
                break;

            case "sqlserver":
                if (string.IsNullOrEmpty(storageSettings.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "ConnectionString is required for SqlServer provider. " +
                        "Please specify a connection string in Storage:ConnectionString configuration.");
                }
                services.AddDbContextFactory<PrivatekonomyContext>(options =>
                {
                    options.UseSqlServer(storageSettings.ConnectionString);
                });
                break;

            case "mysql":
            case "mariadb":
                if (string.IsNullOrEmpty(storageSettings.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "ConnectionString is required for MySQL/MariaDB provider. " +
                        "Please specify a connection string in Storage:ConnectionString configuration.");
                }
                services.AddDbContextFactory<PrivatekonomyContext>(options =>
                {
                    var serverVersion = ServerVersion.AutoDetect(storageSettings.ConnectionString);
                    options.UseMySql(storageSettings.ConnectionString, serverVersion);
                });
                break;

            case "jsonfile":
                // JsonFile uses InMemory with persistence layer
                services.AddSingleton<JsonIdentityPersistenceService>();
                services.AddDbContextFactory<PrivatekonomyContext>((provider, options) =>
                    options.UseInMemoryDatabase("PrivatekonomyDb")
                        .AddInterceptors(provider.GetRequiredService<JsonIdentityPersistenceService>()));
                services.AddSingleton<IDataPersistenceService, JsonFilePersistenceService>();
                services.AddHostedService<JsonFilePersistenceHostedService>();
                break;

            case "inmemory":
                services.AddDbContextFactory<PrivatekonomyContext>(options =>
                    options.UseInMemoryDatabase("PrivatekonomyDb"));
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown storage provider '{storageSettings.Provider}'. Check Storage:Provider configuration.");
        }
        
        return services;
    }
}
