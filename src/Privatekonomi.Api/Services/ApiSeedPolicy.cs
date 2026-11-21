using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Api.Services;

/// <summary>Controls initialization where API and Web may share a database.</summary>
public static class ApiSeedPolicy
{
    public const string ProfileKey = "ApiData:Profile";
    public const string SqliteConnectionKey = "ApiData:SqliteConnectionString";
    public const string SqlServerConnectionKey = "ApiData:SqlServerConnectionString";

    /// <summary>Applies an API data profile before EF storage services are registered.</summary>
    public static void ApplyDataProfile(IConfiguration configuration, IHostEnvironment environment)
    {
        var profile = configuration[ProfileKey]?.Trim();
        if (string.IsNullOrWhiteSpace(profile))
            return;

        switch (profile.ToLowerInvariant())
        {
            case "mock":
                configuration["Storage:Provider"] = "InMemory";
                configuration["Storage:ConnectionString"] = string.Empty;
                break;
            case "sqlite":
                configuration["Storage:Provider"] = "Sqlite";
                var sqliteConnection = configuration[SqliteConnectionKey];
                if (!string.IsNullOrWhiteSpace(sqliteConnection))
                    configuration["Storage:ConnectionString"] = sqliteConnection;
                break;
            case "sqlserver":
                configuration["Storage:Provider"] = "SqlServer";
                var sqlServerConnection = configuration[SqlServerConnectionKey];
                if (string.IsNullOrWhiteSpace(sqlServerConnection))
                    throw new InvalidOperationException("SQL Server API profile requires ApiData:SqlServerConnectionString.");
                configuration["Storage:ConnectionString"] = sqlServerConnection;
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown API data profile '{profile}'. Supported profiles: Mock, Sqlite, SqlServer.");
        }

        ValidateDataProfile(profile, configuration, environment);
    }

    private static void ValidateDataProfile(string profile, IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["Storage:Provider"];
        var seedTestData = configuration.GetValue<bool>("Storage:SeedTestData");

        if (environment.IsProduction() && seedTestData)
            throw new InvalidOperationException("Test data seeding is not allowed in Production.");

        if (string.Equals(profile, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            if (environment.IsProduction())
                throw new InvalidOperationException("Mock API data is not allowed in Production.");
            if (!string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Mock API profile requires InMemory storage.");
        }
        else if (string.Equals(profile, "Sqlite", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Sqlite API profile requires Sqlite storage.");
        }
        else if (string.Equals(profile, "SqlServer", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SqlServer API profile requires SQL Server storage.");
        }
    }

    /// <summary>Returns whether the API can seed reference data without an uncoordinated server database write.</summary>
    public static bool CanSeedReferenceData(PrivatekonomyContext context) =>
        !context.Database.IsRelational() || context.Database.IsSqlite();

    /// <summary>Rejects demo initialization on server databases shared with another process.</summary>
    public static void ValidateDemoInitialization(PrivatekonomyContext context, bool isDevelopment, bool demoEnabled)
    {
        if (!CanSeedReferenceData(context) && isDevelopment && demoEnabled)
            throw new InvalidOperationException("API demo initialization is not supported on shared relational storage without cross-process coordination.");
    }

    /// <summary>Allows sample data only in a newly created, non-persistent API in-memory database.</summary>
    public static void ValidateTestDataInitialization(
        PrivatekonomyContext context, string provider, bool isDevelopment, bool demoEnabled, bool seedTestData, bool databaseCreated)
    {
        if (isDevelopment && demoEnabled && seedTestData &&
            (!string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase) ||
             !context.Database.IsInMemory() || !databaseCreated))
            throw new InvalidOperationException("API test data requires a newly created, isolated InMemory database.");
    }
}
