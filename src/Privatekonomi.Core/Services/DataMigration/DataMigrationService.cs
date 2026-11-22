using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.DataMigration;

public sealed class DataMigrationService : IDataMigrationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<DataMigrationPreview> PreviewAsync(
        DataMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var sourceProvider = NormalizeProvider(request.SourceProvider);
            var targetProvider = NormalizeProvider(request.TargetProvider);
            ValidateProvider(sourceProvider);
            ValidateProvider(targetProvider);

            if (sourceProvider == "JSONFILE")
            {
                var counts = await ReadJsonSnapshotCountsAsync(request.SourceConnectionString, cancellationToken);
                return await PreviewJsonTargetAsync(request, targetProvider, counts, cancellationToken);
            }

            await using var source = CreateContext(sourceProvider, request.SourceConnectionString);
            if (sourceProvider == "SQLITE" && !SourceSqliteFileExists(request.SourceConnectionString))
                throw new InvalidOperationException("Källdatabasen (SQLite) finns inte.");
            if (!await source.Database.CanConnectAsync(cancellationToken))
                throw new InvalidOperationException($"Källans anslutning misslyckades ({sourceProvider}).");

            var sourceCounts = await CountEntitiesAsync(source, cancellationToken);
            if (targetProvider == "JSONFILE")
            {
                ValidateJsonTargetDirectory(request.TargetConnectionString);
                return new DataMigrationPreview
                {
                    CanMigrate = true,
                    Message = "Källan är tillgänglig och JSON-målet är tomt.",
                    SourceProvider = request.SourceProvider,
                    TargetProvider = request.TargetProvider,
                    SourceCounts = sourceCounts
                };
            }

            await using var target = CreateContext(targetProvider, request.TargetConnectionString);
            await target.Database.EnsureCreatedAsync(cancellationToken);
            await EnsureTargetIsEmptyAsync(target, cancellationToken);

            return new DataMigrationPreview
            {
                CanMigrate = true,
                Message = "Källan är tillgänglig och måldatabasen är tom.",
                SourceProvider = request.SourceProvider,
                TargetProvider = request.TargetProvider,
                SourceCounts = sourceCounts
            };
        }
        catch (Exception ex)
        {
            return new DataMigrationPreview
            {
                CanMigrate = false,
                Message = ex.Message,
                SourceProvider = request.SourceProvider,
                TargetProvider = request.TargetProvider
            };
        }
    }

    public async Task<DataMigrationResult> MigrateAsync(
        DataMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sourceProvider = NormalizeProvider(request.SourceProvider);
        var targetProvider = NormalizeProvider(request.TargetProvider);
        ValidateProvider(sourceProvider);
        ValidateProvider(targetProvider);

        try
        {
            if (sourceProvider == "JSONFILE")
            {
                if (targetProvider == "JSONFILE")
                    throw new InvalidOperationException("JSON till JSON stöds inte som direkt omkopiering; ange en relationsdatabas som mål.");

                await using var targetContext = CreateContext(targetProvider, request.TargetConnectionString);
                await targetContext.Database.EnsureCreatedAsync(cancellationToken);
                await EnsureTargetIsEmptyAsync(targetContext, cancellationToken);
                var counts = await ImportJsonSnapshotAsync(request.SourceConnectionString, targetContext, cancellationToken);
                var (verified, verificationSucceeded) =
                    await VerifyJsonImportAsync(request.SourceConnectionString, targetContext, cancellationToken);
                return Success(request, counts, verified, verificationSucceeded);
            }

            await using var source = CreateContext(sourceProvider, request.SourceConnectionString);
            if (sourceProvider == "SQLITE" && !SourceSqliteFileExists(request.SourceConnectionString))
                throw new InvalidOperationException("Källdatabasen (SQLite) finns inte.");
            if (!await source.Database.CanConnectAsync(cancellationToken))
                throw new InvalidOperationException($"Källans anslutning misslyckades ({sourceProvider}).");

            if (targetProvider == "JSONFILE")
            {
                var counts = await ExportJsonSnapshotAsync(request.TargetConnectionString, source, cancellationToken);
                return Success(request, counts);
            }

            await using var target = CreateContext(targetProvider, request.TargetConnectionString);
            await target.Database.EnsureCreatedAsync(cancellationToken);
            await EnsureTargetIsEmptyAsync(target, cancellationToken);

            var migratedCounts = await CopyBetweenRelationalContextsAsync(source, target, cancellationToken);
            var verifiedCounts = await VerifyRelationalMigrationAsync(source, target, cancellationToken);
            var relationalVerificationSucceeded = migratedCounts.Count == 0 || migratedCounts.All(pair =>
                verifiedCounts.TryGetValue(pair.Key, out var count) && count >= pair.Value);
            return Success(request, migratedCounts, verifiedCounts, relationalVerificationSucceeded);
        }
        catch (Exception ex)
        {
            return new DataMigrationResult
            {
                Success = false,
                Message = ex.Message,
                SourceProvider = request.SourceProvider,
                TargetProvider = request.TargetProvider
            };
        }
    }

    private static DataMigrationResult Success(DataMigrationRequest request, Dictionary<string, int> counts)
    {
        var total = counts.Values.Sum();
        return new DataMigrationResult
        {
            Success = true,
            Message = $"Migreringen slutfördes. {total} entiteter flyttades.",
            SourceProvider = request.SourceProvider,
            TargetProvider = request.TargetProvider,
            MigratedCounts = counts
        };
    }

    private static DataMigrationResult Success(
        DataMigrationRequest request,
        Dictionary<string, int> counts,
        Dictionary<string, int> verifiedCounts,
        bool verificationSucceeded = true)
    {
        var total = counts.Values.Sum();
        return new DataMigrationResult
        {
            Success = verificationSucceeded,
            Message = verificationSucceeded
                ? $"Migreringen slutfördes och verifierades. {total} entiteter flyttades."
                : $"Migreringen slutfördes men efterkontrollen hittade en avvikelse.",
            SourceProvider = request.SourceProvider,
            TargetProvider = request.TargetProvider,
            MigratedCounts = counts,
            VerifiedCounts = verifiedCounts,
            VerificationSucceeded = verificationSucceeded
        };
    }

    private static void ValidateProvider(string provider)
    {
        if (provider is not ("SQLITE" or "SQLSERVER" or "MYSQL" or "MARIADB" or "POSTGRESQL" or "JSONFILE"))
            throw new InvalidOperationException($"Lagringsprovider '{provider}' stöds inte för migrering.");
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException("Lagringsprovider saknas.");

        return provider.Trim().Replace(" ", string.Empty).ToUpperInvariant() switch
        {
            "SQLITE" => "SQLITE",
            "SQLSERVER" => "SQLSERVER",
            "SQL_SERVER" => "SQLSERVER",
            "MYSQL" => "MYSQL",
            "MARIADB" => "MARIADB",
            "POSTGRESQL" => "POSTGRESQL",
            "POSTGRES" => "POSTGRESQL",
            "JSONFILE" => "JSONFILE",
            "JSON" => "JSONFILE",
            _ => provider.Trim().ToUpperInvariant()
        };
    }

    private static PrivatekonomyContext CreateContext(string provider, string connectionString)
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>();
        switch (provider)
        {
            case "SQLITE":
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("Anslutningssträng för SQLite saknas.");
                options.UseSqlite(connectionString);
                break;
            case "SQLSERVER":
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("Anslutningssträng för SQL Server saknas.");
                options.UseSqlServer(connectionString);
                break;
            case "MYSQL":
            case "MARIADB":
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("Anslutningssträng för MySQL/MariaDB saknas.");
                var serverVersion = ServerVersion.AutoDetect(connectionString);
                options.UseMySql(connectionString, serverVersion);
                break;
            case "POSTGRESQL":
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("Anslutningssträng för PostgreSQL saknas.");
                options.UseNpgsql(connectionString);
                break;
            default:
                throw new InvalidOperationException($"Provider '{provider}' kan inte användas som EF-relationsdatabas.");
        }

        return new PrivatekonomyContext(options.Options);
    }

    private static bool SourceSqliteFileExists(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
            if (string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.IsNullOrWhiteSpace(builder.DataSource))
                return false;
            return File.Exists(builder.DataSource);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<Dictionary<string, int>> CountEntitiesAsync(
        PrivatekonomyContext context,
        CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, int>();
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (entityType.FindOwnership() != null)
                continue;

            var count = await GetEntitySet(context, entityType.ClrType)
                .Cast<object>()
                .CountAsync(cancellationToken);
            if (count > 0)
                counts[entityType.ClrType.Name] = count;
        }
        return counts;
    }

    private static async Task<Dictionary<string, int>> VerifyRelationalMigrationAsync(
        PrivatekonomyContext source,
        PrivatekonomyContext target,
        CancellationToken cancellationToken)
    {
        var sourceCounts = await CountEntitiesAsync(source, cancellationToken);
        var targetCounts = await CountEntitiesAsync(target, cancellationToken);
        var verified = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var pair in sourceCounts)
        {
            if (targetCounts.TryGetValue(pair.Key, out var targetCount))
                verified[pair.Key] = targetCount;
        }

        return verified;
    }

    private static async Task<(Dictionary<string, int> Verified, bool Succeeded)> VerifyJsonImportAsync(
        string sourceDirectory,
        PrivatekonomyContext target,
        CancellationToken cancellationToken)
    {
        var snapshotCounts = await ReadJsonSnapshotCountsAsync(sourceDirectory, cancellationToken);
        var targetCounts = await CountEntitiesAsync(target, cancellationToken);
        var verified = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var pair in snapshotCounts)
        {
            var targetTypeName = pair.Key.Split('.').Last();
            if (targetCounts.TryGetValue(targetTypeName, out var targetCount))
                verified[targetTypeName] = targetCount;
        }

        var succeeded = snapshotCounts.All(pair =>
        {
            var targetTypeName = pair.Key.Split('.').Last();
            return targetCounts.TryGetValue(targetTypeName, out var targetCount) &&
                   targetCount >= pair.Value;
        });
        return (verified, succeeded);
    }

    private static void ValidateJsonTargetDirectory(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new InvalidOperationException("Målmapp för JSON saknas.");

        Directory.CreateDirectory(targetDirectory);
        if (Directory.EnumerateFileSystemEntries(targetDirectory).Any())
            throw new InvalidOperationException(
                "Målmappen för JSON innehåller redan filer; befintliga data skrivs inte över.");
    }

    private static async Task<Dictionary<string, int>> ReadJsonSnapshotCountsAsync(
        string sourceDirectory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
            throw new InvalidOperationException("Källmapp för JSON saknas.");

        var snapshotPath = Path.Combine(sourceDirectory, DataMigrationSnapshot.FileName);
        if (!File.Exists(snapshotPath))
            throw new InvalidOperationException(
                $"JSON-snapshot saknas ({DataMigrationSnapshot.FileName}).");

        await using var stream = File.OpenRead(snapshotPath);
        var snapshot = await JsonSerializer.DeserializeAsync<DataMigrationSnapshot>(
            stream,
            JsonOptions,
            cancellationToken) ?? throw new InvalidOperationException("JSON-snapshot kunde inte läsas.");

        ValidateSnapshotIntegrity(snapshot);
        return snapshot.Entities.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
    }

    private static void ValidateSnapshotIntegrity(DataMigrationSnapshot snapshot)
    {
        if (snapshot.Version != 1)
            throw new InvalidOperationException(
                $"JSON-snapshot-version {snapshot.Version} stöds inte. Förväntad version är 1.");
        if (string.IsNullOrWhiteSpace(snapshot.ChecksumSha256))
            throw new InvalidOperationException(
                "JSON-snapshot saknar integritetskontroll (ChecksumSha256).");

        var entityBytes = JsonSerializer.SerializeToUtf8Bytes(snapshot.Entities, JsonOptions);
        var expected = Convert.ToHexString(SHA256.HashData(entityBytes)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(snapshot.ChecksumSha256.ToLowerInvariant()),
                System.Text.Encoding.UTF8.GetBytes(expected)))
            throw new InvalidOperationException(
                "JSON-snapshotets integritetskontroll misslyckades. Filen kan vara ändrad eller skadad.");
    }

    private static async Task<DataMigrationPreview> PreviewJsonTargetAsync(
        DataMigrationRequest request,
        string targetProvider,
        Dictionary<string, int> counts,
        CancellationToken cancellationToken)
    {
        if (targetProvider == "JSONFILE")
            throw new InvalidOperationException("JSON till JSON stöds inte som direkt omkopiering.");

        await using var target = CreateContext(targetProvider, request.TargetConnectionString);
        await target.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureTargetIsEmptyAsync(target, cancellationToken);

        return new DataMigrationPreview
        {
            CanMigrate = true,
            Message = "JSON-snapshotet är giltigt och måldatabasen är tom.",
            SourceProvider = request.SourceProvider,
            TargetProvider = request.TargetProvider,
            SourceCounts = counts
        };
    }

    private static async Task EnsureTargetIsEmptyAsync(PrivatekonomyContext target, CancellationToken cancellationToken)
    {
        foreach (var entityType in target.Model.GetEntityTypes())
        {
            if (entityType.FindOwnership() != null || entityType.ClrType == null)
                continue;

            var set = GetEntitySet(target, entityType.ClrType);
            var entities = await set.Cast<object>().ToListAsync(cancellationToken);
            if (entityType.ClrType.Name == nameof(Category))
            {
                var customCategoryProperty = entityType.FindProperty("IsSystemCategory");
                if (customCategoryProperty?.PropertyInfo != null &&
                    entities.Any(entity =>
                        customCategoryProperty.PropertyInfo!.GetValue(entity) is false))
                {
                    throw new InvalidOperationException(
                        "Måldatabasen är inte tom (Category). Migreringen avbröts för att befintliga data inte ska skrivas över.");
                }
                continue;
            }

            // Categories and bank sources are seeded as reference data by EnsureCreated.
            if (entityType.ClrType.Name == nameof(BankSource))
                continue;

            if (entities.Count > 0)
                throw new InvalidOperationException(
                    $"Måldatabasen är inte tom ({entityType.ClrType.Name}). Migreringen avbröts för att befintliga data inte ska skrivas över.");
        }
    }

    private static async Task<Dictionary<string, int>> CopyBetweenRelationalContextsAsync(
        PrivatekonomyContext source,
        PrivatekonomyContext target,
        CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, int>();
        var allEntityTypes = source.Model.GetEntityTypes()
            .Where(entityType => entityType.FindOwnership() == null)
            .Distinct()
            .ToList();
        var identityTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var transaction = await target.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var entityType in allEntityTypes)
            {
                var sourceSet = GetEntitySet(source, entityType.ClrType);
                var sourceEntities = await sourceSet.Cast<object>().ToListAsync(cancellationToken);
                if (sourceEntities.Count == 0)
                    continue;

                var existingTargetEntities = await GetEntitySet(target, entityType.ClrType)
                    .Cast<object>()
                    .ToListAsync(cancellationToken);

                var clones = sourceEntities
                    .Select(entity => CloneEntity(entity, entityType))
                    .Where(clone => !HasMatchingPrimaryKey(entityType, clone, existingTargetEntities))
                    .ToList();
                if (clones.Count == 0)
                    continue;

                await target.AddRangeAsync(clones, cancellationToken);
                if (target.Database.IsSqlServer())
                {
                    var tableName = entityType.GetTableName();
                    if (!string.IsNullOrWhiteSpace(tableName))
                    {
                        var schemaName = entityType.GetSchema();
                        identityTables.Add(string.IsNullOrWhiteSpace(schemaName)
                            ? $"[{tableName.Replace("]", "]]")}]"
                            : $"[{schemaName.Replace("]", "]]")}].[{tableName.Replace("]", "]]")}]");
                    }
                }
                counts[entityType.ClrType.Name] = clones.Count;
            }

            var enabledIdentityTables = new List<string>();
            if (target.Database.IsSqlServer())
            {
                foreach (var tableName in identityTables)
                {
                    try
                    {
                        await target.Database.ExecuteSqlInterpolatedAsync(
                            $"SET IDENTITY_INSERT {tableName} ON",
                            cancellationToken);
                        enabledIdentityTables.Add(tableName);
                    }
                    catch (Microsoft.Data.SqlClient.SqlException)
                    {
                        // Tabellen har ingen identity-kolumn eller ingår inte i migreringen.
                    }
                }
            }

            try
            {
                await target.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                foreach (var tableName in enabledIdentityTables)
                {
                    await target.Database.ExecuteSqlInterpolatedAsync(
                        $"SET IDENTITY_INSERT {tableName} OFF",
                        cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return counts;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool HasMatchingPrimaryKey(
        IEntityType entityType,
        object candidate,
        IReadOnlyCollection<object> existingEntities)
    {
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey == null)
            return false;

        return existingEntities.Any(existing =>
            primaryKey.Properties.All(property =>
            {
                var propertyInfo = property.PropertyInfo;
                return propertyInfo != null &&
                       Equals(propertyInfo.GetValue(candidate), propertyInfo.GetValue(existing));
            }));
    }

    private static object CloneEntity(object source, IEntityType entityType)
    {
        var target = Activator.CreateInstance(entityType.ClrType)
                     ?? throw new InvalidOperationException($"Det går inte att skapa entitetstypen {entityType.ClrType.Name}.");

        foreach (var property in entityType.GetProperties())
        {
            var propertyInfo = property.PropertyInfo;
            if (propertyInfo == null || !propertyInfo.CanRead || !propertyInfo.CanWrite)
                continue;

            propertyInfo.SetValue(target, propertyInfo.GetValue(source));
        }

        return target;
    }

    private static IQueryable GetEntitySet(PrivatekonomyContext context, Type entityType)
    {
        var setMethod = typeof(DbContext).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method =>
                method.Name == "Set" &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 0);

        return (IQueryable)(setMethod.MakeGenericMethod(entityType).Invoke(context, null)
                            ?? throw new InvalidOperationException(
                                $"Det går inte att öppna DbSet för {entityType.Name}."));
    }

    private static async Task<Dictionary<string, int>> ExportJsonSnapshotAsync(
        string targetDirectory,
        PrivatekonomyContext source,
        CancellationToken cancellationToken)
    {
        ValidateJsonTargetDirectory(targetDirectory);

        var snapshot = new DataMigrationSnapshot();
        foreach (var entityType in source.Model.GetEntityTypes())
        {
            if (entityType.FindOwnership() != null)
                continue;

            var entities = await GetEntitySet(source, entityType.ClrType).Cast<object>().ToListAsync(cancellationToken);
            if (entities.Count == 0)
                continue;

            var records = new List<Dictionary<string, JsonElement>>();
            foreach (var entity in entities)
            {
                var record = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in entityType.GetProperties())
                {
                    var propertyInfo = property.PropertyInfo;
                    if (propertyInfo == null || !propertyInfo.CanRead)
                        continue;

                    var value = propertyInfo.GetValue(entity);
                    record[property.Name] = value == null
                        ? JsonSerializer.SerializeToElement<object?>(null)
                        : JsonSerializer.SerializeToElement(value, property.ClrType, JsonOptions);
                }
                records.Add(record);
            }
            snapshot.Entities[entityType.ClrType.FullName ?? entityType.ClrType.Name] = records;
        }

        var entityBytes = JsonSerializer.SerializeToUtf8Bytes(snapshot.Entities, JsonOptions);
        snapshot.ChecksumSha256 = Convert.ToHexString(SHA256.HashData(entityBytes)).ToLowerInvariant();

        var snapshotPath = Path.Combine(targetDirectory, DataMigrationSnapshot.FileName);
        await using var stream = new FileStream(snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        return snapshot.Entities.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count);
    }

    private static async Task<Dictionary<string, int>> ImportJsonSnapshotAsync(
        string sourceDirectory,
        PrivatekonomyContext target,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
            throw new InvalidOperationException("Källmapp för JSON saknas.");

        var snapshotPath = Path.Combine(sourceDirectory, DataMigrationSnapshot.FileName);
        if (!File.Exists(snapshotPath))
            throw new InvalidOperationException(
                $"JSON-snapshot saknas ({DataMigrationSnapshot.FileName}). Befintligt äldre JSON-format stöds endast via applikationens JSON-persistens.");

        await using var stream = File.OpenRead(snapshotPath);
        var snapshot = await JsonSerializer.DeserializeAsync<DataMigrationSnapshot>(stream, JsonOptions, cancellationToken)
                       ?? throw new InvalidOperationException("JSON-snapshot kunde inte läsas.");
        ValidateSnapshotIntegrity(snapshot);

        var counts = new Dictionary<string, int>();
        await using var transaction = await target.Database.BeginTransactionAsync(cancellationToken);
        var enabledIdentityTables = new List<string>();
        try
        {
            foreach (var entityType in target.Model.GetEntityTypes())
            {
                if (entityType.FindOwnership() != null)
                    continue;

                var typeName = entityType.ClrType.FullName ?? entityType.ClrType.Name;
                if (!snapshot.Entities.TryGetValue(typeName, out var records))
                    continue;

                var existingEntities = await GetEntitySet(target, entityType.ClrType)
                    .Cast<object>()
                    .ToListAsync(cancellationToken);

                foreach (var record in records)
                {
                    var entity = Activator.CreateInstance(entityType.ClrType)
                                 ?? throw new InvalidOperationException($"Det går inte att skapa entitetstypen {entityType.ClrType.Name}.");

                    foreach (var property in entityType.GetProperties())
                    {
                        var propertyInfo = property.PropertyInfo;
                        if (propertyInfo == null || !propertyInfo.CanWrite ||
                            !record.TryGetValue(property.Name, out var jsonElement))
                            continue;

                        if (jsonElement.ValueKind == JsonValueKind.Null)
                        {
                            propertyInfo.SetValue(entity, null);
                            continue;
                        }

                        var value = jsonElement.Deserialize(property.ClrType, JsonOptions);
                        propertyInfo.SetValue(entity, value);
                    }

                    if (HasMatchingPrimaryKey(entityType, entity, existingEntities))
                        continue;

                    await target.AddAsync(entity, cancellationToken);
                    existingEntities.Add(entity);
                }

                counts[entityType.ClrType.Name] = records.Count;
            }

            if (target.Database.IsSqlServer())
            {
                foreach (var entityType in target.Model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    if (string.IsNullOrWhiteSpace(tableName) ||
                        !snapshot.Entities.ContainsKey(entityType.ClrType.FullName ?? entityType.ClrType.Name))
                        continue;

                    var schemaName = entityType.GetSchema();
                    var sqlTableName = string.IsNullOrWhiteSpace(schemaName)
                        ? $"[{tableName.Replace("]", "]]")}]"
                        : $"[{schemaName.Replace("]", "]]")}].[{tableName.Replace("]", "]]")}]";
                    try
                    {
                        await target.Database.ExecuteSqlInterpolatedAsync(
                            $"SET IDENTITY_INSERT {sqlTableName} ON",
                            cancellationToken);
                        enabledIdentityTables.Add(sqlTableName);
                    }
                    catch (Microsoft.Data.SqlClient.SqlException)
                    {
                    }
                }
            }

            await target.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return counts;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            foreach (var tableName in enabledIdentityTables)
            {
                await target.Database.ExecuteSqlInterpolatedAsync(
                    $"SET IDENTITY_INSERT {tableName} OFF",
                    cancellationToken);
            }
        }
    }
}
