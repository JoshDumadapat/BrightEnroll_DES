using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class DiscountSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DiscountSeeder>? _logger;

    public DiscountSeeder(AppDbContext context, ILogger<DiscountSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger?.LogInformation("=== STARTING DISCOUNT SEEDING ===");

            // Check if discounts already exist
            var existingCount = await _context.Discounts.CountAsync();

            if (existingCount > 0)
            {
                _logger?.LogInformation($"Discounts already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var discounts = new List<Discount>
            {
                // Sibling Discount
                new Discount
                {
                    DiscountType = "Sibling",
                    DiscountName = "Sibling Discount - 2nd Child",
                    RateOrValue = 10m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "10% discount for 2nd child when siblings are enrolled",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Sibling",
                    DiscountName = "Sibling Discount - 3rd Child",
                    RateOrValue = 15m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "15% discount for 3rd child when siblings are enrolled",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Sibling",
                    DiscountName = "Sibling Discount - 4th Child and Above",
                    RateOrValue = 20m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "20% discount for 4th child and above when siblings are enrolled",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },

                // Early Bird Discount
                new Discount
                {
                    DiscountType = "Early-bird",
                    DiscountName = "Early Bird Discount - Full Payment",
                    RateOrValue = 5m,
                    IsPercentage = true,
                    MaxAmount = 5000m,
                    MinAmount = null,
                    Description = "5% discount for full payment before enrollment deadline (max Php 5,000)",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Early-bird",
                    DiscountName = "Early Bird Discount - Early Enrollment",
                    RateOrValue = 3m,
                    IsPercentage = true,
                    MaxAmount = 3000m,
                    MinAmount = null,
                    Description = "3% discount for enrollment completed 30 days before deadline (max Php 3,000)",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },

                // Scholarship Discounts
                new Discount
                {
                    DiscountType = "Scholarship",
                    DiscountName = "Academic Excellence Scholarship",
                    RateOrValue = 50m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "50% discount for students with outstanding academic performance",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Scholarship",
                    DiscountName = "Athletic Scholarship",
                    RateOrValue = 30m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "30% discount for student athletes",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Scholarship",
                    DiscountName = "Arts and Culture Scholarship",
                    RateOrValue = 25m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "25% discount for students excelling in arts and culture",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Scholarship",
                    DiscountName = "Financial Aid Scholarship",
                    RateOrValue = 40m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "40% discount for students with financial need",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },

                // Manual Discounts
                new Discount
                {
                    DiscountType = "Manual",
                    DiscountName = "Staff Discount",
                    RateOrValue = 20m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "20% discount for children of school staff",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Manual",
                    DiscountName = "Alumni Discount",
                    RateOrValue = 10m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "10% discount for children of alumni",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Manual",
                    DiscountName = "Loyalty Discount",
                    RateOrValue = 5m,
                    IsPercentage = true,
                    MaxAmount = null,
                    MinAmount = null,
                    Description = "5% discount for returning students",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },

                // Fixed Amount Discounts
                new Discount
                {
                    DiscountType = "Manual",
                    DiscountName = "Registration Fee Waiver",
                    RateOrValue = 500m,
                    IsPercentage = false,
                    MaxAmount = 500m,
                    MinAmount = 500m,
                    Description = "Fixed Php 500 discount for registration fee waiver",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                },
                new Discount
                {
                    DiscountType = "Manual",
                    DiscountName = "Book Fee Discount",
                    RateOrValue = 1000m,
                    IsPercentage = false,
                    MaxAmount = 1000m,
                    MinAmount = 1000m,
                    Description = "Fixed Php 1,000 discount for book fees",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                }
            };

            _context.Discounts.AddRange(discounts);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== DISCOUNT SEEDING COMPLETED: {discounts.Count} discounts created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding discounts: {Message}", ex.Message);
            throw new Exception($"Failed to seed discounts: {ex.Message}", ex);
        }
    }
}
