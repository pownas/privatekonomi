using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public sealed class HouseholdBalanceSheetService : IHouseholdBalanceSheetService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService _currentUserService;

    public HouseholdBalanceSheetService(PrivatekonomyContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<Household>> GetAvailableSharedHouseholdsAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            return [];

        return await _context.Households
            .AsNoTracking()
            .Where(h => h.ShareBalanceSheet && h.Members.Any(m => m.IsActive && m.UserId == userId))
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<HouseholdBalanceSheetData?> GetSharedBalanceSheetAsync(int householdId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            return null;

        var household = await _context.Households
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.HouseholdId == householdId && h.ShareBalanceSheet &&
                                      h.Members.Any(m => m.IsActive && m.UserId == userId));
        if (household is null)
            return null;

        var userIds = household.Members
            .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.UserId))
            .Select(m => m.UserId!)
            .Distinct()
            .ToArray();

        var bankSources = await _context.BankSources
            .AsNoTracking()
            .Include(account => account.Transactions.Where(transaction => userIds.Contains(transaction.UserId!)))
            .Where(account => userIds.Contains(account.UserId!))
            .OrderBy(account => account.Name)
            .ToListAsync();
        var investments = await _context.Investments
            .AsNoTracking()
            .Include(investment => investment.BankSource)
            .Where(investment => userIds.Contains(investment.UserId!))
            .OrderBy(investment => investment.Name)
            .ToListAsync();
        foreach (var investment in investments.Where(investment =>
                     investment.BankSource is not null &&
                     !userIds.Contains(investment.BankSource.UserId ?? string.Empty)))
        {
            investment.BankSource = null;
        }
        var assets = await _context.Assets
            .AsNoTracking()
            .Where(asset => userIds.Contains(asset.UserId!))
            .OrderBy(asset => asset.Name)
            .ToListAsync();
        var loans = await _context.Loans
            .AsNoTracking()
            .Where(loan => userIds.Contains(loan.UserId!))
            .OrderBy(loan => loan.Name)
            .ToListAsync();
        var transactions = await _context.Transactions
            .AsNoTracking()
            .Where(transaction => userIds.Contains(transaction.UserId!))
            .ToListAsync();

        return new HouseholdBalanceSheetData(bankSources, investments, assets, loans, transactions);
    }

    public async Task SetBalanceSheetSharingAsync(int householdId, bool enabled)
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("Du måste vara inloggad för att ändra hushållets delningsinställning.");

        var household = await _context.Households
            .FirstOrDefaultAsync(h => h.HouseholdId == householdId &&
                                      h.Members.Any(m => m.IsActive && m.UserId == userId));
        if (household is null)
            throw new UnauthorizedAccessException("Du är inte en aktiv medlem i hushållet.");

        household.ShareBalanceSheet = enabled;
        household.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task LinkCurrentUserToMemberAsync(int householdId, int householdMemberId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            throw new UnauthorizedAccessException("Du måste vara inloggad för att koppla ditt konto.");

        var accountEmail = await _context.Users
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(accountEmail))
            throw new UnauthorizedAccessException("Ditt appkonto saknar en e-postadress att verifiera.");

        var member = await _context.HouseholdMembers.FirstOrDefaultAsync(candidate =>
            candidate.HouseholdMemberId == householdMemberId &&
            candidate.HouseholdId == householdId &&
            candidate.IsActive &&
            candidate.UserId == null);
        if (member is null ||
            !string.Equals(member.Email?.Trim(), accountEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Medlemmens e-postadress måste matcha ditt appkonto.");

        var alreadyLinked = await _context.HouseholdMembers.AnyAsync(candidate =>
            candidate.HouseholdId == householdId &&
            candidate.IsActive &&
            candidate.UserId == userId);
        if (alreadyLinked)
            throw new InvalidOperationException("Ditt appkonto är redan kopplat till en aktiv hushållsmedlem.");

        member.UserId = userId;
        await _context.SaveChangesAsync();
    }

    private string? GetAuthenticatedUserId() =>
        _currentUserService.IsAuthenticated ? _currentUserService.UserId : null;
}
