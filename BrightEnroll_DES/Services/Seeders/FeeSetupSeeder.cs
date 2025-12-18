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

            // Check which grade levels already have fees for this school year
            // Load all existing fees ONCE before the loop to avoid concurrency issues
            var existingFees = await _context.Fees
                .AsNoTracking()
                .Where(f => f.SchoolYear == schoolYear || f.SchoolYear == null)
                .ToListAsync();

            // Create a dictionary for quick lookup by GradeLevelId
            var existingFeesByGradeLevel = existingFees
                .GroupBy(f => f.GradeLevelId)
                .ToDictionary(g => g.Key, g => g.First());

            var gradeLevelsWithFees = existingFees.Select(f => f.GradeLevelId).ToHashSet();
            
            // Check if ALL grade levels have fees
            var allGradeLevelsHaveFees = gradeLevels.All(g => gradeLevelsWithFees.Contains(g.GradeLevelId));
            
            if (allGradeLevelsHaveFees && existingFees.Any())
            {
                _logger?.LogInformation($"Fees for all grade levels in school year {schoolYear} already exist. Skipping.");
                return;
            }
            
            if (existingFees.Any())
            {
                _logger?.LogInformation($"Some fees exist for school year {schoolYear}. Will create fees for missing grade levels.");
            }

            _context.ChangeTracker.Clear(); // Clear tracker before creating new entities

            var fees = new List<Fee>();
            var feeBreakdowns = new List<FeeBreakdown>();

            // Fee structure per grade level - Complete for all grade levels
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

            // Create fees for ALL grade levels in the database
            foreach (var gradeLevel in gradeLevels)
            {
                // Check if fee already exists using in-memory dictionary (no database query in loop)
                if (existingFeesByGradeLevel.ContainsKey(gradeLevel.GradeLevelId))
                {
                    _logger?.LogInformation($"Fee already exists for {gradeLevel.GradeLevelName}, skipping.");
                    continue;
                }

                // Get fee structure from dictionary, or use default based on grade level name
                (decimal tuition, decimal misc, decimal other) feesForGrade;
                
                if (feeStructure.TryGetValue(gradeLevel.GradeLevelName, out feesForGrade))
                {
                    // Use predefined fee structure
                }
                else
                {
                    // Calculate default fees for grade levels not in dictionary
                    // Base calculation: Higher grade = higher fees
                    if (gradeLevel.GradeLevelName.Contains("Pre") || gradeLevel.GradeLevelName.Contains("Pre-School"))
                    {
                        feesForGrade = (15000m, 5000m, 2000m);
                    }
                    else if (gradeLevel.GradeLevelName.Contains("Kinder") || gradeLevel.GradeLevelName.Contains("Kindergarten"))
                    {
                        feesForGrade = (18000m, 6000m, 2500m);
                    }
                    else if (gradeLevel.GradeLevelName.Contains("Grade"))
                    {
                        // Extract grade number if possible
                        var gradeMatch = System.Text.RegularExpressions.Regex.Match(gradeLevel.GradeLevelName, @"\d+");
                        if (gradeMatch.Success && int.TryParse(gradeMatch.Value, out int gradeNum))
                        {
                            // Progressive fee structure: Grade 1-2: 20000, Grade 3-4: 22000, Grade 5-6: 24000
                            if (gradeNum <= 2)
                            {
                                feesForGrade = (20000m, 7000m, 3000m);
                            }
                            else if (gradeNum <= 4)
                            {
                                feesForGrade = (22000m, 8000m, 3500m);
                            }
                            else
                            {
                                feesForGrade = (24000m, 9000m, 4000m);
                            }
                        }
                        else
                        {
                            // Default for any grade level
                            feesForGrade = (20000m, 7000m, 3000m);
                        }
                    }
                    else
                    {
                        // Default fee structure
                        feesForGrade = (20000m, 7000m, 3000m);
                    }
                    
                    _logger?.LogInformation($"Using calculated fees for grade level '{gradeLevel.GradeLevelName}': Tuition={feesForGrade.tuition}, Misc={feesForGrade.misc}, Other={feesForGrade.other}");
                }

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
                _logger?.LogInformation($"Created fee for {gradeLevel.GradeLevelName}: Tuition={feesForGrade.tuition}, Misc={feesForGrade.misc}, Other={feesForGrade.other}");
            }

            if (fees.Any())
            {
                _context.Fees.AddRange(fees);
                await _context.SaveChangesAsync();
                _logger?.LogInformation($"Created {fees.Count} fee records.");
            }
            else
            {
                _logger?.LogInformation("No new fees to create. All grade levels already have fees.");
            }

            // Clear change tracker before loading fees for breakdowns
            _context.ChangeTracker.Clear();

            // Get all fees (including newly created and existing) for breakdown creation
            // Use AsNoTracking to avoid concurrency issues
            var allFeesForBreakdown = await _context.Fees
                .AsNoTracking()
                .Where(f => f.SchoolYear == schoolYear || f.SchoolYear == null)
                .Include(f => f.Breakdowns)
                .ToListAsync();

            // Create fee breakdowns for all fees that don't have breakdowns yet
            foreach (var fee in allFeesForBreakdown)
            {
                var gradeLevel = gradeLevels.First(g => g.GradeLevelId == fee.GradeLevelId);
                
                // Skip if breakdowns already exist
                if (fee.Breakdowns != null && fee.Breakdowns.Any())
                {
                    _logger?.LogInformation($"Fee breakdowns already exist for {gradeLevel.GradeLevelName}, skipping.");
                    continue;
                }
                
                // Get fee structure (from dictionary or use existing fee amounts)
                (decimal tuition, decimal misc, decimal other) feesForGrade;
                if (feeStructure.TryGetValue(gradeLevel.GradeLevelName, out feesForGrade))
                {
                    // Use predefined
                }
                else
                {
                    // Use the fee amounts that were saved
                    feesForGrade = (fee.TuitionFee, fee.MiscFee, fee.OtherFee);
                }

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

            if (feeBreakdowns.Any())
            {
                _context.ChangeTracker.Clear(); // Clear before adding new entities
                _context.FeeBreakdowns.AddRange(feeBreakdowns);
                await _context.SaveChangesAsync();
                _logger?.LogInformation($"Created {feeBreakdowns.Count} fee breakdown records.");
            }
            
            _context.ChangeTracker.Clear();

            // Use AsNoTracking for final count queries to avoid concurrency issues
            var totalFees = await _context.Fees
                .AsNoTracking()
                .Where(f => f.SchoolYear == schoolYear || f.SchoolYear == null)
                .CountAsync();
            
            var totalBreakdowns = await _context.FeeBreakdowns
                .AsNoTracking()
                .Where(fb => _context.Fees
                    .AsNoTracking()
                    .Any(f => f.FeeId == fb.FeeId && (f.SchoolYear == schoolYear || f.SchoolYear == null)))
                .CountAsync();

            _logger?.LogInformation($"=== FEE SETUP SEEDING COMPLETED: {totalFees} total fees with {totalBreakdowns} breakdowns for school year {schoolYear} ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding fee setup: {Message}", ex.Message);
            throw new Exception($"Failed to seed fee setup: {ex.Message}", ex);
        }
    }
}
