using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class FeeSetupSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeeSetupSeeder>? _logger;

    public FeeSetupSeeder(AppDbContext context, ILogger<FeeSetupSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger?.LogInformation("=== STARTING FEE SETUP SEEDING ===");

            // Get all grade levels
            var gradeLevels = await _context.GradeLevels
                .Where(g => g.IsActive)
                .ToListAsync();

            if (!gradeLevels.Any())
            {
                _logger?.LogWarning("No grade levels found. Please seed grade levels first.");
                return;
            }

            var currentYear = DateTime.Now.Year;
            var schoolYear = $"{currentYear}-{currentYear + 1}";

            // Check if fees already exist for this school year
            var existingFees = await _context.Fees
                .Where(f => f.SchoolYear == schoolYear)
                .ToListAsync();

            if (existingFees.Any())
            {
                _logger?.LogInformation($"Fees for school year {schoolYear} already exist. Skipping.");
                return;
            }

            var fees = new List<Fee>();
            var feeBreakdowns = new List<FeeBreakdown>();

            // Fee structure per grade level
            var feeStructure = new Dictionary<string, (decimal tuition, decimal misc, decimal other)>
            {
                { "Pre-School", (15000m, 5000m, 2000m) },
                { "Kinder", (18000m, 6000m, 2500m) },
                { "Grade 1", (20000m, 7000m, 3000m) },
                { "Grade 2", (20000m, 7000m, 3000m) },
                { "Grade 3", (22000m, 8000m, 3500m) },
                { "Grade 4", (22000m, 8000m, 3500m) },
                { "Grade 5", (24000m, 9000m, 4000m) },
                { "Grade 6", (24000m, 9000m, 4000m) }
            };

            foreach (var gradeLevel in gradeLevels)
            {
                if (feeStructure.TryGetValue(gradeLevel.GradeLevelName, out var feesForGrade))
                {
                    var fee = new Fee
                    {
                        GradeLevelId = gradeLevel.GradeLevelId,
                        TuitionFee = feesForGrade.tuition,
                        MiscFee = feesForGrade.misc,
                        OtherFee = feesForGrade.other,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null,
                        CreatedBy = "System",
                        UpdatedBy = null,
                        IsActive = true,
                        SchoolYear = schoolYear
                    };

                    fees.Add(fee);
                }
            }

            _context.Fees.AddRange(fees);
            await _context.SaveChangesAsync();

            // Create fee breakdowns
            foreach (var fee in fees)
            {
                var gradeLevel = gradeLevels.First(g => g.GradeLevelId == fee.GradeLevelId);
                var feesForGrade = feeStructure[gradeLevel.GradeLevelName];

                // Tuition breakdown
                var tuitionBreakdowns = new[]
                {
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Tuition",
                        ItemName = "Basic Tuition",
                        Amount = feesForGrade.tuition * 0.7m,
                        DisplayOrder = 1,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    },
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Tuition",
                        ItemName = "Instructional Materials",
                        Amount = feesForGrade.tuition * 0.3m,
                        DisplayOrder = 2,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    }
                };

                // Misc breakdown
                var miscBreakdowns = new[]
                {
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Misc",
                        ItemName = "Library Fee",
                        Amount = feesForGrade.misc * 0.3m,
                        DisplayOrder = 1,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    },
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Misc",
                        ItemName = "Laboratory Fee",
                        Amount = feesForGrade.misc * 0.4m,
                        DisplayOrder = 2,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    },
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Misc",
                        ItemName = "Computer Fee",
                        Amount = feesForGrade.misc * 0.3m,
                        DisplayOrder = 3,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    }
                };

                // Other breakdown
                var otherBreakdowns = new[]
                {
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Other",
                        ItemName = "ID Card",
                        Amount = 200m,
                        DisplayOrder = 1,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    },
                    new FeeBreakdown
                    {
                        FeeId = fee.FeeId,
                        BreakdownType = "Other",
                        ItemName = "School Uniform",
                        Amount = feesForGrade.other - 200m,
                        DisplayOrder = 2,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    }
                };

                feeBreakdowns.AddRange(tuitionBreakdowns);
                feeBreakdowns.AddRange(miscBreakdowns);
                feeBreakdowns.AddRange(otherBreakdowns);
            }

            _context.FeeBreakdowns.AddRange(feeBreakdowns);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== FEE SETUP SEEDING COMPLETED: {fees.Count} fees with breakdowns created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding fee setup: {Message}", ex.Message);
            throw new Exception($"Failed to seed fee setup: {ex.Message}", ex);
        }
    }
}
