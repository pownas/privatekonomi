using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Privatekonomi.ServiceDefaults;

public static class ApplicationLogDirectory
{
    public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredDirectory = configuration["PRIVATEKONOMI_LOG_DIRECTORY"];
        return string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "logs"))
            : Path.GetFullPath(Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(environment.ContentRootPath, configuredDirectory));
    }
}