using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public sealed record BalanceSheetAccountCode(string Number, string Name);

/// <summary>
/// Supplies suggested BAS account codes for the household balance sheet.
/// A code configured on a bank account takes precedence over the suggested code.
/// </summary>
public static class BalanceSheetAccountPlan
{
    public static bool IsLiabilityBankSource(BankSource account) =>
        Normalize(account.AccountType) is "credit_card" or "loan";

    public static decimal GetBankSourceLiabilityValue(BankSource account) =>
        IsLiabilityBankSource(account) ? Math.Abs(account.CurrentBalance) : 0;

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
        if (!string.IsNullOrWhiteSpace(investment.ChartOfAccountsCode))
            return UseConfiguredCode(investment.ChartOfAccountsCode, new("1810", "Andelar i börsnoterade företag"));

        if (investment.BankSource is not null && !string.IsNullOrWhiteSpace(investment.BankSource.ChartOfAccountsCode))
            return ForBankSource(investment.BankSource);

        return new("1810", "Andelar i börsnoterade företag");
    }

    public static BalanceSheetAccountCode ForAsset(Asset asset)
    {
        var type = Normalize(asset.Type);
        var suggested = ContainsAny(type, "mark", "tomt")
            ? new BalanceSheetAccountCode("1130", "Mark och markanläggningar")
            : ContainsAny(type, "fastighet", "bostad", "hus", "villa")
                ? new BalanceSheetAccountCode("1110", "Byggnader")
                : ContainsAny(type, "fordon", "bil", "motorcykel", "båt")
                    ? new BalanceSheetAccountCode("1240", "Bilar och andra transportmedel")
                    : ContainsAny(type, "elektronik", "dator", "mobil")
                        ? new BalanceSheetAccountCode("1250", "Datorer och kringutrustning")
                        : ContainsAny(type, "möbel", "inventarie")
                            ? new BalanceSheetAccountCode("1220", "Inventarier och verktyg")
                            : new BalanceSheetAccountCode("1290", "Övriga materiella anläggningstillgångar");

        return UseConfiguredCode(asset.ChartOfAccountsCode, suggested);
    }

    public static BalanceSheetAccountCode ForLoan(Loan loan)
    {
        var type = Normalize(loan.Type);
        var suggested = ContainsAny(type, "kreditkort", "avbetalning", "bnpl", "kortfristig")
            ? new BalanceSheetAccountCode("2890", "Övriga kortfristiga skulder")
            : ContainsAny(type, "bolån", "banklån", "kreditinstitut")
                ? new BalanceSheetAccountCode("2350", "Övriga skulder till kreditinstitut")
                : new BalanceSheetAccountCode("2390", "Övriga långfristiga skulder");

        return UseConfiguredCode(loan.ChartOfAccountsCode, suggested);
    }

    private static BalanceSheetAccountCode UseConfiguredCode(string? configuredCode, BalanceSheetAccountCode suggested) =>
        string.IsNullOrWhiteSpace(configuredCode)
            ? suggested
            : new(configuredCode.Trim(), "Egen kontokoppling");

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool ContainsAny(string value, params string[] fragments) =>
        fragments.Any(value.Contains);
}
