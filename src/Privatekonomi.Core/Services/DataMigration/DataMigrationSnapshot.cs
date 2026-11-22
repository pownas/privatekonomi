using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace Privatekonomi.Core.Services.DataMigration;

public sealed class DataMigrationSnapshot
{
    public const string FileName = "privatekonomi-migration.json";

    public int Version { get; set; } = 1;
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public Dictionary<string, List<Dictionary<string, JsonElement>>> Entities { get; set; } = new();

    public void ValidateIntegrity(JsonSerializerOptions jsonOptions)
    {
        if (Version != 1)
            throw new InvalidOperationException(
                $"JSON-snapshot-version {Version} stöds inte. Förväntad version är 1.");
        if (string.IsNullOrWhiteSpace(ChecksumSha256))
            throw new InvalidOperationException(
                "JSON-snapshot saknar integritetskontroll (ChecksumSha256).");

        var entityBytes = JsonSerializer.SerializeToUtf8Bytes(Entities, jsonOptions);
        var expected = Convert.ToHexString(SHA256.HashData(entityBytes)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(ChecksumSha256.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(expected)))
            throw new InvalidOperationException(
                "JSON-snapshotets integritetskontroll misslyckades. Filen kan vara ändrad eller skadad.");
    }
}
