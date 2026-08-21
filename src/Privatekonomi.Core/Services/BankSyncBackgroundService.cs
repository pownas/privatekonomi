using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Background service for automatic bank transaction synchronization
/// </summary>
public class BankSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BankSyncBackgroundService> _logger;
    private readonly BankSyncSettings _settings;

    public BankSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BankSyncBackgroundService> logger,
        IOptions<BankSyncSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Bank sync background service is disabled");
            return;
        }

        _logger.LogInformation("Bank sync background service started with interval: {Interval} minutes", 
            _settings.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllConnectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic bank synchronization");
            }

            // Wait for the configured interval
            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }
    }

    private async Task SyncAllConnectionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var bankConnectionService = scope.ServiceProvider.GetRequiredService<IBankConnectionService>();

        _logger.LogInformation("Starting automatic bank synchronization");

        try
        {
            // Get all active connections with auto-sync enabled
            var connections = await bankConnectionService.GetConnectionsAsync();
            var autoSyncConnections = connections
                .Where(c => c.AutoSyncEnabled && c.Status == "Active")
                .ToList();

            if (!autoSyncConnections.Any())
            {
                _logger.LogDebug("No active connections with auto-sync enabled");
                return;
            }

            _logger.LogInformation("Found {Count} connections to sync", autoSyncConnections.Count);

            foreach (var connection in autoSyncConnections)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await SyncConnectionAsync(connection, bankConnectionService, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing connection {ConnectionId} for bank {BankName}",
                        connection.BankConnectionId,
                        connection.BankSource?.Name ?? "Unknown");
                }
            }

            _logger.LogInformation("Completed automatic bank synchronization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync all connections");
        }
    }

    private async Task SyncConnectionAsync(
        Core.Models.BankConnection connection,
        IBankConnectionService bankConnectionService,
        CancellationToken cancellationToken)
    {
        if (connection.BankSource == null)
        {
            _logger.LogWarning("Connection {ConnectionId} has no bank source", connection.BankConnectionId);
            return;
        }

        _logger.LogInformation("Syncing connection {ConnectionId} for {BankName}",
            connection.BankConnectionId,
            connection.BankSource.Name);

        try
        {
            var bankApiService = bankConnectionService.GetBankApiService(connection.BankSource.Name);
            if (bankApiService == null)
            {
                _logger.LogWarning("No API service found for {BankName}", connection.BankSource.Name);
                return;
            }

            // Get accounts for this connection
            var accounts = await bankApiService.GetAccountsAsync(connection);
            
            if (!accounts.Any())
            {
                _logger.LogWarning("No accounts found for connection {ConnectionId}", connection.BankConnectionId);
                return;
            }

            // Sync transactions for each account
            foreach (var account in accounts)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Sync last 7 days by default
                    var fromDate = DateTime.Now.AddDays(-7);
                    var toDate = DateTime.Now;

                    var result = await bankConnectionService.SyncTransactionsAsync(
                        connection.BankConnectionId,
                        account.AccountId,
                        fromDate,
                        toDate,
                        skipDuplicates: true);

                    _logger.LogInformation(
                        "Synced {ImportedCount} transactions for account {AccountId} (skipped {DuplicateCount} duplicates)",
                        result.ImportedCount,
                        account.AccountId,
                        result.DuplicateCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing account {AccountId}", account.AccountId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing connection {ConnectionId}", connection.BankConnectionId);
            
            // Update connection status if there's a persistent error
            connection.Status = "Error";
            connection.UpdatedAt = DateTime.UtcNow;
            await bankConnectionService.UpdateConnectionAsync(connection);
        }
    }
}

/// <summary>
/// Configuration settings for bank synchronization
/// </summary>
public class BankSyncSettings
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60; // Default: sync every hour
}
