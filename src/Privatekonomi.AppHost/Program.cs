var builder = DistributedApplication.CreateBuilder(args);
const int webPort = 5274;
const int apiPort = 5277;

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

var apiSettings = ServiceConfiguration.Load(builder.AppHostDirectory, "Privatekonomi.Api", dotnetEnvironment,
    "eb8f9493-8e5f-4f49-95ce-0bf639dddbd5");
var webSettings = ServiceConfiguration.Load(builder.AppHostDirectory, "Privatekonomi.Web", dotnetEnvironment,
    "2b2d9b8c-52d5-49e1-8a2b-4fcf76e8ae78");

var apiStorageProvider = builder.AddParameter("api-storage-provider",
    configureSelectedStorage && configuredStorageProvider is not null
        ? usesJsonFileStorage ? "InMemory" : storageProvider
        : apiSettings["Storage:Provider"] ?? "InMemory");
var webStorageProvider = builder.AddParameter("web-storage-provider",
    configureSelectedStorage && configuredStorageProvider is not null
        ? storageProvider : webSettings["Storage:Provider"] ?? "InMemory");
var apiSeedTestData = builder.AddParameter("api-seed-test-data", apiSettings["Storage:SeedTestData"] ?? "false");
var webSeedTestData = builder.AddParameter("web-seed-test-data", webSettings["Storage:SeedTestData"] ?? "false");
var bankSyncEnabled = builder.AddParameter("web-bank-sync-enabled", webSettings["BankSync:Enabled"] ?? "false");
var bankSyncInterval = builder.AddParameter("web-bank-sync-interval-minutes", webSettings["BankSync:IntervalMinutes"] ?? "60");

// Add the API project
var apiBuilder = builder.AddDotnetProject("api", "../Privatekonomi.Api/Privatekonomi.Api.csproj",
        options => options.LaunchProfileName = "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnvironment)
    .WithEnvironment("DOTNET_ENVIRONMENT", dotnetEnvironment)
    .WithEnvironment("PRIVATEKONOMI_ENVIRONMENT", privatekonomiEnvironment)
    .WithEnvironment("PRIVATEKONOMI_LAN_ACCESS", isLanAccessEnabled ? "true" : "false")
    .WithEnvironment("Storage__Provider", apiStorageProvider)
    .WithEnvironment("Storage__SeedTestData", apiSeedTestData);

if (configureSelectedStorage && configuredStorageProvider is not null)
{
    apiBuilder = apiBuilder
        .WithEnvironment("PRIVATEKONOMI_STORAGE_PROVIDER", storageProvider);
    if (!string.IsNullOrWhiteSpace(storageConnectionString) && !usesJsonFileStorage)
    {
        var apiConnectionString = builder.AddParameter("api-storage-connection-string", storageConnectionString, secret: true);
        apiBuilder = apiBuilder.WithEnvironment("Storage__ConnectionString", apiConnectionString);
    }
}

if (exposeServiceEndpoints)
{
    // Keep the advertised endpoint and the service listener on the same fixed port.
    apiBuilder = apiBuilder
        .WithHttpEndpoint(port: apiPort, name: "http")
        .WithEnvironment("PORT", apiPort.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
if (isRaspberryPi)
{
    apiBuilder = apiBuilder.WithEnvironment("PRIVATEKONOMI_RASPBERRY_PI", "true");
}

var api = apiBuilder;

// Add the Web project and reference the API
var webBuilder = builder.AddDotnetProject("web", "../Privatekonomi.Web/Privatekonomi.Web.csproj",
        options => options.LaunchProfileName = "http")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", aspnetEnvironment)
    .WithEnvironment("DOTNET_ENVIRONMENT", dotnetEnvironment)
    .WithEnvironment("PRIVATEKONOMI_ENVIRONMENT", privatekonomiEnvironment)
    .WithEnvironment("PRIVATEKONOMI_LAN_ACCESS", isLanAccessEnabled ? "true" : "false")
    .WithEnvironment("Storage__Provider", webStorageProvider)
    .WithEnvironment("Storage__SeedTestData", webSeedTestData)
    .WithEnvironment("BankSync__Enabled", bankSyncEnabled)
    .WithEnvironment("BankSync__IntervalMinutes", bankSyncInterval);

if (configureSelectedStorage && configuredStorageProvider is not null)
{
    webBuilder = webBuilder
        .WithEnvironment("PRIVATEKONOMI_STORAGE_PROVIDER", storageProvider);
    if (!string.IsNullOrWhiteSpace(storageConnectionString))
    {
        var webConnectionString = builder.AddParameter("web-storage-connection-string", storageConnectionString, secret: true);
        webBuilder = webBuilder.WithEnvironment("Storage__ConnectionString", webConnectionString);
    }
}

if (exposeServiceEndpoints)
{
    // Keep the advertised endpoint and the service listener on the same fixed port.
    webBuilder = webBuilder
        .WithHttpEndpoint(port: webPort, name: "http")
        .WithEnvironment("PORT", webPort.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
if (isRaspberryPi)
{
    webBuilder = webBuilder.WithEnvironment("PRIVATEKONOMI_RASPBERRY_PI", "true");
}

var web = webBuilder;

builder.Build().Run();
