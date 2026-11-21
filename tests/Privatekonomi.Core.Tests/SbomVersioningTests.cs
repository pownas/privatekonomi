namespace Privatekonomi.Core.Tests;

[TestClass]
public class SbomVersioningTests
{
    [TestMethod]
    [DataRow("1.0.0-beta.2", "[1.0.0-beta.1, 1.0.0)", true)]
    [DataRow("1.0.0", "[1.0.0-beta.1, 1.0.0)", false)]
    [DataRow("1.2.0", "[1.2.0]", true)]
    [DataRow("2.0.0", "(, 2.0.0]", true)]
    [DataRow("2.0.1", "(, 2.0.0]", false)]
    public void IsVersionAffectedUsesNuGetVersionRanges(string version, string range, bool expected)
    {
        Assert.AreEqual(expected, SbomVersioning.IsVersionAffected(version, range));
    }

    [TestMethod]
    public void IsVersionAffectedInvalidRangeThrows()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => SbomVersioning.IsVersionAffected("1.0.0", "invalid"));
    }

    [TestMethod]
    [DataRow("2.0.0-preview.1", "1.9.0", false)]
    [DataRow("2.0.0-preview.1", "2.0.0", true)]
    [DataRow("1.0", "1.0.0", false)]
    [DataRow("1.0.0", "Okänd", false)]
    public void IsUpdateAvailableRequiresNewerVersion(string installed, string latest, bool expected)
    {
        Assert.AreEqual(expected, SbomVersioning.IsUpdateAvailable(installed, latest));
    }
}