using System.Text.Json;

namespace Privatekonomi.Core.Services.DataMigration;

public sealed class DataMigrationSnapshot
{
    public const string FileName = "privatekonomi-migration.json";

    public int Version { get; set; } = 1;
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, List<Dictionary<string, JsonElement>>> Entities { get; set; } = new();
}
