using NuGet.Versioning;

internal static class SbomVersioning
{
    internal static bool IsVersionAffected(string currentVersionStr, string rangeStr)
    {
        if (!NuGetVersion.TryParse(currentVersionStr, out var currentVersion) ||
            !VersionRange.TryParse(rangeStr, out var range))
        {
            throw new InvalidDataException($"Ogiltig paketversion eller sårbarhetsintervall: {currentVersionStr}, {rangeStr}");
        }

        return range.Satisfies(currentVersion);
    }

    internal static bool IsUpdateAvailable(string installedVersion, string latestVersion) =>
        NuGetVersion.TryParse(latestVersion, out var latest) &&
        NuGetVersion.TryParse(installedVersion, out var installed) && latest > installed;
}