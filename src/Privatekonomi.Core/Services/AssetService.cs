using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class AssetService : IAssetService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;

    public AssetService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
    {
        var query = _context.Assets.AsNoTracking().AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(a => a.UserId == _currentUserService.UserId);
        }

        return await query.OrderByDescending(a => a.CurrentValue).ToListAsync();
    }

    public async Task<Asset?> GetAssetByIdAsync(int id)
    {
        var query = _context.Assets.AsQueryable();

        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(a => a.UserId == _currentUserService.UserId);
        }

        return await query.FirstOrDefaultAsync(a => a.AssetId == id);
    }

    public async Task<Asset> CreateAssetAsync(Asset asset)
    {
        asset.CreatedAt = DateTime.UtcNow;
        
        // Set user ID for new assets
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            asset.UserId = _currentUserService.UserId;
        }
        
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task UpdateAssetAsync(Asset asset)
    {
        var existingAsset = await GetAssetByIdAsync(asset.AssetId)
            ?? throw new InvalidOperationException($"Asset with ID {asset.AssetId} not found");

        existingAsset.Name = asset.Name;
        existingAsset.Type = asset.Type;
        existingAsset.Description = asset.Description;
        existingAsset.PurchaseValue = asset.PurchaseValue;
        existingAsset.CurrentValue = asset.CurrentValue;
        existingAsset.PurchaseDate = asset.PurchaseDate;
        existingAsset.Location = asset.Location;
        existingAsset.ChartOfAccountsCode = asset.ChartOfAccountsCode;
        existingAsset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAssetAsync(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset != null)
        {
            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Asset>> GetAssetsByTypeAsync(string type)
    {
        return await _context.Assets
            .Where(a => a.Type == type)
            .OrderByDescending(a => a.CurrentValue)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalAssetValueAsync()
    {
        return await _context.Assets.SumAsync(a => a.CurrentValue);
    }
}
