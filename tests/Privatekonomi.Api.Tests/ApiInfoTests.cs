using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Api.Controllers;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Tests.Infrastructure;
using Privatekonomi.Core.Configuration;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class ApiInfoTests
{
    [TestMethod]
    public async Task AnonymousRequestReturnsOnlyPublicApiBuildMetadata()
    {
        await using var factory = new ApiWebApplicationFactory("ApiInfo_" + Guid.NewGuid());
        using var configuredFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<StorageSettings>(settings =>
            {
                settings.Provider = "InMemory";
                settings.ConnectionString = "Server=private-host;Auth=confidential-marker";
            })));
        using var client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/api-info?userId=alice");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("private-host", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("confidential-marker", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("alice", StringComparison.Ordinal));
        using var json = JsonDocument.Parse(body);
        var properties = json.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        CollectionAssert.AreEqual(new[] { "buildNumber", "dataProfile", "environment", "sha", "storageProvider", "version" }, properties);

        var expectedVersion = typeof(ApiInfoController).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>().Single(item => item.Key == "AppVersion").Value;
        Assert.IsFalse(string.IsNullOrWhiteSpace(expectedVersion));
        Assert.AreEqual(expectedVersion, json.RootElement.GetProperty("version").GetString());
        Assert.AreEqual("Development", json.RootElement.GetProperty("environment").GetString());
        Assert.AreEqual("InMemory", json.RootElement.GetProperty("storageProvider").GetString());
    }

    [TestMethod]
    public void MetadataUsesEmbeddedRevisionAndBuildNumber()
    {
        var info = ApiInfoResponse.FromMetadata("0.1.0-alfa", "42", "0.1.0-alfa+1234567890abcdef", "abcdef1234567890", "Production", "sqlserver", "SqlServer");
        Assert.AreEqual("0.1.0-alfa", info.Version);
        Assert.AreEqual("42", info.BuildNumber);
        Assert.AreEqual("12345678", info.Sha);
        Assert.AreEqual("Production", info.Environment);
        Assert.AreEqual("SqlServer", info.StorageProvider);
        Assert.AreEqual("SqlServer", info.DataProfile);
    }

    [TestMethod]
    public void MetadataFallsBackToGitHubSha()
    {
        var info = ApiInfoResponse.FromMetadata("0.1.0-alfa", null, "0.1.0-alfa+invalid", "abcdef1234567890", "testing", null);
        Assert.IsNull(info.BuildNumber);
        Assert.AreEqual("abcdef12", info.Sha);
        Assert.AreEqual("Testing", info.Environment);
    }

    [TestMethod]
    public void MissingOrUnsafeMetadataIsOmitted()
    {
        var info = ApiInfoResponse.FromMetadata(null, "Data Source=private.db;******", "1.0+123", "invalid", "secret-host", "Server=private-host;******");
        Assert.IsNull(info.Version);
        Assert.IsNull(info.BuildNumber);
        Assert.IsNull(info.Sha);
        Assert.IsNull(info.Environment);
        Assert.IsNull(info.StorageProvider);
    }

    [TestMethod]
    [DataRow("inmemory", "InMemory")]
    [DataRow("SQLITE", "Sqlite")]
    [DataRow("sqlserver", "SqlServer")]
    [DataRow("mysql", "MySQL")]
    [DataRow("mariadb", "MariaDB")]
    [DataRow("jsonfile", "JsonFile")]
    [DataRow("unknown", null)]
    public void ProviderIsLimitedToKnownLabels(string provider, string? expected)
    {
        var info = ApiInfoResponse.FromMetadata(null, null, null, null, null, provider);
        Assert.AreEqual(expected, info.StorageProvider);
    }
}
