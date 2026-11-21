using System.Net;
using System.Text;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class SbomDataProviderTests
{
    private const string Index = "https://example.test/index.json";
    private const string IndexJson = """
        {"resources":[{"@id":"https://example.test/registration","@type":"RegistrationsBaseUrl/3.6.0"},{"@id":"https://example.test/vulnerabilities","@type":"VulnerabilityInfo/6.7.0"}]}
        """;
    private const string PagesJson = """
        [{"@name":"base","@id":"https://example.test/base"},{"@name":"update","@id":"https://example.test/update"}]
        """;
    private const string BaseJson = """
        {"example.package":[{"severity":2,"versions":"[1.0.0,2.0.0)","url":"https://example.test/advisory"}]}
        """;
    private const string UpdateJson = """
        {"other.package":[{"severity":1,"versions":"[1.0.0]","url":"https://example.test/other"}]}
        """;

    [TestMethod]
    public async Task OnlineDownloadSavesAllPagesAndOfflineUsesSameTimestamp()
    {
        using var directory = new TestDirectory();
        using var client = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/index.json" => Json(IndexJson),
            "/vulnerabilities" => Json(PagesJson),
            "/base" => Json(BaseJson),
            "/update" => Json(UpdateJson),
            _ => throw new InvalidOperationException("Unexpected request")
        });

        var online = await SbomDataProvider.LoadAsync(client, Index, directory.CachePath, false, false);
        string cacheJson = await File.ReadAllTextAsync(directory.CachePath);
        var offline = await SbomDataProvider.LoadAsync(client, Index, directory.CachePath, true, false);

        Assert.IsFalse(online.IsCached);
        StringAssert.Contains(cacheJson, "\n  \"UpdatedAtUtc\"");
        StringAssert.Contains(cacheJson, "\n  \"Vulnerabilities\"");
        Assert.IsTrue(offline.IsCached);
        Assert.AreEqual(online.Cache.UpdatedAtUtc, offline.Cache.UpdatedAtUtc);
        Assert.AreEqual(2, offline.Cache.Vulnerabilities.Count);
        Assert.AreEqual("https://example.test/other", offline.Cache.Vulnerabilities["OTHER.PACKAGE"][0].Url);
        Assert.IsNull(offline.RegistrationUrl);
    }

    [TestMethod]
    public async Task TimeoutUsesCacheWithoutReplacingIt()
    {
        using var directory = new TestDirectory();
        using var online = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/index.json" => Json(IndexJson),
            "/vulnerabilities" => Json(PagesJson),
            "/base" => Json(BaseJson),
            "/update" => Json(UpdateJson),
            _ => throw new InvalidOperationException("Unexpected request")
        });
        var original = await SbomDataProvider.LoadAsync(online, Index, directory.CachePath, false, false);
        using var interrupted = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/index.json" => Json(IndexJson),
            "/vulnerabilities" => Json(PagesJson),
            "/base" => Json(BaseJson),
            "/update" => throw new TaskCanceledException("timeout"),
            _ => throw new InvalidOperationException("Unexpected request")
        });

        var result = await SbomDataProvider.LoadAsync(interrupted, Index, directory.CachePath, false, false);

        Assert.IsTrue(result.IsCached);
        StringAssert.Contains(result.Warning!, "timeout");
        Assert.AreEqual(original.Cache.UpdatedAtUtc, result.Cache.UpdatedAtUtc);
        Assert.AreEqual(2, result.Cache.Vulnerabilities.Count);
    }

    [TestMethod]
    public async Task UnavailableIndexRequiresExistingCache()
    {
        using var directory = new TestDirectory();
        using var client = CreateClient(_ => throw new HttpRequestException("network unavailable"));

        var error = await Assert.ThrowsExactlyAsync<FileNotFoundException>(() =>
            SbomDataProvider.LoadAsync(client, Index, directory.CachePath, false, false));
        StringAssert.Contains(error.Message, "network unavailable");
    }

    [TestMethod]
    public async Task UnavailableIndexUsesExistingCache()
    {
        using var directory = new TestDirectory();
        using var online = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/index.json" => Json(IndexJson),
            "/vulnerabilities" => Json(PagesJson),
            "/base" => Json(BaseJson),
            "/update" => Json(UpdateJson),
            _ => throw new InvalidOperationException("Unexpected request")
        });
        var saved = await SbomDataProvider.LoadAsync(online, Index, directory.CachePath, false, false);
        using var offline = CreateClient(_ => throw new HttpRequestException("network unavailable"));

        var result = await SbomDataProvider.LoadAsync(offline, Index, directory.CachePath, false, false);

        Assert.IsTrue(result.IsCached);
        Assert.AreEqual(saved.Cache.UpdatedAtUtc, result.Cache.UpdatedAtUtc);
        StringAssert.Contains(result.Warning!, "network unavailable");
    }

    [TestMethod]
    public async Task RequireOnlineDoesNotFallBack()
    {
        using var directory = new TestDirectory();
        using var client = CreateClient(_ => throw new TaskCanceledException("timeout"));

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() =>
            SbomDataProvider.LoadAsync(client, Index, directory.CachePath, false, true));
    }

    [TestMethod]
    public async Task InvalidCacheFailsRatherThanReportingNoVulnerabilities()
    {
        using var directory = new TestDirectory();
        await File.WriteAllTextAsync(directory.CachePath, "{\"UpdatedAtUtc\":\"2026-01-01T00:00:00Z\",\"Vulnerabilities\":{}}");
        using var client = CreateClient(_ => throw new InvalidOperationException("Offline mode must not make HTTP requests"));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
            SbomDataProvider.LoadAsync(client, Index, directory.CachePath, true, false));
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond) => new(new TestHandler(respond));

    private sealed class TestHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class TestDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"sbom-test-{Guid.NewGuid():N}");
        public string CachePath => Path.Combine(_path, "cache.json");

        public TestDirectory() => Directory.CreateDirectory(_path);

        public void Dispose() => Directory.Delete(_path, recursive: true);
    }
}