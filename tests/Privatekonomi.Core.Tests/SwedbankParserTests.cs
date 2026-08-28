using System.Text;
using Privatekonomi.Core.Services.Parsers;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class SwedbankParserTests
{
    // Example from the issue - Swedish format with comma separator and quoted fields
    private const string SwedishFormatCommaSeparated = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""50kr/onsda"",""50kr/onsda"",-50.00,9989.74
2,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""CSN Centrala stu"",""CSN Centrala stu"",-3277.00,10039.74
3,84525,1234567891,""e-sparkonto"",SEK,2025-11-11,2025-11-11,2025-11-12,""CSN LÅN NOV"",""CSN LÅN NOV"",3280.00,13316.74";

    // Swedish format with tab separator
    private const string SwedishFormatTabSeparated = @"Radnummer	Clearingnummer	Kontonummer	Produkt	Valuta	Bokföringsdag	Transaktionsdag	Valutadag	Referens	Beskrivning	Belopp	Bokfört saldo
1	84525	1234567891	e-sparkonto	SEK	2025-11-12	2025-11-12	2025-11-12	50kr/onsda	50kr/onsda	-50.00	9989.74
2	84525	1234567891	e-sparkonto	SEK	2025-11-12	2025-11-12	2025-11-12	CSN Centrala stu	CSN Centrala stu	-3277.00	10039.74
3	84525	1234567891	e-sparkonto	SEK	2025-11-11	2025-11-11	2025-11-12	CSN LÅN NOV	CSN LÅN NOV	3280.00	13316.74";

    // Old English format with semicolon separator
    private const string EnglishFormatSemicolonSeparated = @"""Row Type"";""Date"";""Debit/Credit"";""Details"";""Beneficiary/Payer"";""Amount"";""Currency"";""Balance"";""Client Account""
""20"";""15.11.2025"";""D"";""Matinköp ICA"";""ICA SUPERMARKET"";""245.50"";""SEK"";""8500.00"";""1111222333""
""20"";""14.11.2025"";""K"";""Lön"";""FÖRETAG AB"";""25000.00"";""SEK"";""33500.00"";""1111222333""";

    [TestMethod]
    public void SwedbankParser_CanParse_ReturnsTrueForSwedishCommaSeparated()
    {
        // Arrange
        var parser = new SwedbankParser();

        // Act
        var result = parser.CanParse(SwedishFormatCommaSeparated);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SwedbankParser_CanParse_ReturnsTrueForSwedishTabSeparated()
    {
        // Arrange
        var parser = new SwedbankParser();

        // Act
        var result = parser.CanParse(SwedishFormatTabSeparated);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SwedbankParser_CanParse_ReturnsTrueForEnglishFormat()
    {
        // Arrange
        var parser = new SwedbankParser();

        // Act
        var result = parser.CanParse(EnglishFormatSemicolonSeparated);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SwedbankParser_CanParse_ReturnsFalseForInvalidFormat()
    {
        // Arrange
        var parser = new SwedbankParser();
        var invalidCsv = "Name,Amount\nTest,100";

        // Act
        var result = parser.CanParse(invalidCsv);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ParsesSwedishCommaSeparatedCorrectly()
    {
        // Arrange
        var parser = new SwedbankParser();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SwedishFormatCommaSeparated));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.IsNotNull(transactions);
        Assert.AreEqual(3, transactions.Count);

        // Check first transaction (expense)
        var transaction1 = transactions[0];
        Assert.AreEqual(new DateTime(2025, 11, 12), transaction1.Date);
        Assert.AreEqual(50.00m, transaction1.Amount);
        Assert.IsFalse(transaction1.IsIncome); // Negative amount = expense
        Assert.AreEqual("50kr/onsda", transaction1.Description);

        // Check second transaction (expense)
        var transaction2 = transactions[1];
        Assert.AreEqual(new DateTime(2025, 11, 12), transaction2.Date);
        Assert.AreEqual(3277.00m, transaction2.Amount);
        Assert.IsFalse(transaction2.IsIncome);
        Assert.AreEqual("CSN Centrala stu", transaction2.Description);

        // Check third transaction (income)
        var transaction3 = transactions[2];
        Assert.AreEqual(new DateTime(2025, 11, 11), transaction3.Date);
        Assert.AreEqual(3280.00m, transaction3.Amount);
        Assert.IsTrue(transaction3.IsIncome); // Positive amount = income
        Assert.AreEqual("CSN LÅN NOV", transaction3.Description);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ParsesSwedishTabSeparatedCorrectly()
    {
        // Arrange
        var parser = new SwedbankParser();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SwedishFormatTabSeparated));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.IsNotNull(transactions);
        Assert.AreEqual(3, transactions.Count);

        // Verify same data as comma-separated
        Assert.AreEqual(50.00m, transactions[0].Amount);
        Assert.AreEqual(3277.00m, transactions[1].Amount);
        Assert.AreEqual(3280.00m, transactions[2].Amount);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ParsesEnglishFormatCorrectly()
    {
        // Arrange
        var parser = new SwedbankParser();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(EnglishFormatSemicolonSeparated));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.IsNotNull(transactions);
        Assert.AreEqual(2, transactions.Count);

        // Check expense transaction
        var expense = transactions[0];
        Assert.AreEqual(new DateTime(2025, 11, 15), expense.Date);
        Assert.AreEqual(245.50m, expense.Amount);
        Assert.IsFalse(expense.IsIncome); // D = Debit = expense
        Assert.AreEqual("ICA SUPERMARKET - Matinköp ICA", expense.Description);

        // Check income transaction
        var income = transactions[1];
        Assert.AreEqual(new DateTime(2025, 11, 14), income.Date);
        Assert.AreEqual(25000.00m, income.Amount);
        Assert.IsTrue(income.IsIncome); // K = Kredit = income
        Assert.AreEqual("FÖRETAG AB - Lön", income.Description);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_SkipsNonSEKTransactions()
    {
        // Arrange
        var parser = new SwedbankParser();
        var csvWithMultipleCurrencies = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""SEK Transaction"",""SEK Transaction"",-50.00,9989.74
2,84525,1234567891,""e-sparkonto"",USD,2025-11-12,2025-11-12,2025-11-12,""USD Transaction"",""USD Transaction"",-100.00,10039.74
3,84525,1234567891,""e-sparkonto"",SEK,2025-11-11,2025-11-11,2025-11-12,""Another SEK"",""Another SEK"",3280.00,13316.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithMultipleCurrencies));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(2, transactions.Count); // Only SEK transactions
        foreach (var transaction in transactions)
        {
            Assert.AreNotEqual("USD Transaction", transaction.Description);
        }
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_HandlesDecimalCommasCorrectly()
    {
        // Arrange
        var parser = new SwedbankParser();
        var csvWithDecimalComma = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""Test"",""Test"",""1234,56"",9989.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithDecimalComma));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual(1234.56m, transactions[0].Amount);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_SkipsRowsWithMissingDescription()
    {
        // Arrange
        var parser = new SwedbankParser();
        var csvWithMissingDescription = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,"""","""",50.00,9989.74
2,84525,1234567891,""e-sparkonto"",SEK,2025-11-11,2025-11-11,2025-11-12,""Valid"",""Valid"",100.00,10039.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithMissingDescription));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count); // Only the row with valid description
        Assert.AreEqual("Valid", transactions[0].Description);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_FallbackToReferenceIfDescriptionEmpty()
    {
        // Arrange
        var parser = new SwedbankParser();
        var csvWithReferenceOnly = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""Reference Text"","""",50.00,9989.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithReferenceOnly));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual("Reference Text", transactions[0].Description);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_TruncatesLongDescriptions()
    {
        // Arrange
        var parser = new SwedbankParser();
        var longDescription = new string('X', 600); // 600 characters
        var csvWithLongDescription = $@"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""{longDescription}"",""{longDescription}"",50.00,9989.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithLongDescription));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual(500, transactions[0].Description.Length); // Truncated to 500
    }

    [TestMethod]
    public void SwedbankParser_BankName_ReturnsSwedbank()
    {
        // Arrange
        var parser = new SwedbankParser();

        // Act & Assert
        Assert.AreEqual("Swedbank", parser.BankName);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ThrowsExceptionForInvalidFormat()
    {
        // Arrange
        var parser = new SwedbankParser();
        var invalidCsv = "Invalid,Header\nValue1,Value2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidCsv));

        // Act & Assert
        bool exceptionThrown = false;
        try
        {
            await parser.ParseAsync(stream);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }
        
        Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_HandlesEscapedQuotesInDescription()
    {
        // Arrange
        var parser = new SwedbankParser();
        var csvWithEscapedQuotes = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""Test """"quoted"""" text"",""Test """"quoted"""" text"",50.00,9989.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithEscapedQuotes));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count);
        StringAssert.Contains(transactions[0].Description, "quoted");
    }

    [TestMethod]
    public void SwedbankParser_CanParse_ReturnsTrueWithMetadataLine()
    {
        // Arrange - Example from issue with metadata on row 1, header on row 2
        var parser = new SwedbankParser();
        var csvWithMetadata = @"* Transaktioner Period 2024-01-01–2025-11-12 Skapad 2025-11-13 07:35 CET
Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,9141231231,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""50kr/onsda"",""50kr/onsda"",-50.00,9989.74
2,84525,9141231231,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""CSN Centrala stu"",""CSN Centrala stu"",-3277.00,10039.74";

        // Act
        var result = parser.CanParse(csvWithMetadata);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ParsesCorrectlyWithMetadataLine()
    {
        // Arrange - Example from issue with metadata on row 1, header on row 2
        var parser = new SwedbankParser();
        var csvWithMetadata = @"* Transaktioner Period 2024-01-01–2025-11-12 Skapad 2025-11-13 07:35 CET
Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,9141231231,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""50kr/onsda"",""50kr/onsda"",-50.00,9989.74
2,84525,9141231231,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""CSN Centrala stu"",""CSN Centrala stu"",-3277.00,10039.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithMetadata));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.IsNotNull(transactions);
        Assert.AreEqual(2, transactions.Count);

        // Check first transaction (expense)
        var transaction1 = transactions[0];
        Assert.AreEqual(new DateTime(2025, 11, 12), transaction1.Date);
        Assert.AreEqual(50.00m, transaction1.Amount);
        Assert.IsFalse(transaction1.IsIncome); // Negative amount = expense
        Assert.AreEqual("50kr/onsda", transaction1.Description);

        // Check second transaction (expense)
        var transaction2 = transactions[1];
        Assert.AreEqual(new DateTime(2025, 11, 12), transaction2.Date);
        Assert.AreEqual(3277.00m, transaction2.Amount);
        Assert.IsFalse(transaction2.IsIncome);
        Assert.AreEqual("CSN Centrala stu", transaction2.Description);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_HandlesMultipleMetadataLines()
    {
        // Arrange - Test with multiple metadata lines before header
        var parser = new SwedbankParser();
        var csvWithMultipleMetadata = @"* Export från Swedbank
* Transaktioner Period 2024-01-01–2025-11-12
Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,9141231231,""e-sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""Test"",""Test"",-100.00,9989.74";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithMultipleMetadata));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual(100.00m, transactions[0].Amount);
        Assert.AreEqual("Test", transactions[0].Description);
    }

    [TestMethod]
    public void SwedbankParser_CanParse_ReturnsTrueWithMetadataLineEnglishFormat()
    {
        // Arrange - Old English format with metadata line
        var parser = new SwedbankParser();
        var csvWithMetadata = @"Export from Swedbank
""Row Type"";""Date"";""Debit/Credit"";""Details"";""Beneficiary/Payer"";""Amount"";""Currency"";""Balance"";""Client Account""
""20"";""15.11.2025"";""D"";""Matinköp ICA"";""ICA SUPERMARKET"";""245.50"";""SEK"";""8500.00"";""1111222333""";

        // Act
        var result = parser.CanParse(csvWithMetadata);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ParsesEnglishFormatWithMetadataLine()
    {
        // Arrange - Old English format with metadata line
        var parser = new SwedbankParser();
        var csvWithMetadata = @"Export from Swedbank - Account 123456
""Row Type"";""Date"";""Debit/Credit"";""Details"";""Beneficiary/Payer"";""Amount"";""Currency"";""Balance"";""Client Account""
""20"";""15.11.2025"";""D"";""Matinköp ICA"";""ICA SUPERMARKET"";""245.50"";""SEK"";""8500.00"";""1111222333""
""20"";""14.11.2025"";""K"";""Lön"";""FÖRETAG AB"";""25000.00"";""SEK"";""33500.00"";""1111222333""";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithMetadata));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(2, transactions.Count);
        Assert.AreEqual(245.50m, transactions[0].Amount);
        Assert.IsFalse(transactions[0].IsIncome);
        Assert.AreEqual(25000.00m, transactions[1].Amount);
        Assert.IsTrue(transactions[1].IsIncome);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ExtractsClearingAndAccountNumberFromSwedishFormat()
    {
        // Arrange
        var parser = new SwedbankParser();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SwedishFormatCommaSeparated));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(3, transactions.Count);
        foreach (var transaction in transactions)
        {
            Assert.AreEqual("84525", transaction.ClearingNumber);
            Assert.AreEqual("1234567891", transaction.AccountNumber);
        }
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ExtractsClearingAndAccountNumberFromTabSeparatedFormat()
    {
        // Arrange
        var parser = new SwedbankParser();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(SwedishFormatTabSeparated));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(3, transactions.Count);
        foreach (var transaction in transactions)
        {
            Assert.AreEqual("84525", transaction.ClearingNumber);
            Assert.AreEqual("1234567891", transaction.AccountNumber);
        }
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ExtractsAccountNumberFromEnglishFormat()
    {
        // Arrange
        var parser = new SwedbankParser();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(EnglishFormatSemicolonSeparated));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(2, transactions.Count);
        foreach (var transaction in transactions)
        {
            Assert.AreEqual("1111222333", transaction.AccountNumber);
            Assert.IsNull(transaction.ClearingNumber); // Old format has no separate clearing number column
        }
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_HandlesMultipleAccountsInSameFile()
    {
        // Arrange - CSV with transactions for two different accounts
        var parser = new SwedbankParser();
        var csvWithMultipleAccounts = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,""lönekonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""Lön"",""Lön"",25000.00,25000.00
2,84525,9876543210,""sparkonto"",SEK,2025-11-12,2025-11-12,2025-11-12,""Överföring"",""Överföring"",-5000.00,20000.00";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvWithMultipleAccounts));

        // Act
        var parseResult = await parser.ParseAsync(stream);
        var transactions = parseResult.Transactions;

        // Assert
        Assert.AreEqual(2, transactions.Count);
        Assert.AreEqual("1234567891", transactions[0].AccountNumber);
        Assert.AreEqual("84525", transactions[0].ClearingNumber);
        Assert.AreEqual("9876543210", transactions[1].AccountNumber);
        Assert.AreEqual("84525", transactions[1].ClearingNumber);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_EmitsInvalidDateWarning()
    {
        var parser = new SwedbankParser();
        var csv = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,lönekonto,SEK,not-a-date,2025-11-12,2025-11-12,REF1,Beskrivning,-100.00,900.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await parser.ParseAsync(stream);

        Assert.AreEqual(0, result.Transactions.Count, "No transactions should be imported for invalid date");
        Assert.AreEqual(1, result.Warnings.Count, "Expected one warning");
        Assert.AreEqual("InvalidDate", result.Warnings[0].WarningType);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_EmitsInvalidAmountWarning()
    {
        var parser = new SwedbankParser();
        var csv = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,lönekonto,SEK,2025-11-12,2025-11-12,2025-11-12,REF1,Köp ICA,NOT_A_NUMBER,900.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await parser.ParseAsync(stream);

        Assert.AreEqual(0, result.Transactions.Count, "No transactions should be imported for invalid amount");
        Assert.AreEqual(1, result.Warnings.Count, "Expected one warning");
        Assert.AreEqual("InvalidAmount", result.Warnings[0].WarningType);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_EmitsMissingDescriptionWarning()
    {
        var parser = new SwedbankParser();
        // Both Beskrivning and Referens are empty
        var csv = "Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokf\u00f6ringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bok\u00f6rt saldo\n" +
                  "1,84525,1234567891,l\u00f6nekonto,SEK,2025-11-12,2025-11-12,2025-11-12,,,,-100.00,900.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await parser.ParseAsync(stream);

        Assert.AreEqual(0, result.Transactions.Count);
        Assert.AreEqual(1, result.Warnings.Count);
        Assert.AreEqual("MissingDescription", result.Warnings[0].WarningType);
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_EmitsUnsupportedCurrencyWarning()
    {
        var parser = new SwedbankParser();
        var csv = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,lönekonto,USD,2025-11-12,2025-11-12,2025-11-12,REF1,Köp,-100.00,900.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await parser.ParseAsync(stream);

        Assert.AreEqual(0, result.Transactions.Count, "USD transaction should be skipped");
        Assert.AreEqual(1, result.Warnings.Count);
        Assert.AreEqual("UnsupportedCurrency", result.Warnings[0].WarningType);
        StringAssert.Contains(result.Warnings[0].Message, "USD");
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_WarningIncludesRawData()
    {
        var parser = new SwedbankParser();
        var csv = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,lönekonto,SEK,BAD_DATE,2025-11-12,2025-11-12,REF1,Beskrivning,-100.00,900.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await parser.ParseAsync(stream);

        Assert.AreEqual(1, result.Warnings.Count);
        Assert.IsFalse(string.IsNullOrEmpty(result.Warnings[0].RawData), "Warning should include raw row data");
    }

    [TestMethod]
    public async Task SwedbankParser_ParseAsync_ValidRowsImportedDespiteWarnings()
    {
        var parser = new SwedbankParser();
        // First row valid, second row has bad date
        var csv = @"Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
1,84525,1234567891,lönekonto,SEK,2025-11-12,2025-11-12,2025-11-12,REF1,ICA Butiken,-100.00,900.00
2,84525,1234567891,lönekonto,SEK,BAD_DATE,2025-11-12,2025-11-12,REF2,Lön,25000.00,25900.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await parser.ParseAsync(stream);

        Assert.AreEqual(1, result.Transactions.Count, "Valid row should be imported");
        Assert.AreEqual(1, result.Warnings.Count, "Invalid row should produce warning");
        Assert.AreEqual("InvalidDate", result.Warnings[0].WarningType);
    }
}
