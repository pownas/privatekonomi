using System.Text;
using Privatekonomi.Core.Services.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class OfxParserTests
{
    private readonly OfxParser _parser;

    public OfxParserTests()
    {
        _parser = new OfxParser();
    }

    // Sample OFX file content (SGML style - older format)
    private const string SgmlOfxContent = @"OFXHEADER:100
DATA:OFXSGML
VERSION:102
SECURITY:NONE
ENCODING:USASCII
CHARSET:1252
COMPRESSION:NONE
OLDFILEUID:NONE
NEWFILEUID:NONE

<OFX>
<SIGNONMSGSRSV1>
<SONRS>
<STATUS>
<CODE>0
<SEVERITY>INFO
</STATUS>
<DTSERVER>20250115120000
<LANGUAGE>ENG
</SONRS>
</SIGNONMSGSRSV1>
<BANKMSGSRSV1>
<STMTTRNRS>
<STMTRS>
<CURDEF>SEK
<BANKACCTFROM>
<BANKID>1234
<ACCTID>987654321
<ACCTTYPE>CHECKING
</BANKACCTFROM>
<BANKTRANLIST>
<DTSTART>20250101000000
<DTEND>20250115235959
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20250110000000
<TRNAMT>-125.50
<FITID>20250110001
<NAME>ICA MAXI STOCKHOLM
<MEMO>Kortköp
</STMTTRN>
<STMTTRN>
<TRNTYPE>CREDIT
<DTPOSTED>20250115000000
<TRNAMT>3500.00
<FITID>20250115001
<NAME>Lön
<MEMO>Månadslön
</STMTTRN>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20250112000000
<TRNAMT>-89.00
<FITID>20250112001
<NAME>COOP FORUM
</STMTTRN>
</BANKTRANLIST>
<LEDGERBAL>
<BALAMT>8742.30
<DTASOF>20250115120000
</LEDGERBAL>
</STMTRS>
</STMTTRNRS>
</BANKMSGSRSV1>
</OFX>";

    // Sample OFX file content (XML style - newer format)
    private const string XmlOfxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?OFX OFXHEADER=""200"" VERSION=""220"" SECURITY=""NONE"" OLDFILEUID=""NONE"" NEWFILEUID=""NONE""?>
<OFX>
<SIGNONMSGSRSV1>
<SONRS>
<STATUS>
<CODE>0</CODE>
<SEVERITY>INFO</SEVERITY>
</STATUS>
<DTSERVER>20250115120000</DTSERVER>
<LANGUAGE>ENG</LANGUAGE>
</SONRS>
</SIGNONMSGSRSV1>
<BANKMSGSRSV1>
<STMTTRNRS>
<STMTRS>
<CURDEF>SEK</CURDEF>
<BANKACCTFROM>
<BANKID>1234</BANKID>
<ACCTID>987654321</ACCTID>
<ACCTTYPE>CHECKING</ACCTTYPE>
</BANKACCTFROM>
<BANKTRANLIST>
<DTSTART>20250101000000</DTSTART>
<DTEND>20250115235959</DTEND>
<STMTTRN>
<TRNTYPE>DEBIT</TRNTYPE>
<DTPOSTED>20250110000000</DTPOSTED>
<TRNAMT>-125.50</TRNAMT>
<FITID>20250110001</FITID>
<NAME>ICA MAXI STOCKHOLM</NAME>
<MEMO>Kortköp</MEMO>
</STMTTRN>
<STMTTRN>
<TRNTYPE>CREDIT</TRNTYPE>
<DTPOSTED>20250115000000</DTPOSTED>
<TRNAMT>3500.00</TRNAMT>
<FITID>20250115001</FITID>
<NAME>Lön</NAME>
<MEMO>Månadslön</MEMO>
</STMTTRN>
</BANKTRANLIST>
</STMTRS>
</STMTTRNRS>
</BANKMSGSRSV1>
</OFX>";

    [TestMethod]
    public void BankName_ReturnsOfxAllman()
    {
        Assert.AreEqual("OFX (Allmän)", _parser.BankName);
    }

    [TestMethod]
    public void CanParse_ReturnsTrueForOfxHeader()
    {
        var result = _parser.CanParse(SgmlOfxContent);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CanParse_ReturnsTrueForXmlOfx()
    {
        var result = _parser.CanParse(XmlOfxContent);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CanParse_ReturnsTrueForOfxTag()
    {
        var content = "<OFX><SIGNONMSGSRSV1></SIGNONMSGSRSV1></OFX>";
        var result = _parser.CanParse(content);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CanParse_ReturnsFalseForCsv()
    {
        var csvContent = "Datum;Belopp;Beskrivning\n2025-01-15;-125,50;ICA";
        var result = _parser.CanParse(csvContent);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ParseAsync_ParsesSgmlOfxCorrectly()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.IsNotNull(transactions);
        Assert.AreEqual(3, transactions.Count);

        // Check first transaction (expense)
        var expense = transactions[0];
        Assert.AreEqual(new DateTime(2025, 1, 10), expense.Date.Date);
        Assert.AreEqual(125.50m, expense.Amount);
        Assert.IsFalse(expense.IsIncome);
        StringAssert.Contains(expense.Description, "ICA MAXI STOCKHOLM");

        // Check second transaction (income)
        var income = transactions[1];
        Assert.AreEqual(new DateTime(2025, 1, 15), income.Date.Date);
        Assert.AreEqual(3500.00m, income.Amount);
        Assert.IsTrue(income.IsIncome);
        StringAssert.Contains(income.Description, "Lön");
    }

    [TestMethod]
    public async Task ParseAsync_ParsesXmlOfxCorrectly()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(XmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.IsNotNull(transactions);
        Assert.AreEqual(2, transactions.Count);

        // Check expense
        var expense = transactions.First(t => !t.IsIncome);
        Assert.AreEqual(new DateTime(2025, 1, 10), expense.Date.Date);
        Assert.AreEqual(125.50m, expense.Amount);

        // Check income
        var income = transactions.First(t => t.IsIncome);
        Assert.AreEqual(new DateTime(2025, 1, 15), income.Date.Date);
        Assert.AreEqual(3500.00m, income.Amount);
    }

    [TestMethod]
    public async Task ParseAsync_SetsImportedFlagTrue()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        foreach (var t in transactions) { Assert.IsTrue(t.Imported); }
    }

    [TestMethod]
    public async Task ParseAsync_SetsImportSourceToOfx()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        foreach (var t in transactions) { Assert.AreEqual("OFX Import", t.ImportSource); }
    }

    [TestMethod]
    public async Task ParseAsync_SetsCurrencyToSek()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        foreach (var t in transactions) { Assert.AreEqual("SEK", t.Currency); }
    }

    [TestMethod]
    public async Task ParseAsync_CombinesNameAndMemo()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        var icaTransaction = transactions.First(t => t.Description.Contains("ICA"));
        StringAssert.Contains(icaTransaction.Description, "Kortköp");
    }

    [TestMethod]
    public async Task ParseAsync_HandlesMissingMemo()
    {
        // Arrange - The COOP transaction has no MEMO
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        var coopTransaction = transactions.FirstOrDefault(t => t.Description.Contains("COOP"));
        Assert.IsNotNull(coopTransaction);
        Assert.AreEqual("COOP FORUM", coopTransaction.Description);
    }

    [TestMethod]
    public async Task ParseAsync_HandlesEmptyFile()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(0, transactions.Count());
    }

    [TestMethod]
    public async Task ParseAsync_HandlesFileWithNoTransactions()
    {
        // Arrange
        var ofxWithNoTransactions = @"OFXHEADER:100
DATA:OFXSGML
VERSION:102
<OFX>
<SIGNONMSGSRSV1>
<SONRS>
<STATUS>
<CODE>0
<SEVERITY>INFO
</STATUS>
</SONRS>
</SIGNONMSGSRSV1>
</OFX>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofxWithNoTransactions));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(0, transactions.Count());
    }

    [TestMethod]
    public async Task ParseAsync_CorrectlyIdentifiesIncomeVsExpense()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SgmlOfxContent));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        var expenses = transactions.Where(t => !t.IsIncome).ToList();
        var incomes = transactions.Where(t => t.IsIncome).ToList();

        Assert.AreEqual(2, expenses.Count); // ICA and COOP
        Assert.AreEqual(1, incomes.Count()); // Lön
    }

    [TestMethod]
    public async Task ParseAsync_HandlesDateWithTimezone()
    {
        // Arrange
        var ofxWithTimezone = @"OFXHEADER:100
DATA:OFXSGML
VERSION:102
<OFX>
<BANKMSGSRSV1>
<STMTTRNRS>
<STMTRS>
<BANKTRANLIST>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20250110120000[-5:EST]
<TRNAMT>-100.00
<FITID>12345
<NAME>Test Transaction
</STMTTRN>
</BANKTRANLIST>
</STMTRS>
</STMTTRNRS>
</BANKMSGSRSV1>
</OFX>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofxWithTimezone));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count());
        Assert.AreEqual(new DateTime(2025, 1, 10), transactions[0].Date.Date);
    }

    [TestMethod]
    public async Task ParseAsync_HandlesDecimalAmountsWithComma()
    {
        // Arrange - Swedish style with comma decimal separator
        var ofxWithComma = @"OFXHEADER:100
DATA:OFXSGML
VERSION:102
<OFX>
<BANKMSGSRSV1>
<STMTTRNRS>
<STMTRS>
<BANKTRANLIST>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20250110
<TRNAMT>-125,50
<FITID>12345
<NAME>Test
</STMTTRN>
</BANKTRANLIST>
</STMTRS>
</STMTTRNRS>
</BANKMSGSRSV1>
</OFX>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofxWithComma));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count());
        Assert.AreEqual(125.50m, transactions[0].Amount);
    }

    [TestMethod]
    public async Task ParseAsync_SkipsInvalidTransactions()
    {
        // Arrange - Transaction without required fields
        var ofxWithInvalid = @"OFXHEADER:100
DATA:OFXSGML
VERSION:102
<OFX>
<BANKMSGSRSV1>
<STMTTRNRS>
<STMTRS>
<BANKTRANLIST>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>invalid-date
<TRNAMT>-100.00
<NAME>Invalid Date Transaction
</STMTTRN>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20250110
<TRNAMT>-100.00
<NAME>Valid Transaction
</STMTTRN>
</BANKTRANLIST>
</STMTRS>
</STMTTRNRS>
</BANKMSGSRSV1>
</OFX>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofxWithInvalid));

        // Act
        var parseResult = await _parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count());
        StringAssert.Contains(transactions[0].Description, "Valid Transaction");
    }
}
