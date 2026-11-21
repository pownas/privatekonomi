using System.Reflection;

namespace Privatekonomi.Web.Services;

/// <summary>Formats the version metadata embedded in the web application at build time.</summary>
public static class AppVersionInfo
{
    /// <summary>The version of the running web application.</summary>
    public static string Current { get; } = FromAssembly(typeof(AppVersionInfo).Assembly);

    private static string FromAssembly(Assembly assembly)
    {
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
        string? GetMetadata(string key) => metadata.FirstOrDefault(item => item.Key == key)?.Value;

        return Format(
            GetMetadata("AppVersion"),
            GetMetadata("BuildNumber"),
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            GetMetadata("GitHubSha"));
    }

    /// <summary>Formats version, build number and source revision, including unavailable values.</summary>
    public static string Format(string? version, string? buildNumber, string? informationalVersion, string? gitHubSha)
    {
        var separator = informationalVersion?.IndexOf('+') ?? -1;
        var revision = separator >= 0 ? informationalVersion![(separator + 1)..] : null;
        var sha = ValidSha(revision) ? revision : ValidSha(gitHubSha) ? gitHubSha : null;

        return $"Version {ValueOrUnknown(version)} · Bygge {ValueOrUnknown(buildNumber)} · SHA {(sha is null ? "okänd" : sha[..Math.Min(8, sha.Length)])}";
    }

    private static string ValueOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "okänd" : value.Trim();

    private static bool ValidSha(string? value) =>
        value is { Length: >= 7 and <= 64 } && value.All(Uri.IsHexDigit);
}
