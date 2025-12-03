using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Inventory;

public class InventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItem>> GetAllItemsAsync(string? category = null, string? searchTerm = null)
    {
        var query = _context.InventoryItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(i => i.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i => 
                i.ItemName.Contains(searchTerm) || 
                i.ItemCode.Contains(searchTerm));
        }

        return await query
            .Where(i => i.IsActive)
            .OrderBy(i => i.ItemName)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetItemByIdAsync(int itemId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);
    }

    public async Task<InventoryItem?> GetItemByCodeAsync(string itemCode)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ItemCode == itemCode && i.IsActive);
    }

    public async Task<int> CreateItemAsync(InventoryItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ItemCode))
        {
            // Generate item code if not provided
            var lastItem = await _context.InventoryItems
                .OrderByDescending(i => i.ItemCode)
                .FirstOrDefaultAsync();
            
            int nextNumber = 1;
            if (lastItem != null && int.TryParse(lastItem.ItemCode.Replace("ITM", ""), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
            
            item.ItemCode = $"ITM{nextNumber:D6}";
        }

        item.CreatedDate = DateTime.Now;
        item.IsActive = true;

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        return item.ItemId;
    }

    public async Task<bool> UpdateItemAsync(int itemId, InventoryItem updatedItem)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);

        if (item == null)
            return false;

        item.ItemName = updatedItem.ItemName;
        item.Category = updatedItem.Category;
        item.Unit = updatedItem.Unit;
        item.Quantity = updatedItem.Quantity;
        item.ReorderLevel = updatedItem.ReorderLevel;
        item.MaxStock = updatedItem.MaxStock;
        item.UnitPrice = updatedItem.UnitPrice;
        item.Supplier = updatedItem.Supplier;
        item.Description = updatedItem.Description;
        item.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteItemAsync(int itemId)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);

        if (item == null)
            return false;

        item.IsActive = false;
        item.UpdatedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await _context.InventoryItems
            .Where(i => i.IsActive && i.Quantity <= i.ReorderLevel)
            .OrderBy(i => i.Quantity)
            .ToListAsync();
    }

    public async Task<List<string>> GetItemCategoriesAsync()
    {
        return await _context.InventoryItems
            .Where(i => i.IsActive && !string.IsNullOrWhiteSpace(i.Category))
            .Select(i => i.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}

