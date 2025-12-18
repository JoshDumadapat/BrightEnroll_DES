using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Services.Business.Audit;

namespace BrightEnroll_DES.Services.Business.Finance;

public class FeeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeeService>? _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    public FeeService(
        AppDbContext context, 
        ILogger<FeeService>? logger = null,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<List<Fee>> GetAllFeesAsync()
    {
        try
        {
            // Use AsNoTracking for read-only query to avoid concurrency issues
            // Combine queries into a single operation to avoid multiple concurrent DbContext operations
            var fees = await _context.Fees
                .AsNoTracking()
                .Include(f => f.GradeLevel)
                .Include(f => f.Breakdowns)
                .Where(f => f.IsActive && 
                           f.GradeLevel != null && 
                           f.GradeLevel.IsActive) // Filter directly in query instead of separate call
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
            // Single query with navigation property filter to avoid multiple concurrent DbContext operations
            return await _context.Fees
                .Include(f => f.GradeLevel)
                .Include(f => f.Breakdowns)
                .Where(f => f.GradeLevelId == gradeLevelId && 
                           f.IsActive && 
                           f.GradeLevel != null && 
                           f.GradeLevel.IsActive)
                .FirstOrDefaultAsync();
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
                
                var createdFee = await GetFeeByGradeLevelIdAsync(request.GradeLevelId) ?? fee;
                
                // Audit logging (non-blocking, background task)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_serviceScopeFactory != null)
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                            
                            await auditLogService.CreateTransactionLogAsync(
                                action: "Create Fee",
                                module: "Finance",
                                description: $"Created fee for Grade Level ID {request.GradeLevelId}: Tuition=₱{request.TuitionFee:N2}, Misc=₱{request.MiscFee:N2}, Other=₱{request.OtherFee:N2}",
                                userName: request.CreatedBy,
                                userRole: null,
                                userId: null,
                                entityType: "Fee",
                                entityId: createdFee.FeeId.ToString(),
                                oldValues: null,
                                newValues: $"GradeLevelId: {createdFee.GradeLevelId}, Tuition: ₱{createdFee.TuitionFee:N2}, Misc: ₱{createdFee.MiscFee:N2}, Other: ₱{createdFee.OtherFee:N2}",
                                status: "Success",
                                severity: "High"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create audit log for fee creation: {Message}", ex.Message);
                    }
                });
                
                return createdFee;
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
                
                // Capture old values for audit log
                var oldTuition = fee.TuitionFee;
                var oldMisc = fee.MiscFee;
                var oldOther = fee.OtherFee;
                
                // Reload fee with includes, but verify GradeLevel exists first
                var updatedFee = await _context.Fees
                    .Include(f => f.GradeLevel)
                    .Include(f => f.Breakdowns)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);
                
                // If reload failed or GradeLevel is null, return the fee we already have
                if (updatedFee == null || updatedFee.GradeLevel == null)
                {
                    _logger?.LogWarning("Could not reload fee {FeeId} with includes, returning fee from context", feeId);
                    updatedFee = fee;
                }
                
                // Audit logging (non-blocking, background task)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_serviceScopeFactory != null)
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                            
                            await auditLogService.CreateTransactionLogAsync(
                                action: "Update Fee",
                                module: "Finance",
                                description: $"Updated fee ID {feeId} for Grade Level ID {updatedFee.GradeLevelId}",
                                userName: request.UpdatedBy,
                                userRole: null,
                                userId: null,
                                entityType: "Fee",
                                entityId: feeId.ToString(),
                                oldValues: $"Tuition: ₱{oldTuition:N2}, Misc: ₱{oldMisc:N2}, Other: ₱{oldOther:N2}",
                                newValues: $"Tuition: ₱{updatedFee.TuitionFee:N2}, Misc: ₱{updatedFee.MiscFee:N2}, Other: ₱{updatedFee.OtherFee:N2}",
                                status: "Success",
                                severity: "High"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create audit log for fee update: {Message}", ex.Message);
                    }
                });
                
                return updatedFee;
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

    public async Task<decimal> CalculateTotalFeesAsync(string gradeLevelName)
    {
        if (string.IsNullOrWhiteSpace(gradeLevelName))
            return 0;

        // Load grade level - use EF Core translatable case-insensitive comparison
        // Convert both to uppercase for case-insensitive comparison (EF Core can translate this)
        var gradeLevelNameUpper = gradeLevelName.ToUpper();
        var gradeLevel = await _context.GradeLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(g =>
                g.GradeLevelName.ToUpper() == gradeLevelNameUpper);

        if (gradeLevel == null)
        {
            _logger?.LogWarning("No GradeLevel found matching: {GradeLevelName}", gradeLevelName);
            return 0;
        }

        // Load associated fee
        var fee = await GetFeeByGradeLevelIdAsync(gradeLevel.GradeLevelId);
        if (fee == null)
        {
            _logger?.LogWarning("No fee record found for GradeLevelId: {Id}", gradeLevel.GradeLevelId);
        }

        return fee?.TotalFee ?? 0;
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

