using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.Business.Finance;

public class DiscountService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DiscountService>? _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly IAuthService? _authService;

    public DiscountService(
        AppDbContext context, 
        ILogger<DiscountService>? logger = null,
        IServiceScopeFactory? serviceScopeFactory = null,
        IAuthService? authService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _authService = authService;
    }

    // Get all discounts
    public async Task<List<Discount>> GetAllDiscountsAsync()
    {
        try
        {
            var discounts = await _context.Discounts
                .OrderBy(d => d.DiscountType)
                .ThenBy(d => d.DiscountName)
                .ToListAsync();

            _logger?.LogInformation("Loaded {Count} discounts from database", discounts.Count);
            return discounts;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading discounts: {Message}", ex.Message);
            throw;
        }
    }

    // Get active discounts only
    public async Task<List<Discount>> GetActiveDiscountsAsync()
    {
        try
        {
            var discounts = await _context.Discounts
                .Where(d => d.IsActive)
                .OrderBy(d => d.DiscountType)
                .ThenBy(d => d.DiscountName)
                .ToListAsync();

            _logger?.LogInformation("Loaded {Count} active discounts from database", discounts.Count);
            return discounts;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading active discounts: {Message}", ex.Message);
            throw;
        }
    }

    // Get discount by ID
    public async Task<Discount?> GetDiscountByIdAsync(int discountId)
    {
        try
        {
            return await _context.Discounts
                .FirstOrDefaultAsync(d => d.DiscountId == discountId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting discount by ID {DiscountId}: {Message}", discountId, ex.Message);
            throw;
        }
    }

    // Get discount by type
    public async Task<Discount?> GetDiscountByTypeAsync(string discountType)
    {
        try
        {
            return await _context.Discounts
                .FirstOrDefaultAsync(d => d.DiscountType == discountType && d.IsActive);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting discount by type {DiscountType}: {Message}", discountType, ex.Message);
            throw;
        }
    }

    // Create new discount
    public async Task<Discount> CreateDiscountAsync(CreateDiscountRequest request)
    {
        try
        {
            // Check for duplicate discount name
            var existing = await _context.Discounts
                .FirstOrDefaultAsync(d => d.DiscountName.ToLower() == request.DiscountName.Trim().ToLower());

            if (existing != null)
            {
                throw new Exception($"Discount name '{request.DiscountName}' already exists.");
            }

            var discount = new Discount
            {
                DiscountType = request.DiscountType,
                DiscountName = request.DiscountName,
                RateOrValue = request.RateOrValue,
                IsPercentage = request.IsPercentage,
                MaxAmount = request.MaxAmount,
                MinAmount = request.MinAmount,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedDate = DateTime.Now
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Discount created successfully: {DiscountType} - {DiscountName}", 
                discount.DiscountType, discount.DiscountName);
            
            // Log audit trail in background
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        var userRole = currentUser?.user_role ?? "System";
                        var userId = currentUser?.user_ID;
                        
                        await auditLogService.CreateTransactionLogAsync(
                            action: "Create Discount",
                            module: "Finance",
                            description: $"Created discount: {discount.DiscountName} ({discount.DiscountType}) - Rate: {discount.RateOrValue}{(discount.IsPercentage ? "%" : "")}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Discount",
                            entityId: discount.DiscountId.ToString(),
                            oldValues: null,
                            newValues: $"Type: {discount.DiscountType}, Name: {discount.DiscountName}, Rate: {discount.RateOrValue}, IsPercentage: {discount.IsPercentage}, Active: {discount.IsActive}",
                            status: "Success",
                            severity: "High"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create audit log for discount creation: {Message}", ex.Message);
                }
            });
            
            return discount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating discount: {Message}", ex.Message);
            throw;
        }
    }

    // Update existing discount
    public async Task<Discount> UpdateDiscountAsync(int discountId, UpdateDiscountRequest request)
    {
        try
        {
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.DiscountId == discountId);

            if (discount == null)
            {
                throw new Exception($"Discount with ID {discountId} not found.");
            }

            // Check for duplicate name if changed
            if (discount.DiscountName.ToLower() != request.DiscountName.Trim().ToLower())
            {
                var existing = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.DiscountName.ToLower() == request.DiscountName.Trim().ToLower() && d.DiscountId != discountId);

                if (existing != null)
                {
                    throw new Exception($"Discount name '{request.DiscountName}' already exists.");
                }
            }

            discount.DiscountType = request.DiscountType;
            discount.DiscountName = request.DiscountName;
            discount.RateOrValue = request.RateOrValue;
            discount.IsPercentage = request.IsPercentage;
            discount.MaxAmount = request.MaxAmount;
            discount.MinAmount = request.MinAmount;
            discount.Description = request.Description;
            discount.IsActive = request.IsActive;
            discount.UpdatedDate = DateTime.Now;

            // Store old values for audit
            var oldType = discount.DiscountType;
            var oldName = discount.DiscountName;
            var oldRate = discount.RateOrValue;
            var oldIsPercentage = discount.IsPercentage;
            var oldIsActive = discount.IsActive;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Discount updated successfully: Discount ID {DiscountId}", discountId);
            
            // Log audit trail in background
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        var userRole = currentUser?.user_role ?? "System";
                        var userId = currentUser?.user_ID;
                        
                        await auditLogService.CreateTransactionLogAsync(
                            action: "Update Discount",
                            module: "Finance",
                            description: $"Updated discount ID {discountId}: {discount.DiscountName} ({discount.DiscountType})",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Discount",
                            entityId: discountId.ToString(),
                            oldValues: $"Type: {oldType}, Name: {oldName}, Rate: {oldRate}, IsPercentage: {oldIsPercentage}, Active: {oldIsActive}",
                            newValues: $"Type: {discount.DiscountType}, Name: {discount.DiscountName}, Rate: {discount.RateOrValue}, IsPercentage: {discount.IsPercentage}, Active: {discount.IsActive}",
                            status: "Success",
                            severity: "High"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create audit log for discount update: {Message}", ex.Message);
                }
            });
            
            return discount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating discount: {Message}", ex.Message);
            throw;
        }
    }

    // Delete discount (soft delete if in use)
    public async Task<bool> DeleteDiscountAsync(int discountId)
    {
        try
        {
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.DiscountId == discountId);

            if (discount == null)
            {
                throw new Exception($"Discount with ID {discountId} not found.");
            }

            // Check if discount is in use
            var isUsed = await _context.LedgerCharges
                .AnyAsync(lc => lc.DiscountId == discountId);

            // Store discount info for audit
            var discountType = discount.DiscountType;
            var discountName = discount.DiscountName;
            var wasActive = discount.IsActive;
            var isHardDelete = false;

            if (isUsed)
            {
                // Deactivate if in use
                discount.IsActive = false;
                discount.UpdatedDate = DateTime.Now;
                _logger?.LogInformation("Discount {DiscountId} is in use, deactivating instead of deleting", discountId);
            }
            else
            {
                // Remove if not in use
                isHardDelete = true;
                _context.Discounts.Remove(discount);
                _logger?.LogInformation("Discount {DiscountId} deleted permanently", discountId);
            }

            await _context.SaveChangesAsync();
            
            // Log audit trail in background
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        var userRole = currentUser?.user_role ?? "System";
                        var userId = currentUser?.user_ID;
                        
                        await auditLogService.CreateTransactionLogAsync(
                            action: isHardDelete ? "Delete Discount" : "Deactivate Discount",
                            module: "Finance",
                            description: $"{(isHardDelete ? "Deleted" : "Deactivated")} discount: {discountName} ({discountType})",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Discount",
                            entityId: discountId.ToString(),
                            oldValues: $"Type: {discountType}, Name: {discountName}, Active: {wasActive}",
                            newValues: isHardDelete ? "Deleted" : "Active: false",
                            status: "Success",
                            severity: "High"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create audit log for discount deletion: {Message}", ex.Message);
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting discount: {Message}", ex.Message);
            throw;
        }
    }

    // Calculate discount amount
    public decimal CalculateDiscountAmount(Discount discount, decimal baseAmount)
    {
        if (discount == null || !discount.IsActive)
        {
            return 0;
        }

        decimal discountAmount = 0;

        if (discount.IsPercentage)
        {
            discountAmount = baseAmount * (discount.RateOrValue / 100m);
        }
        else
        {
            discountAmount = discount.RateOrValue;
        }

        // Apply min/max limits
        if (discount.MinAmount.HasValue && discountAmount < discount.MinAmount.Value)
        {
            discountAmount = discount.MinAmount.Value;
        }

        if (discount.MaxAmount.HasValue && discountAmount > discount.MaxAmount.Value)
        {
            discountAmount = discount.MaxAmount.Value;
        }

        return discountAmount;
    }
}

// Request model for creating discount
public class CreateDiscountRequest
{
    public string DiscountType { get; set; } = string.Empty;
    public string DiscountName { get; set; } = string.Empty;
    public decimal RateOrValue { get; set; }
    public bool IsPercentage { get; set; } = true;
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// Request model for updating discount
public class UpdateDiscountRequest
{
    public string DiscountType { get; set; } = string.Empty;
    public string DiscountName { get; set; } = string.Empty;
    public decimal RateOrValue { get; set; }
    public bool IsPercentage { get; set; } = true;
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
