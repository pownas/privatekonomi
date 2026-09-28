using System.Text.RegularExpressions;

namespace Privatekonomi.Web.Services;

public sealed record PiUpdateStatus(string State, string? InstalledCommit, string Log);

public sealed class PiUpdateService(IConfiguration configuration, Func<bool>? isRaspberryPi = null)
{
    private readonly string? _directory = configuration["PiUpdate:StateDirectory"];
    private readonly string? _installedCommitFile = configuration["PiUpdate:InstalledCommitFile"];
    private readonly Func<bool> _isRaspberryPi = isRaspberryPi ?? (() =>
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64 &&
        Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true" &&
        File.Exists("/proc/device-tree/model") &&
        File.ReadAllText("/proc/device-tree/model").Contains("Raspberry Pi", StringComparison.Ordinal));

    public bool IsAvailable =>
        configuration.GetValue<bool>("PiUpdate:Enabled") &&
        _isRaspberryPi() &&
        !string.IsNullOrWhiteSpace(_directory) &&
        File.Exists(Path.Combine(_directory, "enabled"));

    public PiUpdateStatus GetStatus()
    {
        if (!IsAvailable)
            return new("disabled", null, "");

        var state = ReadLimited("status", 40).Trim();
        if (state is not ("running" or "succeeded" or "failed"))
            state = "idle";
        if (File.Exists(Path.Combine(_directory!, "request")))
            state = File.Exists(Path.Combine(_directory!, "transaction"))
                ? state == "failed" ? "blocked" : "running"
                : state == "running" ? "running" : "queued";
        else if (state == "running")
            state = "failed";

        var commit = _installedCommitFile is null ? ReadLimited("installed", 64).Trim() : ReadLimitedPath(_installedCommitFile, 64).Trim();
        return new(state, Regex.IsMatch(commit, @"\A[0-9a-f]{40}\z") ? commit : null, ReadLimited("log", 8192));
    }

    public bool TryRequest()
    {
        if (!IsAvailable)
            return false;

        var status = GetStatus();
        if (status.State is "running" or "queued" or "blocked")
            return false;

        try
        {
            using var request = new FileStream(Path.Combine(_directory!, "request"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private string ReadLimited(string name, int maxLength) =>
        ReadLimitedPath(Path.Combine(_directory!, name), maxLength);

    private static string ReadLimitedPath(string path, int maxLength)
    {
        try
        {
            using var file = File.OpenRead(path);
            if (file.Length > maxLength)
                file.Seek(-maxLength, SeekOrigin.End);
            using var reader = new StreamReader(file);
            return reader.ReadToEnd();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "";
        }
    }
}
