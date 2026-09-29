using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Privatekonomi.Web.Services;

public sealed record SiteChangeCommit(
    string Sha,
    string ShortSha,
    string Title,
    string? Summary,
    string Author,
    DateTimeOffset? AuthoredAt,
    string? CommitUrl,
    bool IsInstalled);

public sealed record SiteChangeHistory(
    string? InstalledCommit,
    string? LatestCommit,
    string? CompareUrl,
    string? StatusMessage,
    IReadOnlyList<SiteChangeCommit> Commits);

public sealed class SiteChangeHistoryService(IHttpClientFactory httpClients, PiUpdateService updates)
{
    private const int DefaultCommitCount = 10;
    private const string RepositoryCommitsUrl = "repos/pownas/Privatekonomi/commits?sha=main&per_page={0}";

    public async Task<SiteChangeHistory> GetRecentChangesAsync(int count = DefaultCommitCount, CancellationToken cancellationToken = default)
    {
        var installedCommit = updates.GetStatus().InstalledCommit;

        try
        {
            using var client = httpClients.CreateClient("pi-update");
            var result = await client.GetFromJsonAsync<List<GitHubCommitDto>>(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RepositoryCommitsUrl, Math.Clamp(count, 1, 10)),
                cancellationToken);

            var commits = (result ?? [])
                .Select(commit => MapCommit(commit, installedCommit))
                .Where(commit => commit is not null)
                .Cast<SiteChangeCommit>()
                .ToArray();

            var latestCommit = commits.FirstOrDefault()?.Sha;
            return new(
                installedCommit,
                latestCommit,
                CreateCompareUrl(installedCommit, latestCommit),
                commits.Length == 0 ? "Det finns ingen tillgänglig versionshistorik just nu." : null,
                commits);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return new(
                installedCommit,
                null,
                null,
                installedCommit is null
                    ? "Versionsinformationen är inte tillgänglig just nu."
                    : "Det gick inte att hämta de senaste ändringarna just nu.",
                Array.Empty<SiteChangeCommit>());
        }
    }

    private static SiteChangeCommit? MapCommit(GitHubCommitDto commit, string? installedCommit)
    {
        if (!IsCommitSha(commit.Sha))
            return null;

        var (title, summary) = SplitMessage(commit.Commit?.Message);
        var author = FirstNonEmpty(commit.Commit?.Author?.Name, commit.Commit?.Committer?.Name, "Okänd");
        var authoredAt = commit.Commit?.Author?.Date ?? commit.Commit?.Committer?.Date;

        return new(
            commit.Sha,
            commit.Sha[..8],
            title,
            summary,
            author,
            authoredAt,
            SanitizeCommitUrl(commit.HtmlUrl),
            string.Equals(commit.Sha, installedCommit, StringComparison.OrdinalIgnoreCase));
    }

    private static string? CreateCompareUrl(string? installedCommit, string? latestCommit)
    {
        if (!IsCommitSha(installedCommit) || !IsCommitSha(latestCommit) || string.Equals(installedCommit, latestCommit, StringComparison.OrdinalIgnoreCase))
            return null;

        return $"https://github.com/pownas/Privatekonomi/compare/{installedCommit}...{latestCommit}";
    }

    private static bool IsCommitSha(string? value) =>
        value is not null && Regex.IsMatch(value, @"\A[0-9a-f]{40}\z", RegexOptions.CultureInvariant);

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static (string Title, string? Summary) SplitMessage(string? message)
    {
        var lines = (message ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

        var titleIndex = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
        if (titleIndex < 0)
            return ("Uppdatering utan beskrivning", null);

        var title = NormalizeWhitespace(lines[titleIndex]);
        var summaryLines = lines[(titleIndex + 1)..]
            .SkipWhile(string.IsNullOrWhiteSpace)
            .TakeWhile(line => !string.IsNullOrWhiteSpace(line))
            .Select(NormalizeWhitespace)
            .Where(line => line.Length > 0)
            .ToArray();

        if (summaryLines.Length == 0)
            return (title, null);

        var summary = string.Join(" ", summaryLines);
        if (summary.Length > 240)
            summary = summary[..237] + "...";

        return (title, summary);
    }

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);

    private static string? SanitizeCommitUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.ToString();
    }

    private sealed record GitHubCommitDto(string Sha, GitHubCommitDetails? Commit, string? HtmlUrl);

    private sealed record GitHubCommitDetails(string? Message, GitHubCommitAuthor? Author, GitHubCommitAuthor? Committer);

    private sealed record GitHubCommitAuthor(string? Name, DateTimeOffset? Date);
}
