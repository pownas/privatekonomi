using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class BankSourceService : IBankSourceService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;

    public BankSourceService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private IQueryable<BankSource> ApplyOwnershipFilter(IQueryable<BankSource> query)
    {
        if (_currentUserService?.IsAuthenticated != true)
        {
            return query;
        }

        var userId = _currentUserService.UserId;
        return string.IsNullOrWhiteSpace(userId)
            ? query.Where(_ => false)
            : query.Where(b => b.UserId == userId);
    }

    public async Task<IEnumerable<BankSource>> GetAllBankSourcesAsync()
    {
        return await ApplyOwnershipFilter(_context.BankSources)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<BankSource?> GetBankSourceByIdAsync(int id)
    {
        return await ApplyOwnershipFilter(_context.BankSources)
            .FirstOrDefaultAsync(b => b.BankSourceId == id);
    }

    public async Task<BankSource> CreateBankSourceAsync(BankSource bankSource)
    {
        // Set user ID for new bank sources
        if (_currentUserService?.IsAuthenticated == true)
        {
            bankSource.UserId = _currentUserService.UserId;
        }
        
        _context.BankSources.Add(bankSource);
        await _context.SaveChangesAsync();
        return bankSource;
    }

    public async Task<BankSource> UpdateBankSourceAsync(BankSource bankSource)
    {
        var existingBankSource = await ApplyOwnershipFilter(_context.BankSources)
            .FirstOrDefaultAsync(b => b.BankSourceId == bankSource.BankSourceId);
        if (existingBankSource == null)
        {
            throw new InvalidOperationException($"Bank source with ID {bankSource.BankSourceId} not found");
        }

        if (_currentUserService?.IsAuthenticated == true)
        {
            existingBankSource.UserId = _currentUserService.UserId;
        }

        existingBankSource.Name = bankSource.Name;
        existingBankSource.Color = bankSource.Color;
        existingBankSource.Logo = bankSource.Logo;
        existingBankSource.AccountType = bankSource.AccountType;
        existingBankSource.Currency = bankSource.Currency;
        existingBankSource.Institution = bankSource.Institution;
        existingBankSource.AccountNumber = bankSource.AccountNumber;
        existingBankSource.ClearingNumber = bankSource.ClearingNumber;
        existingBankSource.ChartOfAccountsCode = bankSource.ChartOfAccountsCode;
        existingBankSource.InitialBalance = bankSource.InitialBalance;
        existingBankSource.OpenedDate = bankSource.OpenedDate;
        existingBankSource.ClosedDate = bankSource.ClosedDate;
        existingBankSource.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingBankSource;
    }

    public async Task DeleteBankSourceAsync(int id)
    {
        var bankSource = await ApplyOwnershipFilter(_context.BankSources)
            .FirstOrDefaultAsync(b => b.BankSourceId == id);
        if (bankSource != null)
        {
            _context.BankSources.Remove(bankSource);
            await _context.SaveChangesAsync();
        }
    }
}
