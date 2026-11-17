using Privatekonomi.ServiceDefaults;

namespace Privatekonomi.Web.Services;

public sealed record ApplicationLogFile(
    string FileName,
    string Application,
    long SizeBytes,
    DateTime LastModifiedUtc,
    bool IsCurrent);

public interface IApplicationLogReader
{
    IReadOnlyList<ApplicationLogFile> GetFiles();

    IReadOnlyList<string> DeleteFiles(IEnumerable<string> fileNames);

    Task<IReadOnlyList<string>> ReadLastLinesAsync(
        string fileName,
        int lineCount,
        CancellationToken cancellationToken = default);
}

public sealed class ApplicationLogReader(
    IConfiguration configuration,
    IHostEnvironment environment) : IApplicationLogReader
{
    public const int MaximumLineCount = 2000;

    private readonly string _logDirectory = ApplicationLogDirectory.Resolve(configuration, environment);

    public IReadOnlyList<ApplicationLogFile> GetFiles()
    {
        if (!Directory.Exists(_logDirectory))
            return Array.Empty<ApplicationLogFile>();

        var files = Directory.EnumerateFiles(_logDirectory, "privatekonomi-*.log", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Select(file => new
            {
                File = file,
                Application = TryGetApplication(file.Name)
            })
            .Where(item => item.Application is not null)
            .Select(item => new ApplicationLogFile(
                item.File.Name,
                item.Application!,
                item.File.Length,
                item.File.LastWriteTimeUtc,
                IsCurrent: false))
            .ToArray();

        var latestWriteByApplication = files
            .GroupBy(file => file.Application, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Max(file => file.LastModifiedUtc), StringComparer.Ordinal);

        return files
            .Select(file => file with { IsCurrent = file.LastModifiedUtc == latestWriteByApplication[file.Application] })
            .OrderByDescending(file => file.LastModifiedUtc)
            .ToArray();
    }

    public IReadOnlyList<string> DeleteFiles(IEnumerable<string> fileNames)
    {
        ArgumentNullException.ThrowIfNull(fileNames);

        var requestedNames = fileNames.Distinct(StringComparer.Ordinal).ToArray();
        if (requestedNames.Length == 0)
            return Array.Empty<string>();

        var currentFileNames = GetFiles()
            .Where(file => file.IsCurrent)
            .Select(file => file.FileName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var fileName in requestedNames)
        {
            if (currentFileNames.Contains(fileName))
                throw new InvalidOperationException("The active log file for each application cannot be deleted.");

            var filePath = GetLogFilePath(fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The requested log file no longer exists.", fileName);
        }

        foreach (var fileName in requestedNames)
            File.Delete(GetLogFilePath(fileName));

        return requestedNames;
    }

    public async Task<IReadOnlyList<string>> ReadLastLinesAsync(
        string fileName,
        int lineCount,
        CancellationToken cancellationToken = default)
    {
        if (lineCount is < 1 or > MaximumLineCount)
            throw new ArgumentOutOfRangeException(nameof(lineCount), $"Line count must be between 1 and {MaximumLineCount}.");

        var filePath = GetLogFilePath(fileName);
        var lines = new Queue<string>(lineCount);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (lines.Count == lineCount)
                lines.Dequeue();

            lines.Enqueue(line);
        }

        return lines.ToArray();
    }

    private string GetLogFilePath(string fileName)
    {
        if (fileName.Contains('/') || fileName.Contains('\\') || TryGetApplication(fileName) is null)
            throw new ArgumentException("Only Privatekonomi Web and API log files can be read.", nameof(fileName));

        var fullPath = Path.GetFullPath(Path.Combine(_logDirectory, fileName));
        var directoryPrefix = Path.EndsInDirectorySeparator(_logDirectory)
            ? _logDirectory
            : _logDirectory + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!fullPath.StartsWith(directoryPrefix, comparison))
            throw new ArgumentException("The requested log file is outside the log directory.", nameof(fileName));

        return fullPath;
    }

    private static string? TryGetApplication(string fileName)
    {
        if (fileName.StartsWith("privatekonomi-web-", StringComparison.Ordinal) && fileName.EndsWith(".log", StringComparison.Ordinal))
            return "Web";

        if (fileName.StartsWith("privatekonomi-api-", StringComparison.Ordinal) && fileName.EndsWith(".log", StringComparison.Ordinal))
            return "API";

        return null;
    }
}