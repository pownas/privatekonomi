using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public interface IHouseholdBalanceSheetService
{
    Task<IReadOnlyList<Household>> GetAvailableSharedHouseholdsAsync();
    Task<HouseholdBalanceSheetData?> GetSharedBalanceSheetAsync(int householdId);
    Task LinkCurrentUserToMemberAsync(int householdId, int householdMemberId);
    Task SetBalanceSheetSharingAsync(int householdId, bool enabled);
}

public sealed record HouseholdBalanceSheetData(
    IReadOnlyList<BankSource> BankSources,
    IReadOnlyList<Investment> Investments,
    IReadOnlyList<Asset> Assets,
    IReadOnlyList<Loan> Loans,
    IReadOnlyList<Transaction> Transactions);
