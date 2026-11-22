using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Privatekonomi.Api.Services;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class ApiDataProfileTests
{
    [TestMethod]
    [DataRow("Mock", "Development", "InMemory", false)]
    [DataRow("InMemory", "Production", "InMemory", false)]
    [DataRow("Sqlite", "Development", "Sqlite", false)]
    [DataRow("SqlServer", "Development", "SqlServer", false)]
    [DataRow("Sqlite", "Production", "Sqlite", false)]
    public void ApplyDataProfileSetsMatchingStorage(
        string profile, string environmentName, string expectedProvider, bool seedTestData)
    {
        var values = new Dictionary<string, string?>
        {
            ["ApiData:Profile"] = profile,
            ["ApiData:SqliteConnectionString"] = "Data Source=privatekonomi-api.db",
            ["ApiData:SqlServerConnectionString"] = "Server=localhost;Database=PrivatekonomiApi;Integrated Security=true",
            ["Storage:SeedTestData"] = seedTestData.ToString()
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var environment = CreateEnvironment(environmentName);

        ApiSeedPolicy.ApplyDataProfile(configuration, environment);

        Assert.AreEqual(expectedProvider, configuration["Storage:Provider"]);
        if (profile == "Sqlite")
            Assert.AreEqual("Data Source=privatekonomi-api.db", configuration["Storage:ConnectionString"]);
        if (profile == "InMemory")
            Assert.AreEqual(string.Empty, configuration["Storage:ConnectionString"]);
        if (profile == "SqlServer")
            Assert.AreEqual("Server=localhost;Database=PrivatekonomiApi;Integrated Security=true", configuration["Storage:ConnectionString"]);
    }

    [TestMethod]
    public void MockProfileIsRejectedInProduction()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiData:Profile"] = "Mock",
            ["Storage:Provider"] = "InMemory",
            ["Storage:SeedTestData"] = "false"
        }).Build();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ApiSeedPolicy.ApplyDataProfile(configuration, CreateEnvironment("Production")));
    }

    [TestMethod]
    public void ProductionSeedingIsRejected()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiData:Profile"] = "Sqlite",
            ["ApiData:SqliteConnectionString"] = "Data Source=privatekonomi-api.db",
            ["Storage:SeedTestData"] = "true"
        }).Build();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ApiSeedPolicy.ApplyDataProfile(configuration, CreateEnvironment("Production")));
    }

    [TestMethod]
    public void SqlServerProfileRequiresConnectionString()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiData:Profile"] = "SqlServer",
            ["ApiData:SqlServerConnectionString"] = "",
            ["Storage:Provider"] = "InMemory",
            ["Storage:SeedTestData"] = "false"
        }).Build();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ApiSeedPolicy.ApplyDataProfile(configuration, CreateEnvironment("Development")));
    }

    private static TestHostEnvironment CreateEnvironment(string environmentName)
    {
        return new TestHostEnvironment { EnvironmentName = environmentName };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Privatekonomi.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
