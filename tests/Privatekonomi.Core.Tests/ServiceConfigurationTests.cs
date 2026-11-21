namespace Privatekonomi.Core.Tests;

[TestClass]
public class ServiceConfigurationTests
{
    [TestMethod]
    public void LoadUsesServiceSpecificFilesAndEnvironmentOverrides()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var appHost = Path.Combine(root, "AppHost");
        var web = Path.Combine(root, "Web");
        var api = Path.Combine(root, "Api");
        var environmentKey = $"PRIVATEKONOMI_TEST_{Guid.NewGuid():N}";
        Directory.CreateDirectory(appHost);
        Directory.CreateDirectory(web);
        Directory.CreateDirectory(api);

        try
        {
            File.WriteAllText(Path.Combine(web, "appsettings.json"),
                $$"""{"Storage":{"Provider":"JsonFile","SeedTestData":false},"{{environmentKey}}":"file"}""");
            File.WriteAllText(Path.Combine(web, "appsettings.Development.json"),
                """{"Storage":{"Provider":"Sqlite","SeedTestData":true}}""");
            File.WriteAllText(Path.Combine(api, "appsettings.json"),
                """{"Storage":{"Provider":"InMemory","SeedTestData":true}}""");
            Environment.SetEnvironmentVariable(environmentKey, "environment");

            var webSettings = ServiceConfiguration.Load(appHost, "Web", "Development", Guid.NewGuid().ToString());
            var apiSettings = ServiceConfiguration.Load(appHost, "Api", "Development", Guid.NewGuid().ToString());
            var productionSettings = ServiceConfiguration.Load(appHost, "Web", "Production", Guid.NewGuid().ToString());

            Assert.AreEqual("Sqlite", webSettings["Storage:Provider"]);
            Assert.AreEqual("True", webSettings["Storage:SeedTestData"]);
            Assert.AreEqual("InMemory", apiSettings["Storage:Provider"]);
            Assert.AreEqual("JsonFile", productionSettings["Storage:Provider"]);
            Assert.AreEqual("environment", webSettings[environmentKey]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentKey, null);
            Directory.Delete(root, recursive: true);
        }
    }
}
