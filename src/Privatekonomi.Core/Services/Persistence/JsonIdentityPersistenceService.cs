using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.Persistence;

/// <summary>
/// Persists Identity changes immediately when using JSON storage.
/// </summary>
public sealed class JsonIdentityPersistenceService(
    IOptions<StorageSettings> settings,
    IHostApplicationLifetime? applicationLifetime = null) : SaveChangesInterceptor
{
    private readonly string _path = Path.Combine(
        string.IsNullOrEmpty(settings.Value.ConnectionString) ? "data" : settings.Value.ConnectionString,
        "identity.json");
    private readonly ConditionalWeakTable<DbContext, object> _pending = new();
    private readonly object _saveLock = new();
    private Exception? _persistenceFailure;

    /// <summary>
    /// Loads accounts before seeding or accepting requests. Existing accounts are never replaced.
    /// </summary>
    public async Task LoadAsync(PrivatekonomyContext context)
    {
        if (!File.Exists(_path) || await context.Users.AnyAsync() || await context.Roles.AnyAsync())
            return;

        var snapshot = JsonSerializer.Deserialize<IdentitySnapshot>(await File.ReadAllTextAsync(_path))
            ?? throw new InvalidDataException("Identity data is invalid.");
        if (snapshot.Users is null || snapshot.Roles is null || snapshot.UserRoles is null ||
            snapshot.UserClaims is null || snapshot.UserLogins is null || snapshot.UserTokens is null ||
            snapshot.RoleClaims is null || snapshot.UserPasskeys is null)
            throw new InvalidDataException("Identity collections must not be null.");
        context.Users.AddRange(snapshot.Users);
        context.Roles.AddRange(snapshot.Roles);
        context.UserRoles.AddRange(snapshot.UserRoles);
        context.UserClaims.AddRange(snapshot.UserClaims);
        context.UserLogins.AddRange(snapshot.UserLogins);
        context.UserTokens.AddRange(snapshot.UserTokens);
        context.RoleClaims.AddRange(snapshot.RoleClaims);
        context.Set<IdentityUserPasskey<string>>().AddRange(snapshot.UserPasskeys);
        await context.SaveChangesAsync();
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        TrackChanges(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        TrackChanges(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void TrackChanges(DbContext? context)
    {
        ThrowIfPersistenceFailed();
        if (context is not null && context.ChangeTracker.Entries().Any(entry =>
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                (entry.Entity is ApplicationUser || entry.Metadata.ClrType.Namespace == typeof(IdentityRole).Namespace)))
            _pending.GetValue(context, _ => new object());
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Persist(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Persist(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Persist(DbContext? dbContext)
    {
        if (dbContext is not PrivatekonomyContext context || !_pending.Remove(context))
            return;

        lock (_saveLock)
        {
            ThrowIfPersistenceFailed();
            var temporaryPath = _path + ".tmp";
            try
            {
                var snapshot = new IdentitySnapshot
                {
                    Users = context.Users.AsNoTracking().ToList(),
                    Roles = context.Roles.AsNoTracking().ToList(),
                    UserRoles = context.UserRoles.AsNoTracking().ToList(),
                    UserClaims = context.UserClaims.AsNoTracking().ToList(),
                    UserLogins = context.UserLogins.AsNoTracking().ToList(),
                    UserTokens = context.UserTokens.AsNoTracking().ToList(),
                    RoleClaims = context.RoleClaims.AsNoTracking().ToList(),
                    UserPasskeys = context.Set<IdentityUserPasskey<string>>().AsNoTracking().ToList()
                };
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
                if (!OperatingSystem.IsWindows())
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                using (var stream = new FileStream(temporaryPath, options))
                {
                    JsonSerializer.Serialize(stream, snapshot);
                    stream.Flush(flushToDisk: true);
                }

                // Replace the complete snapshot atomically, never a partially written account file.
                File.Move(temporaryPath, _path, overwrite: true);
            }
            catch (Exception exception)
            {
                // InMemory has already committed. Stop rather than serve accounts that are not durable.
                Volatile.Write(ref _persistenceFailure, exception);
                applicationLifetime?.StopApplication();
                throw new InvalidOperationException(
                    "Identity changes could not be persisted. The application must stop and restart after storage is repaired.",
                    exception);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }

    private void ThrowIfPersistenceFailed()
    {
        if (Volatile.Read(ref _persistenceFailure) is { } failure)
            throw new InvalidOperationException("Identity storage is unavailable. Restart after repairing storage.", failure);
    }

    private sealed class IdentitySnapshot
    {
        public required List<ApplicationUser> Users { get; set; }
        public required List<IdentityRole> Roles { get; set; }
        public required List<IdentityUserRole<string>> UserRoles { get; set; }
        public required List<IdentityUserClaim<string>> UserClaims { get; set; }
        public required List<IdentityUserLogin<string>> UserLogins { get; set; }
        public required List<IdentityUserToken<string>> UserTokens { get; set; }
        public required List<IdentityRoleClaim<string>> RoleClaims { get; set; }
        public required List<IdentityUserPasskey<string>> UserPasskeys { get; set; }
    }
}
