using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

public class FeeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeeService>? _logger;

    public FeeService(AppDbContext context, ILogger<FeeService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<List<Fee>> GetAllFeesAsync()
    {
        try
        {
            var fees = await _context.Fees
                .Include(f => f.GradeLevel)
                .Include(f => f.Breakdowns)
                .Where(f => f.IsActive)
                .OrderBy(f => f.GradeLevelId)
                .ToListAsync();

            _logger?.LogInformation("Loaded {Count} fees from database", fees.Count);
            return fees;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading fees: {Message}", ex.Message);
            throw;
        }
    }

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
            _logger?.LogError(ex, "Error getting fee by grade level ID {GradeLevelId}: {Message}", gradeLevelId, ex.Message);
            throw;
        }
    }

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
            _logger?.LogError(ex, "Error loading grade levels: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Fee> CreateFeeAsync(CreateFeeRequest request)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var fee = new Fee
                {
                    GradeLevelId = request.GradeLevelId,
                    TuitionFee = request.TuitionFee,
                    MiscFee = request.MiscFee,
                    OtherFee = request.OtherFee,
                    CreatedDate = DateTime.Now,
                    CreatedBy = request.CreatedBy,
                    IsActive = true
                };

                _context.Fees.Add(fee);
                await _context.SaveChangesAsync();

                int displayOrder = 0;
                
                foreach (var item in request.TuitionBreakdown ?? new List<FeeBreakdownItemDto>())
                {
                    _context.FeeBreakdowns.Add(new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Tuition",
                        ItemName = item.Name,
                        Amount = item.Amount,
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now
                    });
                }

                // Misc breakdown
                foreach (var item in request.MiscBreakdown ?? new List<FeeBreakdownItemDto>())
                {
                    _context.FeeBreakdowns.Add(new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Misc",
                        ItemName = item.Name,
                        Amount = item.Amount,
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now
                    });
                }

                // Other breakdown
                foreach (var item in request.OtherBreakdown ?? new List<FeeBreakdownItemDto>())
                {
                    _context.FeeBreakdowns.Add(new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Other",
                        ItemName = item.Name,
                        Amount = item.Amount,
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger?.LogInformation("Fee created successfully for grade level ID {GradeLevelId}", request.GradeLevelId);
                return await GetFeeByGradeLevelIdAsync(request.GradeLevelId) ?? fee;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error creating fee: {Message}", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreateFeeAsync: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Fee> UpdateFeeAsync(int feeId, UpdateFeeRequest request)
    {
        try
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

                fee.TuitionFee = request.TuitionFee;
                fee.MiscFee = request.MiscFee;
                fee.OtherFee = request.OtherFee;
                fee.UpdatedDate = DateTime.Now;
                fee.UpdatedBy = request.UpdatedBy;

                _context.FeeBreakdowns.RemoveRange(fee.Breakdowns);

                int displayOrder = 0;

                foreach (var item in request.TuitionBreakdown ?? new List<FeeBreakdownItemDto>())
                {
                    _context.FeeBreakdowns.Add(new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Tuition",
                        ItemName = item.Name,
                        Amount = item.Amount,
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now
                    });
                }

                // Misc breakdown
                foreach (var item in request.MiscBreakdown ?? new List<FeeBreakdownItemDto>())
                {
                    _context.FeeBreakdowns.Add(new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Misc",
                        ItemName = item.Name,
                        Amount = item.Amount,
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now
                    });
                }

                // Other breakdown
                foreach (var item in request.OtherBreakdown ?? new List<FeeBreakdownItemDto>())
                {
                    _context.FeeBreakdowns.Add(new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Other",
                        ItemName = item.Name,
                        Amount = item.Amount,
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger?.LogInformation("Fee updated successfully: Fee ID {FeeId}", feeId);
                return await _context.Fees
                    .Include(f => f.GradeLevel)
                    .Include(f => f.Breakdowns)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId) ?? fee;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error updating fee: {Message}", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateFeeAsync: {Message}", ex.Message);
            throw;
        }
    }
}

public class CreateFeeRequest
{
    public int GradeLevelId { get; set; }
    public decimal TuitionFee { get; set; }
    public decimal MiscFee { get; set; }
    public decimal OtherFee { get; set; }
    public List<FeeBreakdownItemDto>? TuitionBreakdown { get; set; }
    public List<FeeBreakdownItemDto>? MiscBreakdown { get; set; }
    public List<FeeBreakdownItemDto>? OtherBreakdown { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateFeeRequest
{
    public decimal TuitionFee { get; set; }
    public decimal MiscFee { get; set; }
    public decimal OtherFee { get; set; }
    public List<FeeBreakdownItemDto>? TuitionBreakdown { get; set; }
    public List<FeeBreakdownItemDto>? MiscBreakdown { get; set; }
    public List<FeeBreakdownItemDto>? OtherBreakdown { get; set; }
    public string? UpdatedBy { get; set; }
}

public class FeeBreakdownItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

