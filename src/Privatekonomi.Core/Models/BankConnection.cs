namespace Privatekonomi.Core.Models;

/// <summary>
/// Represents a connection to a bank API for automatic import
/// </summary>
public class BankConnection
{
    public int BankConnectionId { get; set; }
    public int BankSourceId { get; set; }
    public BankSource? BankSource { get; set; }
    
    /// <summary>
    /// Type of API integration (e.g., "PSD2", "Proprietary", "OpenBanking")
    /// </summary>
    public string ApiType { get; set; } = "PSD2";
    
    /// <summary>
    /// External account ID from the bank API
    /// </summary>
    public string? ExternalAccountId { get; set; }
    
    /// <summary>
    /// OAuth2 access token (encrypted in production)
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// OAuth2 refresh token (encrypted in production)
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }
    
    /// <summary>
    /// Last time transactions were synced
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
    
    /// <summary>
    /// Whether auto-sync is enabled
    /// </summary>
    public bool AutoSyncEnabled { get; set; }
    
    /// <summary>
    /// Connection status
    /// </summary>
    public string Status { get; set; } = "Active"; // Active, Expired, Error
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
