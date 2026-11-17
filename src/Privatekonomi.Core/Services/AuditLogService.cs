using Microsoft.Extensions.Logging;

using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for logging security-critical operations
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly PrivatekonomyContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(PrivatekonomyContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, int? entityId = null, string? details = null, string? userId = null, string? ipAddress = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                UserId = userId,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: {Action} on {EntityType} {EntityId} by {UserId} from {IpAddress}",
                action, entityType, entityId, userId ?? "Unknown", ipAddress ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
            // Don't throw - we don't want audit logging failures to break the application
        }
    }

    public async Task LogTransactionUpdateAsync(Transaction before, Transaction after, string? userId = null, string? ipAddress = null)
    {
        try
        {
            var changes = new List<string>();

            if (before.Amount != after.Amount)
                changes.Add($"Amount: {before.Amount} -> {after.Amount}");
            
            if (before.Date != after.Date)
                changes.Add($"Date: {before.Date:yyyy-MM-dd} -> {after.Date:yyyy-MM-dd}");
            
            if (before.Description != after.Description)
                changes.Add($"Description: '{before.Description}' -> '{after.Description}'");
            
            if (before.Payee != after.Payee)
                changes.Add($"Payee: '{before.Payee ?? "null"}' -> '{after.Payee ?? "null"}'");
            
            if (before.Notes != after.Notes)
                changes.Add($"Notes: '{before.Notes ?? "null"}' -> '{after.Notes ?? "null"}'");
            
            if (before.Tags != after.Tags)
                changes.Add($"Tags: '{before.Tags ?? "null"}' -> '{after.Tags ?? "null"}'");

            var details = changes.Any() 
                ? string.Join("; ", changes) 
                : "No changes detected";

            await LogAsync(
                "TransactionUpdated",
                "Transaction",
                after.TransactionId,
                details,
                userId,
                ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for transaction update");
            // Don't throw - we don't want audit logging failures to break the application
        }
    }
}
