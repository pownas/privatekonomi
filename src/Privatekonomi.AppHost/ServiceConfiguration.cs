using Microsoft.Extensions.Configuration;

internal static class ServiceConfiguration
{
    public static IConfiguration Load(string appHostDirectory, string projectName, string environment, string userSecretsId)
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(appHostDirectory, "..", projectName));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(projectDirectory, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(projectDirectory, $"appsettings.{environment}.json"), optional: true);

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            configuration.AddUserSecrets(userSecretsId);
        }

        return configuration.AddEnvironmentVariables().Build();
    }
}
