using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Api.Services;

/// <summary>Controls initialization where API and Web may share a database.</summary>
public static class ApiSeedPolicy
{
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
