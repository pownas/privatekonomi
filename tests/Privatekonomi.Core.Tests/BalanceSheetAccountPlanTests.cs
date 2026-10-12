using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BalanceSheetAccountPlanTests
{
    [TestMethod]
    public void BankSourceUsesSuggestedCodeOrExplicitUserMapping()
    {
        var defaultMapping = BalanceSheetAccountPlan.ForBankSource(new BankSource { AccountType = "checking" });
        var customMapping = BalanceSheetAccountPlan.ForBankSource(new BankSource
        {
            AccountType = "checking",
            ChartOfAccountsCode = "1945"
        });

        Assert.AreEqual("1930", defaultMapping.Number);
        Assert.AreEqual("1945", customMapping.Number);
        Assert.AreEqual("Egen kontokoppling", customMapping.Name);
    }

    [TestMethod]
    public void AssetTypesReceiveRelevantSuggestedCodes()
    {
        Assert.AreEqual("1110", BalanceSheetAccountPlan.ForAsset(new Asset { Type = "Fastighet" }).Number);
        Assert.AreEqual("1130", BalanceSheetAccountPlan.ForAsset(new Asset { Type = "Mark" }).Number);
        Assert.AreEqual("1240", BalanceSheetAccountPlan.ForAsset(new Asset { Type = "Fordon" }).Number);
        Assert.AreEqual("1250", BalanceSheetAccountPlan.ForAsset(new Asset { Type = "Elektronik" }).Number);
        Assert.AreEqual("1290", BalanceSheetAccountPlan.ForAsset(new Asset { Type = "Samlarföremål" }).Number);
    }

    [TestMethod]
    public void LoansAreSeparatedIntoShortAndLongTermGroups()
    {
        Assert.AreEqual("2890", BalanceSheetAccountPlan.ForLoan(new Loan { Type = "Kreditkort" }).Number);
        Assert.AreEqual("2350", BalanceSheetAccountPlan.ForLoan(new Loan { Type = "Bolån" }).Number);
        Assert.AreEqual("2390", BalanceSheetAccountPlan.ForLoan(new Loan { Type = "CSN-lån" }).Number);
    }
}
