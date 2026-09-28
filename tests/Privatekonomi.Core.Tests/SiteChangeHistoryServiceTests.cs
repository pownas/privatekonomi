using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using Privatekonomi.Web.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class SiteChangeHistoryServiceTests
{
    [TestMethod]
    public async Task GetRecentChangesAsyncReturnsCommitSummariesAndCompareLink()
    {
        using var fixture = new UpdateFixture();
        var installedCommit = new string('b', 40);
        File.WriteAllText(fixture.Path("installed"), installedCommit);

        var latestCommit = new string('a', 40);
        var response = $$"""
        [
          {
            "sha": "{{latestCommit}}",
            "html_url": "https://github.com/pownas/Privatekonomi/commit/{{latestCommit}}",
            "commit": {
              "message": "Ny adminvy\n\nVisar senaste ändringar från sajten till administratörer.",
              "author": {
                "name": "Pownas",
                "date": "2026-09-28T19:00:00Z"
              }
            }
          },
          {
            "sha": "{{installedCommit}}",
            "html_url": "https://github.com/pownas/Privatekonomi/commit/{{installedCommit}}",
            "commit": {
              "message": "Tidigare release",
              "author": {
                "name": "Copilot",
                "date": "2026-09-27T12:00:00Z"
              }
            }
          }
        ]
        """;

        var service = fixture.CreateHistoryService(response);

        var history = await service.GetRecentChangesAsync();

        Assert.AreEqual(installedCommit, history.InstalledCommit);
        Assert.AreEqual(latestCommit, history.LatestCommit);
        Assert.AreEqual($"https://github.com/pownas/Privatekonomi/compare/{installedCommit}...{latestCommit}", history.CompareUrl);
        Assert.IsNull(history.StatusMessage);
        Assert.AreEqual(2, history.Commits.Count);
        Assert.AreEqual("Ny adminvy", history.Commits[0].Title);
        Assert.AreEqual("Visar senaste ändringar från sajten till administratörer.", history.Commits[0].Summary);
        Assert.AreEqual("Pownas", history.Commits[0].Author);
        Assert.AreEqual("aaaaaaaa", history.Commits[0].ShortSha);
        Assert.IsFalse(history.Commits[0].IsInstalled);
        Assert.AreEqual("bbbbbbbb", history.Commits[1].ShortSha);
        Assert.IsTrue(history.Commits[1].IsInstalled);
    }

    [TestMethod]
    public async Task GetRecentChangesAsyncReturnsGracefulMessageWhenMetadataUnavailable()
    {
        using var fixture = new UpdateFixture();
        var service = fixture.CreateHistoryService(exception: new HttpRequestException("network down"));

        var history = await service.GetRecentChangesAsync();

        Assert.IsNull(history.InstalledCommit);
        Assert.IsNull(history.LatestCommit);
        Assert.IsNull(history.CompareUrl);
        Assert.AreEqual(0, history.Commits.Count);
        Assert.AreEqual("Versionsinformationen är inte tillgänglig just nu.", history.StatusMessage);
        Assert.IsFalse(history.StatusMessage!.Contains(fixture.Path("installed"), StringComparison.Ordinal));
        Assert.IsFalse(history.StatusMessage.Contains("network down", StringComparison.Ordinal));
    }

    private sealed class UpdateFixture : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "site-change-history-" + Guid.NewGuid());

        public UpdateFixture()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path("enabled"), string.Empty);
        }

        public string Path(string name) => System.IO.Path.Combine(_directory, name);

        public SiteChangeHistoryService CreateHistoryService(string? response = null, Exception? exception = null)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PiUpdate:Enabled"] = true.ToString(),
                ["PiUpdate:StateDirectory"] = _directory
            }).Build();

            var updateService = new PiUpdateService(config, () => true);
            var handler = new StubHttpMessageHandler(response, exception);
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(factory => factory.CreateClient("pi-update"))
                .Returns(new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://api.github.com/")
                });

            return new SiteChangeHistoryService(httpClientFactory.Object, updateService);
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }

    private sealed class StubHttpMessageHandler(string? response, Exception? exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (exception is not null)
                throw exception;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response ?? "[]", Encoding.UTF8, "application/json")
            });
        }
    }
}
