namespace Privatekonomi.Core.Services.DataMigration;

public sealed class DataMigrationRequest
{
    public string SourceProvider { get; set; } = string.Empty;
    public string SourceConnectionString { get; set; } = string.Empty;
    public string TargetProvider { get; set; } = string.Empty;
    public string TargetConnectionString { get; set; } = string.Empty;
}

public sealed class DataMigrationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string SourceProvider { get; init; } = string.Empty;
    public string TargetProvider { get; init; } = string.Empty;
    public Dictionary<string, int> MigratedCounts { get; init; } = new();
}
