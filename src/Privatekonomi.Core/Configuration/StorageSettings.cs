namespace Privatekonomi.Core.Configuration;

/// <summary>
/// Configuration settings for data storage
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Storage provider type (InMemory, Sqlite, SqlServer, MySQL, MariaDB, JsonFile)
    /// </summary>
    public string Provider { get; set; } = "InMemory";
    
    /// <summary>
    /// Connection string or file path for the storage provider
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to seed test data (only for development)
    /// </summary>
    public bool SeedTestData { get; set; }
}
