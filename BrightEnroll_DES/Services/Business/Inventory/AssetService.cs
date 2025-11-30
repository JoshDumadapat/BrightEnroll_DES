using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Inventory;

public class AssetService
{
    private readonly AppDbContext _context;

    public AssetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Asset>> GetAllAssetsAsync(string? category = null, string? status = null, string? searchTerm = null)
    {
        var query = _context.Assets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(a => 
                a.AssetName.Contains(searchTerm) || 
                a.AssetId.Contains(searchTerm) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(searchTerm)));
        }

        return await query
            .Where(a => a.IsActive)
            .OrderBy(a => a.AssetName)
            .ToListAsync();
    }

    public async Task<Asset?> GetAssetByIdAsync(string assetId)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId == assetId && a.IsActive);
    }

    public async Task<string> CreateAssetAsync(Asset asset)
    {
        if (string.IsNullOrWhiteSpace(asset.AssetId))
        {
            // Generate asset ID if not provided
            var lastAsset = await _context.Assets
                .OrderByDescending(a => a.AssetId)
                .FirstOrDefaultAsync();
            
            int nextNumber = 1;
            if (lastAsset != null && int.TryParse(lastAsset.AssetId.Replace("AST", ""), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
            
            asset.AssetId = $"AST{nextNumber:D6}";
        }

        asset.CreatedDate = DateTime.Now;
        asset.IsActive = true;
        asset.CurrentValue = asset.PurchaseCost; // Initial value equals purchase cost

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        return asset.AssetId;
    }

    public async Task<bool> UpdateAssetAsync(string assetId, Asset updatedAsset)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId == assetId && a.IsActive);

        if (asset == null)
            return false;

        asset.AssetName = updatedAsset.AssetName;
        asset.Category = updatedAsset.Category;
        asset.Brand = updatedAsset.Brand;
        asset.Model = updatedAsset.Model;
        asset.SerialNumber = updatedAsset.SerialNumber;
        asset.Location = updatedAsset.Location;
        asset.Status = updatedAsset.Status;
        asset.PurchaseDate = updatedAsset.PurchaseDate;
        asset.PurchaseCost = updatedAsset.PurchaseCost;
        asset.CurrentValue = updatedAsset.CurrentValue;
        asset.Description = updatedAsset.Description;
        asset.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAssetAsync(string assetId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId == assetId && a.IsActive);

        if (asset == null)
            return false;

        asset.IsActive = false;
        asset.UpdatedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> GetAssetCategoriesAsync()
    {
        return await _context.Assets
            .Where(a => a.IsActive && !string.IsNullOrWhiteSpace(a.Category))
            .Select(a => a.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}

