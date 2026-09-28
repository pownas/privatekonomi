using Privatekonomi.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BankRegistryTests
{
    [TestMethod]
    public void SupportedBanksShouldContainAllRequiredBanks()
    {
        // Arrange
        var requiredBanks = new[] { "Handelsbanken", "ICA-banken", "Nordea", "SEB", "Swedbank", "Avanza" };
        
        // Act
        var bankNames = BankRegistry.SupportedBanks.Select(b => b.Name).ToList();
        
        // Assert
        Assert.AreEqual(6, BankRegistry.SupportedBanks.Count);
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
    
    [DataTestMethod]
    [DataRow("Handelsbanken", "#003781")]
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
    
    [DataTestMethod]
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
