using System.Reflection;

namespace Privatekonomi.Api.Models;

/// <summary>Public build information for the API.</summary>
public sealed record ApiInfoResponse(string? Version, string? BuildNumber, string? Sha, string? Environment, string? StorageProvider)
{
    /// <summary>Reads the metadata embedded in the API assembly.</summary>
    public static ApiInfoResponse FromAssembly(Assembly assembly, string? environmentName, string? storageProvider)
    {
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
        string? GetMetadata(string key) => metadata.FirstOrDefault(item => item.Key == key)?.Value;

        return FromMetadata(
            GetMetadata("AppVersion"),
            GetMetadata("BuildNumber"),
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            GetMetadata("GitHubSha"),
            environmentName,
            storageProvider);
    }

    /// <summary>Creates a response from build metadata, omitting unavailable or invalid values.</summary>
    public static ApiInfoResponse FromMetadata(string? version, string? buildNumber, string? informationalVersion, string? gitHubSha, string? environmentName, string? storageProvider)
    {
        var separator = informationalVersion?.IndexOf('+') ?? -1;
        var revision = separator >= 0 ? informationalVersion![(separator + 1)..] : null;
        var sha = ValidSha(revision) ? revision : ValidSha(gitHubSha) ? gitHubSha : null;

        return new ApiInfoResponse(SafeValue(version), SafeValue(buildNumber),
            sha is null ? null : sha[..Math.Min(8, sha.Length)], SafeEnvironment(environmentName), SafeProvider(storageProvider));
    }

    private static string? SafeEnvironment(string? name) =>
        new[] { "Development", "Staging", "Production", "Test", "Testing", "Local" }
            .FirstOrDefault(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));

    private static string? SafeProvider(string? name) =>
        new[] { "InMemory", "Sqlite", "SqlServer", "MySQL", "MariaDB", "JsonFile" }
            .FirstOrDefault(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));

    private static string? SafeValue(string? value)
    {
        value = value?.Trim();
        return value is { Length: > 0 and <= 128 } &&
            value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_')
            ? value : null;
    }

    private static bool ValidSha(string? value) =>
        value is { Length: >= 7 and <= 64 } && value.All(Uri.IsHexDigit);
}
