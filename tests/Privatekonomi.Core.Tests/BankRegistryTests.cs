using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BankRegistryTests
{
    [TestMethod]
    public void SupportedBanksShouldBeSortedByName()
    {
        var names = BankRegistry.SupportedBanks.Select(b => b.Name).ToList();
        var sortedNames = names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        CollectionAssert.AreEqual(sortedNames, names);
    }

    [TestMethod]
    public void SupportedBanksShouldContainAllRequiredBanks()
    {
        // Arrange
        var requiredBanks = new[]
        {
            BankNames.Csn, BankNames.Handelsbanken, BankNames.IcaBanken, BankNames.Nordea, BankNames.Seb,
            BankNames.Swedbank, BankNames.Avanza, BankNames.Nordnet, BankNames.Lysa,
            BankNames.Opti, BankNames.Klarna, BankNames.LantmannenFinans, BankNames.Revolut,
            BankNames.NordiskaBanken, BankNames.Montrose
        };
        
        // Act
        var bankNames = BankRegistry.SupportedBanks.Select(b => b.Name).ToList();
        
        // Assert
        Assert.AreEqual(requiredBanks.Length, BankRegistry.SupportedBanks.Count);
        foreach (var requiredBank in requiredBanks)
        {
            CollectionAssert.Contains(bankNames, requiredBank);
        }
    }
    
    [TestMethod]
    public void AllBanksShouldHaveColorDefined()
    {
        // Assert
        foreach (var bank in BankRegistry.SupportedBanks)
        {
            Assert.IsNotNull(bank.Name);
            Assert.AreNotEqual(0, bank.Name.Length);
            Assert.IsNotNull(bank.ColorHex);
            Assert.AreNotEqual(0, bank.ColorHex.Length);
            StringAssert.StartsWith(bank.ColorHex, "#");
        }
    }

    [TestMethod]
    public void AllBanksShouldHaveUniqueWebsite()
    {
        var websites = BankRegistry.SupportedBanks
            .Select(b => BankRegistry.NormalizeWebsite(b.Website))
            .ToList();

        Assert.AreEqual(websites.Count, websites.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.IsTrue(BankRegistry.SupportedBanks.All(b =>
            Uri.TryCreate(b.Website, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)));
    }

    [TestMethod]
    public void GetBankByWebsiteShouldBeNormalized()
    {
        var bank = BankRegistry.GetBankByWebsite("https://www.icabanken.se/");

        Assert.IsNotNull(bank);
        Assert.AreEqual("ICA-banken", bank.Name);
        Assert.AreEqual("https://www.icabanken.se", bank.Website.TrimEnd('/'));
    }
    
    [TestMethod]
    [DataRow("Handelsbanken", "#003781")]
    [DataRow("CSN", "#64CCC9")]
    [DataRow("ICA-banken", "#E3000F")]
    [DataRow("Nordea", "#0000A0")]
    [DataRow("SEB", "#60CD18")]
    [DataRow("Swedbank", "#FF7900")]
    [DataRow("Avanza", "#00C281")]
    public void GetBankByNameShouldReturnCorrectBank(string bankName, string expectedColor)
    {
        // Act
        var bank = BankRegistry.GetBankByName(bankName);
        
        // Assert
        Assert.IsNotNull(bank);
        Assert.AreEqual(bankName, bank.Name);
        Assert.AreEqual(expectedColor, bank.ColorHex);
    }
    
    [TestMethod]
    public void GetBankByNameShouldBeCaseInsensitive()
    {
        // Act
        var bank1 = BankRegistry.GetBankByName("handelsbanken");
        var bank2 = BankRegistry.GetBankByName("HANDELSBANKEN");
        var bank3 = BankRegistry.GetBankByName("Handelsbanken");
        
        // Assert
        Assert.IsNotNull(bank1);
        Assert.IsNotNull(bank2);
        Assert.IsNotNull(bank3);
        Assert.AreEqual(bank1.ColorHex, bank2.ColorHex);
        Assert.AreEqual(bank2.ColorHex, bank3.ColorHex);
    }
    
    [TestMethod]
    public void GetBankByNameWithNonExistentBankShouldReturnNull()
    {
        // Act
        var bank = BankRegistry.GetBankByName("NonExistentBank");
        
        // Assert
        Assert.IsNull(bank);
    }
    
    [TestMethod]
    [DataRow("Handelsbanken", "#003781")]
    [DataRow("SEB", "#60CD18")]
    [DataRow(null, null)]
    [DataRow("", null)]
    [DataRow("UnknownBank", null)]
    public void GetBankColorShouldReturnCorrectColor(string? bankName, string? expectedColor)
    {
        // Act
        var color = BankRegistry.GetBankColor(bankName);
        
        // Assert
        Assert.AreEqual(expectedColor, color);
    }
}
