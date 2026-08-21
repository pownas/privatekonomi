using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for managing bank API connections
/// </summary>
public class BankConnectionService : IBankConnectionService
{
    private readonly PrivatekonomyContext _context;
    private readonly Dictionary<string, IBankApiService> _bankApiServices;
    private readonly IAuditLogService? _auditLogService;
    private readonly ITokenEncryptionService? _tokenEncryptionService;
    private readonly ILogger<BankConnectionService> _logger;

    public BankConnectionService(
        PrivatekonomyContext context,
        IEnumerable<IBankApiService> bankApiServices,
        ILogger<BankConnectionService> logger,
        IAuditLogService? auditLogService = null,
        ITokenEncryptionService? tokenEncryptionService = null)
    {
        _context = context;
        _bankApiServices = bankApiServices.ToDictionary(s => s.BankName, s => s, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
        _auditLogService = auditLogService;
        _tokenEncryptionService = tokenEncryptionService;
    }

    public async Task<List<BankConnection>> GetConnectionsAsync(int? bankSourceId = null)
    {
        var query = _context.BankConnections
            .Include(c => c.BankSource)
            .AsQueryable();

        if (bankSourceId.HasValue)
        {
            query = query.Where(c => c.BankSourceId == bankSourceId.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<BankConnection?> GetConnectionAsync(int connectionId)
    {
        var connection = await _context.BankConnections
            .Include(c => c.BankSource)
            .FirstOrDefaultAsync(c => c.BankConnectionId == connectionId);

        // Decrypt tokens when retrieving
        if (connection != null && _tokenEncryptionService != null)
        {
            if (!string.IsNullOrEmpty(connection.AccessToken))
                connection.AccessToken = _tokenEncryptionService.Decrypt(connection.AccessToken);
            
            if (!string.IsNullOrEmpty(connection.RefreshToken))
                connection.RefreshToken = _tokenEncryptionService.Decrypt(connection.RefreshToken);
        }

        return connection;
    }

    public async Task<BankConnection> CreateConnectionAsync(BankConnection connection)
    {
        // Encrypt tokens before saving
        if (_tokenEncryptionService != null)
        {
            if (!string.IsNullOrEmpty(connection.AccessToken))
                connection.AccessToken = _tokenEncryptionService.Encrypt(connection.AccessToken);
            
            if (!string.IsNullOrEmpty(connection.RefreshToken))
                connection.RefreshToken = _tokenEncryptionService.Encrypt(connection.RefreshToken);
        }

        connection.CreatedAt = DateTime.UtcNow;
        _context.BankConnections.Add(connection);
        await _context.SaveChangesAsync();
        
        // Audit log
        await _auditLogService?.LogAsync(
            "BankConnectionCreated",
            "BankConnection",
            connection.BankConnectionId,
            $"Created connection for bank source {connection.BankSourceId}")!;
        
        _logger.LogInformation("Bank connection {Id} created for bank source {BankSourceId}", 
            connection.BankConnectionId, connection.BankSourceId);
        
        return connection;
    }

    public async Task<BankConnection> UpdateConnectionAsync(BankConnection connection)
    {
        // Encrypt tokens before saving if they were updated
        if (_tokenEncryptionService != null)
        {
            if (!string.IsNullOrEmpty(connection.AccessToken) && 
                !_tokenEncryptionService.IsEncrypted(connection.AccessToken))
                connection.AccessToken = _tokenEncryptionService.Encrypt(connection.AccessToken);
            
            if (!string.IsNullOrEmpty(connection.RefreshToken) && 
                !_tokenEncryptionService.IsEncrypted(connection.RefreshToken))
                connection.RefreshToken = _tokenEncryptionService.Encrypt(connection.RefreshToken);
        }

        connection.UpdatedAt = DateTime.UtcNow;
        _context.BankConnections.Update(connection);
        await _context.SaveChangesAsync();
        
        // Audit log
        await _auditLogService?.LogAsync(
            "BankConnectionUpdated",
            "BankConnection",
            connection.BankConnectionId,
            $"Updated connection for bank source {connection.BankSourceId}")!;
        
        _logger.LogInformation("Bank connection {Id} updated", connection.BankConnectionId);
        
        return connection;
    }

    public async Task DeleteConnectionAsync(int connectionId)
    {
        var connection = await _context.BankConnections.FindAsync(connectionId);
        if (connection != null)
        {
            _context.BankConnections.Remove(connection);
            await _context.SaveChangesAsync();
            
            // Audit log
            await _auditLogService?.LogAsync(
                "BankConnectionDeleted",
                "BankConnection",
                connectionId,
                $"Deleted connection for bank source {connection.BankSourceId}")!;
            
            _logger.LogInformation("Bank connection {Id} deleted", connectionId);
        }
    }

    public IBankApiService? GetBankApiService(string bankName)
    {
        return _bankApiServices.TryGetValue(bankName, out var service) ? service : null;
    }

    public List<string> GetAvailableBanks()
    {
        return _bankApiServices.Keys.ToList();
    }

    public async Task<BankApiImportResult> SyncTransactionsAsync(
        int connectionId,
        string accountId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool skipDuplicates = true)
    {
        var connection = await GetConnectionAsync(connectionId);
        if (connection == null)
            throw new InvalidOperationException($"Bank connection {connectionId} not found");

        if (connection.BankSource == null)
            throw new InvalidOperationException("Bank source not found for connection");

        var bankService = GetBankApiService(connection.BankSource.Name);
        if (bankService == null)
            throw new InvalidOperationException($"No API service available for {connection.BankSource.Name}");

        // Default date range: last 90 days
        var from = fromDate ?? (connection.LastSyncedAt ?? DateTime.Now.AddDays(-90));
        var to = toDate ?? DateTime.Now;

        return await bankService.ImportTransactionsAsync(connection, accountId, from, to, skipDuplicates);
    }
}
