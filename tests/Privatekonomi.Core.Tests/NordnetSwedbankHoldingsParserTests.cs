using System.Text;
using Privatekonomi.Core.Services.Parsers;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class NordnetSwedbankHoldingsParserTests
{
    [TestMethod]
    public void CanParseRecognizesSemicolonHoldingsExport()
    {
        var parser = new NordnetSwedbankHoldingsParser();
        var csv = "Kontonummer;Värdepapper;ISIN;Antal;Marknadsvärde;GAV;Valuta\n"
            + "123456;Example AB;SE0000000001;10;1500,00;120,00;SEK";

        Assert.IsTrue(parser.CanParse(csv));
    }

    [TestMethod]
    public async Task ParseMapsQuantityValueAndAccount()
    {
        var parser = new NordnetSwedbankHoldingsParser();
        var csv = "Kontonummer;Värdepapper;ISIN;Antal;Marknadsvärde;GAV;Valuta\n"
            + "ISK 123;Example AB;SE0000000001;10;1500,00;120,00;SEK";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var investments = await parser.ParseAsync(stream);

        Assert.AreEqual(1, investments.Count);
        Assert.AreEqual("Example AB", investments[0].Name);
        Assert.AreEqual(10m, investments[0].Quantity);
        Assert.AreEqual(150m, investments[0].CurrentPrice);
        Assert.AreEqual(120m, investments[0].PurchasePrice);
        Assert.AreEqual("ISK 123", investments[0].AccountNumber);
        Assert.AreEqual("ISK", investments[0].AccountType);
    }

    [TestMethod]
    public void CanParseRejectsTransactionExport()
    {
        var parser = new NordnetSwedbankHoldingsParser();
        var csv = "Datum;Typ av transaktion;Belopp;Beskrivning";

        Assert.IsFalse(parser.CanParse(csv));
    }
}
