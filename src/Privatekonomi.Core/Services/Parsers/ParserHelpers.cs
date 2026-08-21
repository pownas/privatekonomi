namespace Privatekonomi.Core.Services.Parsers;

/// <summary>
/// Shared utility methods used by multiple CSV/OFX parsers.
/// </summary>
internal static class ParserHelpers
{
    /// <summary>
    /// Truncates a string to at most <paramref name="max"/> characters, appending "…" if truncated.
    /// </summary>
    internal static string Truncate(string s, int max = 200) =>
        s.Length <= max ? s : s.Substring(0, max) + "…";
}
