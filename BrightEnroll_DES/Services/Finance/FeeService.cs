using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Finance;

// Handles fee setup and fee breakdown operations
public class FeeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeeService>? _logger;

    public FeeService(AppDbContext context, ILogger<FeeService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Gets all grade levels
    public async Task<List<GradeLevel>> GetAllGradeLevelsAsync()
    {
        try
        {
            return await _context.GradeLevels
                .Where(g => g.IsActive)
                .OrderBy(g => g.GradeLevelId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching grade levels: {Message}", ex.Message);
            throw new Exception($"Failed to fetch grade levels: {ex.Message}", ex);
        }
    }

    // Gets all fees with grade level and breakdowns
    public async Task<List<Fee>> GetAllFeesAsync()
    {
        try
        {
            return await _context.Fees
                .Include(f => f.GradeLevel)
                .Include(f => f.Breakdowns)
                .Where(f => f.IsActive)
                .OrderBy(f => f.GradeLevelId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching fees: {Message}", ex.Message);
            throw new Exception($"Failed to fetch fees: {ex.Message}", ex);
        }
    }

    // Gets fee by grade level ID
    public async Task<Fee?> GetFeeByGradeLevelIdAsync(int gradeLevelId)
    {
        try
        {
            return await _context.Fees
                .Include(f => f.GradeLevel)
                .Include(f => f.Breakdowns)
                .FirstOrDefaultAsync(f => f.GradeLevelId == gradeLevelId && f.IsActive);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching fee by grade level ID: {Message}", ex.Message);
            throw new Exception($"Failed to fetch fee: {ex.Message}", ex);
        }
    }

    // Creates a new fee with breakdowns
    public async Task<Fee> CreateFeeAsync(CreateFeeRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if fee already exists for this grade level
            var existingFee = await _context.Fees
                .FirstOrDefaultAsync(f => f.GradeLevelId == request.GradeLevelId && f.IsActive);

            if (existingFee != null)
            {
                throw new Exception($"Fee already exists for grade level ID: {request.GradeLevelId}");
            }

            // Create fee
            var fee = new Fee
            {
                GradeLevelId = request.GradeLevelId,
                TuitionFee = request.TuitionFee,
                MiscFee = request.MiscFee,
                OtherFee = request.OtherFee,
                CreatedBy = request.CreatedBy,
                IsActive = true
            };

            _context.Fees.Add(fee);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Fee created with ID: {FeeId}", fee.FeeId);

            // Create breakdown items
            var breakdowns = new List<FeeBreakdown>();
            int displayOrder = 0;

            // Add tuition breakdown items
            foreach (var item in request.TuitionBreakdown)
            {
                breakdowns.Add(new FeeBreakdown
                {
                    FeeId = fee.FeeId,
                    BreakdownType = "Tuition",
                    ItemName = item.Name,
                    Amount = item.Amount,
                    DisplayOrder = displayOrder++
                });
            }

            // Add misc breakdown items
            foreach (var item in request.MiscBreakdown)
            {
                breakdowns.Add(new FeeBreakdown
                {
                    FeeId = fee.FeeId,
                    BreakdownType = "Misc",
                    ItemName = item.Name,
                    Amount = item.Amount,
                    DisplayOrder = displayOrder++
                });
            }

            // Add other breakdown items
            foreach (var item in request.OtherBreakdown)
            {
                breakdowns.Add(new FeeBreakdown
                {
                    FeeId = fee.FeeId,
                    BreakdownType = "Other",
                    ItemName = item.Name,
                    Amount = item.Amount,
                    DisplayOrder = displayOrder++
                });
            }

            if (breakdowns.Any())
            {
                _context.FeeBreakdowns.AddRange(breakdowns);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Created {Count} breakdown items for fee ID: {FeeId}", breakdowns.Count, fee.FeeId);
            }

            await transaction.CommitAsync();

            // Reload fee with all relationships
            await _context.Entry(fee).Reference(f => f.GradeLevel).LoadAsync();
            await _context.Entry(fee).Collection(f => f.Breakdowns).LoadAsync();

            return fee;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error creating fee: {Message}", ex.Message);
            throw new Exception($"Failed to create fee: {ex.Message}", ex);
        }
    }

    // Updates an existing fee with breakdowns
    public async Task<Fee> UpdateFeeAsync(int feeId, UpdateFeeRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var fee = await _context.Fees
                .Include(f => f.Breakdowns)
                .FirstOrDefaultAsync(f => f.FeeId == feeId);

            if (fee == null)
            {
                throw new Exception($"Fee with ID {feeId} not found");
            }

            // Update fee amounts
            fee.TuitionFee = request.TuitionFee;
            fee.MiscFee = request.MiscFee;
            fee.OtherFee = request.OtherFee;
            fee.UpdatedBy = request.UpdatedBy;
            fee.UpdatedDate = DateTime.Now;

            // Remove existing breakdowns
            _context.FeeBreakdowns.RemoveRange(fee.Breakdowns);

            // Add new breakdown items
            var breakdowns = new List<FeeBreakdown>();
            int displayOrder = 0;

            // Add tuition breakdown items
            foreach (var item in request.TuitionBreakdown)
            {
                breakdowns.Add(new FeeBreakdown
                {
                    FeeId = fee.FeeId,
                    BreakdownType = "Tuition",
                    ItemName = item.Name,
                    Amount = item.Amount,
                    DisplayOrder = displayOrder++
                });
            }

            // Add misc breakdown items
            foreach (var item in request.MiscBreakdown)
            {
                breakdowns.Add(new FeeBreakdown
                {
                    FeeId = fee.FeeId,
                    BreakdownType = "Misc",
                    ItemName = item.Name,
                    Amount = item.Amount,
                    DisplayOrder = displayOrder++
                });
            }

            // Add other breakdown items
            foreach (var item in request.OtherBreakdown)
            {
                breakdowns.Add(new FeeBreakdown
                {
                    FeeId = fee.FeeId,
                    BreakdownType = "Other",
                    ItemName = item.Name,
                    Amount = item.Amount,
                    DisplayOrder = displayOrder++
                });
            }

            if (breakdowns.Any())
            {
                _context.FeeBreakdowns.AddRange(breakdowns);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger?.LogInformation("Fee updated with ID: {FeeId}", fee.FeeId);

            // Reload fee with all relationships
            await _context.Entry(fee).Reference(f => f.GradeLevel).LoadAsync();
            await _context.Entry(fee).Collection(f => f.Breakdowns).LoadAsync();

            return fee;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error updating fee: {Message}", ex.Message);
            throw new Exception($"Failed to update fee: {ex.Message}", ex);
        }
    }
}

// Request DTOs for fee operations
public class CreateFeeRequest
{
    public int GradeLevelId { get; set; }
    public decimal TuitionFee { get; set; }
    public decimal MiscFee { get; set; }
    public decimal OtherFee { get; set; }
    public List<FeeBreakdownItemDto> TuitionBreakdown { get; set; } = new();
    public List<FeeBreakdownItemDto> MiscBreakdown { get; set; } = new();
    public List<FeeBreakdownItemDto> OtherBreakdown { get; set; } = new();
    public string? CreatedBy { get; set; }
}

public class UpdateFeeRequest
{
    public decimal TuitionFee { get; set; }
    public decimal MiscFee { get; set; }
    public decimal OtherFee { get; set; }
    public List<FeeBreakdownItemDto> TuitionBreakdown { get; set; } = new();
    public List<FeeBreakdownItemDto> MiscBreakdown { get; set; } = new();
    public List<FeeBreakdownItemDto> OtherBreakdown { get; set; } = new();
    public string? UpdatedBy { get; set; }
}

public class FeeBreakdownItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

