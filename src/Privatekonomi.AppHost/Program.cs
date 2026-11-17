var builder = DistributedApplication.CreateBuilder(args);

// Raspberry Pi configuration - listen on all network interfaces
var isRaspberryPi = Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true";
var isLanAccessEnabled = Environment.GetEnvironmentVariable("PRIVATEKONOMI_LAN_ACCESS") == "true";
var exposeServiceEndpoints = isRaspberryPi || isLanAccessEnabled;

// Configure Aspire Dashboard to listen on all network interfaces when on Raspberry Pi
if (isRaspberryPi)
{
    // Ensure DOTNET_DASHBOARD_URLS is set to listen on 0.0.0.0 for network access
    var dashboardUrl = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_URLS");
    if (string.IsNullOrEmpty(dashboardUrl) || dashboardUrl.Contains("localhost") || dashboardUrl.Contains("127.0.0.1"))
    {
        Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_URLS", "http://0.0.0.0:17127");
    }
}

var aspnetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? builder.Environment.EnvironmentName;

var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? aspnetEnvironment;

var privatekonomiEnvironment = Environment.GetEnvironmentVariable("PRIVATEKONOMI_ENVIRONMENT")
    ?? aspnetEnvironment;

var configuredStorageProvider = Environment.GetEnvironmentVariable("PRIVATEKONOMI_STORAGE_PROVIDER");
var storageProvider = configuredStorageProvider ?? "Unknown";
var storageConnectionString = Environment.GetEnvironmentVariable("PRIVATEKONOMI_STORAGE_CONNECTION_STRING");
var configureSelectedStorage = Environment.GetEnvironmentVariable("PRIVATEKONOMI_MACOS_STORAGE_CONFIG") == "true";
// JSON persistence is process-local, so keep it in Web and leave API on its in-memory default.
var usesJsonFileStorage = string.Equals(storageProvider, "JsonFile", StringComparison.OrdinalIgnoreCase);

// Add the API project
var apiBuilder = builder.AddProject<Projects.Privatekonomi_Api>("api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnvironment)
    .WithEnvironment("DOTNET_ENVIRONMENT", dotnetEnvironment)
    .WithEnvironment("PRIVATEKONOMI_ENVIRONMENT", privatekonomiEnvironment)
    .WithEnvironment("PRIVATEKONOMI_LAN_ACCESS", isLanAccessEnabled ? "true" : "false");

if (configureSelectedStorage && configuredStorageProvider is not null)
{
    apiBuilder = apiBuilder
        .WithEnvironment("PRIVATEKONOMI_STORAGE_PROVIDER", storageProvider)
        .WithEnvironment("Storage__Provider", usesJsonFileStorage ? "InMemory" : storageProvider);
    if (!string.IsNullOrWhiteSpace(storageConnectionString) && !usesJsonFileStorage)
    {
        apiBuilder = apiBuilder.WithEnvironment("Storage__ConnectionString", storageConnectionString);
    }
}

if (exposeServiceEndpoints)
{
    // Explicitly set the HTTP endpoint port when exposing services on the LAN.
    // The service binds to 0.0.0.0 based on the environment flag.
    apiBuilder = apiBuilder.WithHttpEndpoint(port: 5277, name: "http");
}
if (isRaspberryPi)
{
    apiBuilder = apiBuilder.WithEnvironment("PRIVATEKONOMI_RASPBERRY_PI", "true");
}

var api = apiBuilder;

// Add the Web project and reference the API
var webBuilder = builder.AddProject<Projects.Privatekonomi_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnvironment)
    .WithEnvironment("DOTNET_ENVIRONMENT", dotnetEnvironment)
    .WithEnvironment("PRIVATEKONOMI_ENVIRONMENT", privatekonomiEnvironment)
    .WithEnvironment("PRIVATEKONOMI_LAN_ACCESS", isLanAccessEnabled ? "true" : "false");

if (configureSelectedStorage && configuredStorageProvider is not null)
{
    webBuilder = webBuilder
        .WithEnvironment("PRIVATEKONOMI_STORAGE_PROVIDER", storageProvider)
        .WithEnvironment("Storage__Provider", storageProvider);
    if (!string.IsNullOrWhiteSpace(storageConnectionString))
    {
        webBuilder = webBuilder.WithEnvironment("Storage__ConnectionString", storageConnectionString);
    }
}

if (exposeServiceEndpoints)
{
    // Explicitly set the HTTP endpoint port when exposing services on the LAN.
    // The service binds to 0.0.0.0 based on the environment flag.
    webBuilder = webBuilder.WithHttpEndpoint(port: 5274, name: "http");
}
if (isRaspberryPi)
{
    webBuilder = webBuilder.WithEnvironment("PRIVATEKONOMI_RASPBERRY_PI", "true");
}

var web = webBuilder;

builder.Build().Run();
