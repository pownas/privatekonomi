using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Privatekonomi.Core.Models;
using Privatekonomi.Web.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class PiUpdateTests
{
    [TestMethod]
    public async Task AdminRequirement_RequiresAuthenticatedSystemAdmin()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        manager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(new ApplicationUser { IsSystemAdmin = true });
        manager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(new ApplicationUser { IsSystemAdmin = false });
        var handler = new PiUpdateAdminHandler(manager.Object);
        var requirement = new PiUpdateAdminRequirement();

        foreach (var (id, authenticated, authorized) in new[]
        {
            ("1", true, true), ("2", true, false), ("1", false, false)
        })
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)],
                authenticated ? "cookie" : null);
            var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), null);
            await handler.HandleAsync(context);
            Assert.AreEqual(authorized, context.HasSucceeded);
        }
    }

    [TestMethod]
    public void Request_IsUnavailableWithoutOptInOrPi()
    {
        using var fixture = new UpdateFixture();
        Assert.IsFalse(fixture.Service(enabled: false).TryRequest());
        Assert.IsFalse(fixture.Service(pi: false).TryRequest());
        Assert.IsFalse(fixture.Service(marker: false).TryRequest());
    }

    [TestMethod]
    public async Task Request_IsExclusiveAndTracksStatusAfterRestart()
    {
        using var fixture = new UpdateFixture();
        var service = fixture.Service();
        var attempts = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => Task.Run(service.TryRequest)));
        Assert.AreEqual(1, attempts.Count(result => result));
        Assert.AreEqual("queued", fixture.Service().GetStatus().State);
        File.WriteAllText(fixture.Path("status"), "running\n");
        Assert.AreEqual("running", service.GetStatus().State);
        Assert.IsFalse(service.TryRequest());

        File.Delete(fixture.Path("request"));
        Assert.AreEqual("failed", fixture.Service().GetStatus().State);
        File.WriteAllText(fixture.Path("status"), "failed\n");
        File.WriteAllText(fixture.Path("log"), "build failed");
        Assert.AreEqual("failed", fixture.Service().GetStatus().State);
        Assert.AreEqual("build failed", service.GetStatus().Log);
        Assert.IsTrue(service.TryRequest());
        File.WriteAllText(fixture.Path("transaction"), fixture.Path("backup"));
        File.WriteAllText(fixture.Path("status"), "failed\n");
        Assert.AreEqual("blocked", service.GetStatus().State);
        Assert.IsFalse(service.TryRequest());
    }

    [TestMethod]
    public void Status_ReportsSuccessfulCommitAndLimitsLog()
    {
        using var fixture = new UpdateFixture();
        File.WriteAllText(fixture.Path("status"), "succeeded\n");
        File.WriteAllText(fixture.Path("installed"), new string('a', 40));
        File.WriteAllText(fixture.Path("log"), new string('x', 9000));
        var status = fixture.Service().GetStatus();
        Assert.AreEqual("succeeded", status.State);
        Assert.AreEqual(new string('a', 40), status.InstalledCommit);
        Assert.AreEqual(8192, status.Log.Length);
    }

    [TestMethod]
    public void Status_UsesPublishedCommitInsteadOfStaleUpdateState()
    {
        using var fixture = new UpdateFixture();
        var published = fixture.Path("published-commit");
        File.WriteAllText(fixture.Path("installed"), new string('a', 40));
        File.WriteAllText(published, new string('b', 40));
        Assert.AreEqual(new string('b', 40), fixture.Service(commitFile: published).GetStatus().InstalledCommit);
        File.Delete(published);
        Assert.IsNull(fixture.Service(commitFile: published).GetStatus().InstalledCommit);
    }

    private sealed class UpdateFixture : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pi-update-" + Guid.NewGuid());

        public UpdateFixture()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path("enabled"), "");
        }

        public string Path(string name) => System.IO.Path.Combine(_directory, name);

        public PiUpdateService Service(bool enabled = true, bool pi = true, bool marker = true, string? commitFile = null)
        {
            if (!marker)
                File.Delete(Path("enabled"));
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PiUpdate:Enabled"] = enabled.ToString(),
                ["PiUpdate:StateDirectory"] = _directory,
                ["PiUpdate:InstalledCommitFile"] = commitFile
            }).Build();
            return new PiUpdateService(config, () => pi);
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}
