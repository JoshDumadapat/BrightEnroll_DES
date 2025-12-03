using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Inventory;

public class AssetAssignmentService
{
    private readonly AppDbContext _context;

    public AssetAssignmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetAssignment>> GetAssetAssignmentsAsync(string? assetId = null, string? status = null)
    {
        var query = _context.AssetAssignments
            .Include(a => a.Asset)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(assetId))
        {
            query = query.Where(a => a.AssetId == assetId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        return await query
            .OrderByDescending(a => a.AssignedDate)
            .ToListAsync();
    }

    public async Task<AssetAssignment?> GetAssignmentByIdAsync(int assignmentId)
    {
        return await _context.AssetAssignments
            .Include(a => a.Asset)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
    }

    public async Task<int> CreateAssignmentAsync(AssetAssignment assignment)
    {
        assignment.AssignedDate = DateTime.Now;
        assignment.Status = "Active";

        _context.AssetAssignments.Add(assignment);
        
        // Update asset status to "In Use"
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == assignment.AssetId);
        if (asset != null)
        {
            asset.Status = "In Use";
            asset.UpdatedDate = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return assignment.AssignmentId;
    }

    public async Task<bool> ReturnAssetAsync(int assignmentId)
    {
        var assignment = await _context.AssetAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

        if (assignment == null || assignment.Status == "Returned")
            return false;

        assignment.ReturnDate = DateTime.Now;
        assignment.Status = "Returned";

        // Update asset status to "Available"
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetId == assignment.AssetId);
        if (asset != null)
        {
            asset.Status = "Available";
            asset.UpdatedDate = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}

