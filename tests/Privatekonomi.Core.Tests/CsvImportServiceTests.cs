using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using System.Text;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class CsvImportServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly CsvImportService _service;
    private const string TestUserId = "user-abc123";

    // Minimal valid Swedbank CSV (Swedish/CSN format)
    private const string ValidSwedbankCsv = """
        Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
        1,84525,1234567891,e-sparkonto,SEK,2024-03-01,2024-03-01,2024-03-01,REF1,ICA-Maxi,-250.00,9750.00
        2,84525,1234567891,e-sparkonto,SEK,2024-03-02,2024-03-02,2024-03-02,REF2,Lön,25000.00,34750.00
        """;

    // Minimal valid ICA-banken CSV
    private const string ValidIcaBankenCsv = """
        Datum;Belopp;Beskrivning;Saldo
        2024-03-01;-150,00;Mataffär;9850,00
        2024-03-02;2500,00;Swish in;12350,00
        """;

    public CsvImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _service = new CsvImportService(_context, NullLogger<CsvImportService>.Instance);
    }

    [TestCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    // -------------------------------------------------------------------------
    // DetectBank
    // -------------------------------------------------------------------------

    [TestMethod]
    public void DetectBank_SwedbankCsv_ReturnsSwedbankName()
    {
        var bytes = Encoding.UTF8.GetBytes(ValidSwedbankCsv);
        var result = _service.DetectBank(bytes);
        Assert.AreEqual("Swedbank", result);
    }

    [TestMethod]
    public void DetectBank_IcaBankenCsv_ReturnsIcaBankenName()
    {
        var bytes = Encoding.UTF8.GetBytes(ValidIcaBankenCsv);
        var result = _service.DetectBank(bytes);
        Assert.AreEqual("ICA-banken", result);
    }

    [TestMethod]
    public void DetectBank_UnknownContent_ReturnsNull()
    {
        var bytes = Encoding.UTF8.GetBytes("Col1,Col2,Col3\n1,2,3\n");
        var result = _service.DetectBank(bytes);
        Assert.IsNull(result);
    }

    // -------------------------------------------------------------------------
    // PreviewCsvAsync — duplicate detection scoped to user
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task PreviewCsvAsync_NoExistingTransactions_NoDuplicates()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidSwedbankCsv));
        var result = await _service.PreviewCsvAsync(stream, "Swedbank", TestUserId);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.DuplicateCount);
        Assert.AreEqual(2, result.ImportedCount);
    }

    [TestMethod]
    public async Task PreviewCsvAsync_ExistingTransactionSameUser_DetectedAsDuplicate()
    {
        // Seed an identical transaction for the same user
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 3, 1),
            Amount = 250.00m,
            IsIncome = false,
            Description = "ICA-Maxi",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidSwedbankCsv));
        var result = await _service.PreviewCsvAsync(stream, "Swedbank", TestUserId);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.DuplicateCount);
    }

    [TestMethod]
    public async Task PreviewCsvAsync_ExistingTransactionDifferentUser_NotDuplicate()
    {
        // Seed an identical transaction but for a DIFFERENT user
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 3, 1),
            Amount = 250.00m,
            IsIncome = false,
            Description = "ICA-Maxi",
            UserId = "other-user-456",
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidSwedbankCsv));
        var result = await _service.PreviewCsvAsync(stream, "Swedbank", TestUserId);

        Assert.IsTrue(result.Success);
        // The same transaction belonging to a different user must NOT be treated as a duplicate
        Assert.AreEqual(0, result.DuplicateCount);
    }

    // -------------------------------------------------------------------------
    // Validation warnings — old date and zero amount
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task PreviewCsvAsync_VeryOldDate_EmitsOldDateWarning()
    {
        var oldDate = DateTime.Now.AddYears(-11).ToString("yyyy-MM-dd");
        var csv = $"""
            Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
            1,84525,1234567891,e-sparkonto,SEK,{oldDate},{oldDate},{oldDate},REF,Gammal transaktion,-100.00,900.00
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await _service.PreviewCsvAsync(stream, "Swedbank", TestUserId);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(
            result.Warnings.Any(w => w.WarningType == "OldDate"),
            "Expected an OldDate warning for a transaction more than 10 years old");
    }

    [TestMethod]
    public async Task PreviewCsvAsync_ZeroAmount_EmitsZeroAmountWarning()
    {
        var csv = $"""
            Radnummer,Clearingnummer,Kontonummer,Produkt,Valuta,Bokföringsdag,Transaktionsdag,Valutadag,Referens,Beskrivning,Belopp,Bokfört saldo
            1,84525,1234567891,e-sparkonto,SEK,2024-03-01,2024-03-01,2024-03-01,REF,Nolltransaktion,0.00,1000.00
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await _service.PreviewCsvAsync(stream, "Swedbank", TestUserId);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(
            result.Warnings.Any(w => w.WarningType == "ZeroAmount"),
            "Expected a ZeroAmount warning for a transaction with amount = 0");
    }

    // -------------------------------------------------------------------------
    // BankSource auto-create — display name includes account number
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ImportWithJobAsync_NewAccount_BankSourceNameIncludesAccountNumber()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidSwedbankCsv));
        var (result, _) = await _service.ImportWithJobAsync(
            stream, "Swedbank", "test.csv", 1024, TestUserId);

        Assert.IsTrue(result.Success);

        var bankSource = await _context.BankSources
            .Where(b => b.UserId == TestUserId)
            .FirstOrDefaultAsync();

        Assert.IsNotNull(bankSource, "A BankSource should have been auto-created");
        Assert.IsTrue(
            bankSource.Name.Contains("84525") && bankSource.Name.Contains("1234567891"),
            $"BankSource name should include clearing and account number, got: '{bankSource.Name}'");
    }
}
