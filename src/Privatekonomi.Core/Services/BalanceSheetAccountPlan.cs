using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public sealed record BalanceSheetAccountCode(string Number, string Name);

/// <summary>
/// Supplies suggested BAS account codes for the household balance sheet.
/// A code configured on a bank account takes precedence over the suggested code.
/// </summary>
public static class BalanceSheetAccountPlan
{
    public static BalanceSheetAccountCode ForBankSource(BankSource account)
    {
        var suggested = Normalize(account.AccountType) switch
        {
            "cash" => new BalanceSheetAccountCode("1910", "Kassa"),
            "checking" => new BalanceSheetAccountCode("1930", "Företagskonto/checkkonto"),
            "savings" => new BalanceSheetAccountCode("1940", "Övriga bankkonton"),
            "credit_card" => new BalanceSheetAccountCode("2890", "Övriga kortfristiga skulder"),
            "investment" => new BalanceSheetAccountCode("1810", "Andelar i börsnoterade företag"),
            "pension" => new BalanceSheetAccountCode("1350", "Andelar och värdepapper"),
            "loan" => new BalanceSheetAccountCode("2350", "Övriga skulder till kreditinstitut"),
            _ => new BalanceSheetAccountCode("1930", "Bankkonto")
        };

        return UseConfiguredCode(account.ChartOfAccountsCode, suggested);
    }

    public static BalanceSheetAccountCode ForInvestment(Investment investment)
    {
        if (investment.BankSource is not null && !string.IsNullOrWhiteSpace(investment.BankSource.ChartOfAccountsCode))
            return ForBankSource(investment.BankSource);

        return new("1810", "Andelar i börsnoterade företag");
    }

    public static BalanceSheetAccountCode ForAsset(Asset asset)
    {
        var type = Normalize(asset.Type);
        if (ContainsAny(type, "mark", "tomt"))
            return new("1130", "Mark och markanläggningar");
        if (ContainsAny(type, "fastighet", "bostad", "hus", "villa"))
            return new("1110", "Byggnader");
        if (ContainsAny(type, "fordon", "bil", "motorcykel", "båt"))
            return new("1240", "Bilar och andra transportmedel");
        if (ContainsAny(type, "elektronik", "dator", "mobil"))
            return new("1250", "Datorer och kringutrustning");
        if (ContainsAny(type, "möbel", "inventarie"))
            return new("1220", "Inventarier och verktyg");

        return new("1290", "Övriga materiella anläggningstillgångar");
    }

    public static BalanceSheetAccountCode ForLoan(Loan loan)
    {
        var type = Normalize(loan.Type);
        if (ContainsAny(type, "kreditkort", "avbetalning", "bnpl", "kortfristig"))
            return new("2890", "Övriga kortfristiga skulder");
        if (ContainsAny(type, "bolån", "banklån", "kreditinstitut"))
            return new("2350", "Övriga skulder till kreditinstitut");

        return new("2390", "Övriga långfristiga skulder");
    }

    private static BalanceSheetAccountCode UseConfiguredCode(string? configuredCode, BalanceSheetAccountCode suggested) =>
        string.IsNullOrWhiteSpace(configuredCode)
            ? suggested
            : new(configuredCode.Trim(), "Egen kontokoppling");

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool ContainsAny(string value, params string[] fragments) =>
        fragments.Any(value.Contains);
}
