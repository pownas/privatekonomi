using Privatekonomi.Web.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class AppVersionInfoTests
{
    [TestMethod]
    public void CurrentUsesWebAssemblyVersion()
    {
        StringAssert.StartsWith(AppVersionInfo.Current, "Version 0.1.0-beta · Bygge ");
    }

    [TestMethod]
    public void FormatUsesEmbeddedRevisionAndBuildNumber()
    {
        Assert.AreEqual("Version 0.1.0-alfa · Bygge 42 · SHA 12345678",
            AppVersionInfo.Format("0.1.0-alfa", "42", "0.1.0-alfa+1234567890abcdef", "abcdef1234567890"));
    }

    [TestMethod]
    public void FormatFallsBackToGitHubSha()
    {
        Assert.AreEqual("Version 0.1.0-alfa · Bygge okänd · SHA abcdef12",
            AppVersionInfo.Format("0.1.0-alfa", null, "0.1.0-alfa", "abcdef1234567890"));
    }

    [TestMethod]
    public void FormatFallsBackWhenEmbeddedRevisionIsInvalid()
    {
        Assert.AreEqual("Version 0.1.0-alfa · Bygge 42 · SHA abcdef12",
            AppVersionInfo.Format("0.1.0-alfa", "42", "0.1.0-alfa+invalid", "abcdef1234567890"));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("0.1.0-alfa+not-a-sha", "invalid")]
    [DataRow("0.1.0-alfa+123", "")]
    public void FormatHandlesMissingOrInvalidMetadata(string? informationalVersion, string? gitHubSha)
    {
        Assert.AreEqual("Version okänd · Bygge okänd · SHA okänd",
            AppVersionInfo.Format(null, " ", informationalVersion, gitHubSha));
    }
}
