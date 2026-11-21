using Microsoft.Extensions.Configuration;

internal static class ServiceConfiguration
{
    public static IConfiguration Load(string appHostDirectory, string projectName, string environment, string userSecretsId,
        IDictionary<string, string?>? overrides = null)
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(appHostDirectory, "..", projectName));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(projectDirectory, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(projectDirectory, $"appsettings.{environment}.json"), optional: true)
            .AddEnvironmentVariables();

        if (overrides is not null)
        {
            configuration.AddInMemoryCollection(overrides);
        }

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(environment, "Local", StringComparison.OrdinalIgnoreCase))
        {
            configuration.AddJsonFile(Path.Combine(projectDirectory, "appsettings.local.json"), optional: true);
            configuration.AddUserSecrets(userSecretsId);
        }

        return configuration.Build();
    }
}
