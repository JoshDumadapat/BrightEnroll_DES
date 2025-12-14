using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Academic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

/// <summary>
/// Service for managing student ledgers per school year
/// Handles ledger creation, charge management, payment processing, and balance calculations
/// </summary>
public class StudentLedgerService
{
    private readonly AppDbContext _context;
    private readonly FeeService _feeService;
    private readonly SchoolYearService _schoolYearService;
    private readonly ILogger<StudentLedgerService>? _logger;

    public StudentLedgerService(
        AppDbContext context,
        FeeService feeService,
        SchoolYearService schoolYearService,
        ILogger<StudentLedgerService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _feeService = feeService ?? throw new ArgumentNullException(nameof(feeService));
        _schoolYearService = schoolYearService ?? throw new ArgumentNullException(nameof(schoolYearService));
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a ledger for a student for the current active school year
    /// Called automatically when student enters "For Payment" status
    /// </summary>
    public async Task<StudentLedger> GetOrCreateLedgerForCurrentSYAsync(string studentId, string? gradeLevel = null)
    {
        try
        {
            var activeSchoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
            if (string.IsNullOrWhiteSpace(activeSchoolYear))
            {
                throw new Exception("No active school year is open. Please open a school year before creating ledgers.");
            }

            // Get student to determine grade level if not provided (needed for both existing and new ledger paths)
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                throw new Exception($"Student {studentId} not found.");
            }

            // Check if ledger already exists
            var existingLedger = await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.StudentId == studentId && l.SchoolYear == activeSchoolYear);

            if (existingLedger != null)
            {
                // If ledger exists but has no charges, populate them now
                if (!existingLedger.Charges.Any())
                {
                    var ledgerGradeLevel = gradeLevel ?? existingLedger.GradeLevel ?? student.GradeLevel;
                    if (!string.IsNullOrWhiteSpace(ledgerGradeLevel))
                    {
                        _logger?.LogInformation(
                            "Ledger {LedgerId} exists but has no charges. Populating charges for grade level {GradeLevel}",
                            existingLedger.Id, ledgerGradeLevel);
                        await PopulateInitialChargesAsync(existingLedger.Id, ledgerGradeLevel);
                        await RecalculateTotalsAsync(existingLedger.Id);
                        
                        // Reload ledger with charges
                        return await _context.StudentLedgers
                            .Include(l => l.Charges)
                            .Include(l => l.Payments)
                            .FirstAsync(l => l.Id == existingLedger.Id);
                    }
                }
                
                // Recalculate totals in case charges/payments were modified
                await RecalculateTotalsAsync(existingLedger.Id);
                return existingLedger;
            }

            var finalGradeLevel = gradeLevel ?? student.GradeLevel;
            if (string.IsNullOrWhiteSpace(finalGradeLevel))
            {
                throw new Exception($"Cannot determine grade level for student {studentId}.");
            }

            // Create new ledger
            var ledger = new StudentLedger
            {
                StudentId = studentId,
                SchoolYear = activeSchoolYear,
                GradeLevel = finalGradeLevel,
                Status = "Unpaid",
                TotalCharges = 0,
                TotalPayments = 0,
                Balance = 0,
                CreatedAt = DateTime.Now
            };

            _context.StudentLedgers.Add(ledger);
            await _context.SaveChangesAsync();

            // Populate initial charges (Tuition, Misc, Other fees)
            await PopulateInitialChargesAsync(ledger.Id, finalGradeLevel);

            // Recalculate totals after adding charges
            await RecalculateTotalsAsync(ledger.Id);

            _logger?.LogInformation(
                "Created ledger for student {StudentId}, school year {SchoolYear}, grade {GradeLevel}",
                studentId, activeSchoolYear, finalGradeLevel);

            return await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .FirstAsync(l => l.Id == ledger.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting or creating ledger for student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Populates initial charges (Tuition, Misc, Other) for a ledger
    /// </summary>
    private async Task PopulateInitialChargesAsync(int ledgerId, string gradeLevel)
    {
        try
        {
            // Get fee for grade level - improved matching logic
            var gradeLevels = await _feeService.GetAllGradeLevelsAsync();
            var normalizedGradeLevel = gradeLevel.Trim();
            
            // Try multiple matching strategies
            var gradeLevelEntity = gradeLevels.FirstOrDefault(g =>
            {
                var dbGradeName = g.GradeLevelName.Trim();
                
                // Exact match (case-insensitive)
                if (dbGradeName.Equals(normalizedGradeLevel, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Match without "Grade " prefix
                var dbGradeNoPrefix = dbGradeName.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                var inputGradeNoPrefix = normalizedGradeLevel.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (dbGradeNoPrefix.Equals(inputGradeNoPrefix, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Match just the number (e.g., "1" matches "Grade 1")
                if (int.TryParse(dbGradeNoPrefix, out var dbNum) && 
                    int.TryParse(inputGradeNoPrefix, out var inputNum) && 
                    dbNum == inputNum)
                    return true;
                
                // Match "G1" format
                if (dbGradeName.StartsWith("G", StringComparison.OrdinalIgnoreCase) && 
                    normalizedGradeLevel.StartsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    var dbGNum = dbGradeName.Substring(1).Trim();
                    var inputGNum = normalizedGradeLevel.Substring(1).Trim();
                    if (dbGNum.Equals(inputGNum, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                
                return false;
            });

            if (gradeLevelEntity == null)
            {
                _logger?.LogWarning(
                    "Grade level '{GradeLevel}' not found for ledger {LedgerId}. Available grade levels: {AvailableLevels}", 
                    gradeLevel, ledgerId, string.Join(", ", gradeLevels.Select(g => g.GradeLevelName)));
                return;
            }
            
            _logger?.LogInformation(
                "Matched grade level '{GradeLevel}' to database grade level '{DbGradeLevel}' (ID: {GradeLevelId}) for ledger {LedgerId}",
                gradeLevel, gradeLevelEntity.GradeLevelName, gradeLevelEntity.GradeLevelId, ledgerId);

            var fee = await _feeService.GetFeeByGradeLevelIdAsync(gradeLevelEntity.GradeLevelId);
            if (fee == null)
            {
                _logger?.LogWarning("Fee not found for grade level {GradeLevel} (ledger {LedgerId})", gradeLevel, ledgerId);
                return;
            }

            // Add Tuition charge
            if (fee.TuitionFee > 0)
            {
                _context.LedgerCharges.Add(new LedgerCharge
                {
                    LedgerId = ledgerId,
                    ChargeType = "Tuition",
                    Description = "Tuition Fee",
                    Amount = fee.TuitionFee,
                    CreatedAt = DateTime.Now
                });
            }

            // Add Misc charge
            if (fee.MiscFee > 0)
            {
                _context.LedgerCharges.Add(new LedgerCharge
                {
                    LedgerId = ledgerId,
                    ChargeType = "Misc",
                    Description = "Miscellaneous Fee",
                    Amount = fee.MiscFee,
                    CreatedAt = DateTime.Now
                });
            }

            // Add Other charge
            if (fee.OtherFee > 0)
            {
                _context.LedgerCharges.Add(new LedgerCharge
                {
                    LedgerId = ledgerId,
                    ChargeType = "Other",
                    Description = "Other Fees",
                    Amount = fee.OtherFee,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            
            // Verify all charges were created correctly
            var createdCharges = await _context.LedgerCharges
                .Where(c => c.LedgerId == ledgerId)
                .ToListAsync();
            
            var tuitionCreated = createdCharges.Any(c => c.ChargeType == "Tuition");
            var miscCreated = createdCharges.Any(c => c.ChargeType == "Misc");
            var otherCreated = createdCharges.Any(c => c.ChargeType == "Other");
            var totalCreated = createdCharges.Sum(c => c.Amount);
            var expectedTotal = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
            
            // Log charge creation for debugging
            _logger?.LogInformation(
                "Created charges for ledger {LedgerId}: Tuition={Tuition} (created={TuitionCreated}), Misc={Misc} (created={MiscCreated}), Other={Other} (created={OtherCreated}), ExpectedTotal={ExpectedTotal}, ActualTotal={ActualTotal}",
                ledgerId, fee.TuitionFee, tuitionCreated, fee.MiscFee, miscCreated, fee.OtherFee, otherCreated, expectedTotal, totalCreated);
            
            // Warn if totals don't match
            if (Math.Abs(totalCreated - expectedTotal) > 0.01m)
            {
                _logger?.LogWarning(
                    "Charge total mismatch for ledger {LedgerId}: Expected {ExpectedTotal}, but created charges sum to {ActualTotal}",
                    ledgerId, expectedTotal, totalCreated);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error populating initial charges for ledger {LedgerId}: {Message}", ledgerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Recalculates totals for a ledger based on charges and payments
    /// Must be called after any charge or payment changes
    /// Always calculates from actual charges and payments (not stored totals)
    /// </summary>
    public async Task RecalculateTotalsAsync(int ledgerId)
    {
        try
        {
            var ledger = await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.Id == ledgerId);

            if (ledger == null)
            {
                throw new Exception($"Ledger {ledgerId} not found.");
            }

            // Always calculate from actual charges and payments (not stored totals)
            var calculatedTotalCharges = ledger.Charges?.Sum(c => c.Amount) ?? 0m;
            var calculatedTotalPayments = ledger.Payments?.Sum(p => p.Amount) ?? 0m;
            var calculatedBalance = calculatedTotalCharges - calculatedTotalPayments;

            // Update stored totals
            ledger.TotalCharges = calculatedTotalCharges;
            ledger.TotalPayments = calculatedTotalPayments;
            ledger.Balance = calculatedBalance;

            // Update status based on calculated balance
            if (calculatedTotalPayments == 0)
            {
                ledger.Status = "Unpaid";
            }
            else if (calculatedBalance > 0)
            {
                ledger.Status = "Partially Paid";
            }
            else
            {
                ledger.Status = "Fully Paid";
            }

            ledger.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger?.LogInformation(
                "Recalculated ledger {LedgerId}: Charges={Charges}, Payments={Payments}, Balance={Balance}, Status={Status}",
                ledgerId, ledger.TotalCharges, ledger.TotalPayments, ledger.Balance, ledger.Status);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error recalculating totals for ledger {LedgerId}: {Message}", ledgerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Applies a discount to a ledger
    /// Discounts are stored as negative LedgerCharges
    /// </summary>
    public async Task ApplyDiscountAsync(int ledgerId, string discountType, decimal amount, string? description = null)
    {
        await ApplyDiscountAsync(ledgerId, discountType, amount, null, description);
    }

    /// <summary>
    /// Applies a discount to a ledger with optional discount configuration link
    /// Discounts are stored as negative LedgerCharges
    /// PREVENTS duplicate application of the same discount
    /// </summary>
    public async Task ApplyDiscountAsync(int ledgerId, string discountType, decimal amount, int? discountId = null, string? description = null)
    {
        try
        {
            if (amount <= 0)
            {
                throw new Exception("Discount amount must be greater than zero.");
            }

            var ledger = await _context.StudentLedgers
                .Include(l => l.Charges)
                .FirstOrDefaultAsync(l => l.Id == ledgerId);
            
            if (ledger == null)
            {
                throw new Exception($"Ledger {ledgerId} not found.");
            }

            // If discountId is provided, verify it exists and check for duplicates
            if (discountId.HasValue)
            {
                var discount = await _context.Discounts.FindAsync(discountId.Value);
                if (discount == null)
                {
                    throw new Exception($"Discount with ID {discountId.Value} not found.");
                }

                // CHECK: Prevent duplicate discount application
                // Check if this discount has already been applied to this ledger
                var existingDiscount = ledger.Charges?
                    .FirstOrDefault(c => c.ChargeType == "Discount" && 
                                        c.DiscountId.HasValue && 
                                        c.DiscountId.Value == discountId.Value);
                
                if (existingDiscount != null)
                {
                    throw new Exception($"Discount '{discount.DiscountName}' has already been applied to this ledger. Each discount can only be applied once per ledger.");
                }
            }

            // Create discount charge (negative amount)
            var discountCharge = new LedgerCharge
            {
                LedgerId = ledgerId,
                ChargeType = "Discount",
                Description = description ?? $"{discountType} Discount",
                Amount = -amount, // Negative for discounts
                DiscountId = discountId, // Link to discount configuration if provided
                CreatedAt = DateTime.Now
            };

            _context.LedgerCharges.Add(discountCharge);
            await _context.SaveChangesAsync();

            // Recalculate totals
            await RecalculateTotalsAsync(ledgerId);

            _logger?.LogInformation(
                "Applied {DiscountType} discount of Php {Amount} to ledger {LedgerId} (DiscountId: {DiscountId})",
                discountType, amount, ledgerId, discountId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying discount to ledger {LedgerId}: {Message}", ledgerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Adds a charge to a ledger
    /// </summary>
    public async Task AddChargeAsync(int ledgerId, string chargeType, decimal amount, string? description = null)
    {
        try
        {
            if (amount <= 0)
            {
                throw new Exception("Charge amount must be greater than zero.");
            }

            var ledger = await _context.StudentLedgers.FindAsync(ledgerId);
            if (ledger == null)
            {
                throw new Exception($"Ledger {ledgerId} not found.");
            }

            var charge = new LedgerCharge
            {
                LedgerId = ledgerId,
                ChargeType = chargeType,
                Description = description ?? chargeType,
                Amount = amount,
                CreatedAt = DateTime.Now
            };

            _context.LedgerCharges.Add(charge);
            await _context.SaveChangesAsync();

            // Recalculate totals
            await RecalculateTotalsAsync(ledgerId);

            _logger?.LogInformation(
                "Added {ChargeType} charge of Php {Amount} to ledger {LedgerId}",
                chargeType, amount, ledgerId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding charge to ledger {LedgerId}: {Message}", ledgerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Adds a payment to a ledger
    /// </summary>
    public async Task<LedgerPayment> AddPaymentAsync(int ledgerId, decimal amount, string orNumber, string paymentMethod, string? processedBy = null)
    {
        try
        {
            if (amount <= 0)
            {
                throw new Exception("Payment amount must be greater than zero.");
            }

            var ledger = await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.Id == ledgerId);

            if (ledger == null)
            {
                throw new Exception($"Ledger {ledgerId} not found.");
            }

            // Check if OR number already exists in either LedgerPayments or StudentPayments
            var existingLedgerPayment = await _context.LedgerPayments
                .FirstOrDefaultAsync(p => p.OrNumber == orNumber);
            
            var existingStudentPayment = await _context.StudentPayments
                .FirstOrDefaultAsync(p => p.OrNumber == orNumber);

            if (existingLedgerPayment != null || existingStudentPayment != null)
            {
                throw new Exception($"OR number {orNumber} already exists. Please use a different OR number.");
            }

            // Calculate actual balance from charges and payments (not stored totals)
            // This ensures we use the most up-to-date balance even if totals haven't been recalculated
            var actualTotalCharges = ledger.Charges?.Sum(c => c.Amount) ?? 0m;
            var actualTotalPayments = ledger.Payments?.Sum(p => p.Amount) ?? 0m;
            var actualBalance = actualTotalCharges - actualTotalPayments;

            // Check if payment exceeds balance using ACTUAL calculated balance
            if (amount > actualBalance)
            {
                throw new Exception($"Payment amount (Php {amount:N2}) exceeds balance (Php {actualBalance:N2}).");
            }

            var payment = new LedgerPayment
            {
                LedgerId = ledgerId,
                Amount = amount,
                OrNumber = orNumber,
                PaymentMethod = paymentMethod,
                ProcessedBy = processedBy,
                CreatedAt = DateTime.Now
            };

            _context.LedgerPayments.Add(payment);
            // Save LedgerPayment first to get the ID
            await _context.SaveChangesAsync();

            // Recalculate totals
            await RecalculateTotalsAsync(ledgerId);
            
            // Return payment so caller can create StudentPayment in same transaction

            _logger?.LogInformation(
                "Added payment of Php {Amount} (OR: {OrNumber}) to ledger {LedgerId}",
                amount, orNumber, ledgerId);

            return payment;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding payment to ledger {LedgerId}: {Message}", ledgerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets ledger for a student and school year
    /// Ensures charges are populated if ledger exists but has no charges
    /// </summary>
    public async Task<StudentLedger?> GetLedgerBySchoolYearAsync(string studentId, string schoolYear)
    {
        try
        {
            var ledger = await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.StudentId == studentId && l.SchoolYear == schoolYear);

            if (ledger != null)
            {
                // If ledger exists but has no charges, populate them now
                if (!ledger.Charges.Any())
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
                    var ledgerGradeLevel = ledger.GradeLevel ?? student?.GradeLevel;
                    
                    if (!string.IsNullOrWhiteSpace(ledgerGradeLevel))
                    {
                        _logger?.LogInformation(
                            "Ledger {LedgerId} exists but has no charges. Populating charges for grade level {GradeLevel}",
                            ledger.Id, ledgerGradeLevel);
                        await PopulateInitialChargesAsync(ledger.Id, ledgerGradeLevel);
                        await RecalculateTotalsAsync(ledger.Id);
                        
                        // Reload ledger with charges
                        return await _context.StudentLedgers
                            .Include(l => l.Charges)
                            .Include(l => l.Payments)
                            .FirstAsync(l => l.Id == ledger.Id);
                    }
                    else
                    {
                        _logger?.LogWarning(
                            "Ledger {LedgerId} exists but has no charges and cannot determine grade level for student {StudentId}",
                            ledger.Id, studentId);
                    }
                }
                else
                {
                    // Recalculate totals in case charges/payments were modified
                    await RecalculateTotalsAsync(ledger.Id);
                    
                    // Reload ledger to ensure totals are up to date
                    ledger = await _context.StudentLedgers
                        .Include(l => l.Charges)
                        .Include(l => l.Payments)
                        .FirstAsync(l => l.Id == ledger.Id);
                }
            }

            return ledger;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting ledger for student {StudentId}, school year {SchoolYear}: {Message}", 
                studentId, schoolYear, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets ledger by ID
    /// Ensures charges are populated if ledger exists but has no charges
    /// </summary>
    public async Task<StudentLedger?> GetLedgerByIdAsync(int ledgerId)
    {
        try
        {
            var ledger = await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.Id == ledgerId);

            if (ledger != null)
            {
                // If ledger exists but has no charges, populate them now
                if (!ledger.Charges.Any())
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == ledger.StudentId);
                    var ledgerGradeLevel = ledger.GradeLevel ?? student?.GradeLevel;
                    
                    if (!string.IsNullOrWhiteSpace(ledgerGradeLevel))
                    {
                        _logger?.LogInformation(
                            "Ledger {LedgerId} exists but has no charges. Populating charges for grade level {GradeLevel}",
                            ledger.Id, ledgerGradeLevel);
                        await PopulateInitialChargesAsync(ledger.Id, ledgerGradeLevel);
                        await RecalculateTotalsAsync(ledger.Id);
                        
                        // Reload ledger with charges
                        return await _context.StudentLedgers
                            .Include(l => l.Charges)
                            .Include(l => l.Payments)
                            .FirstAsync(l => l.Id == ledger.Id);
                    }
                    else
                    {
                        _logger?.LogWarning(
                            "Ledger {LedgerId} exists but has no charges and cannot determine grade level for student {StudentId}",
                            ledger.Id, ledger.StudentId);
                    }
                }
                else
                {
                    // Recalculate totals in case charges/payments were modified
                    await RecalculateTotalsAsync(ledger.Id);
                    
                    // Reload ledger to ensure totals are up to date
                    ledger = await _context.StudentLedgers
                        .Include(l => l.Charges)
                        .Include(l => l.Payments)
                        .FirstAsync(l => l.Id == ledger.Id);
                }
            }

            return ledger;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting ledger by ID {LedgerId}: {Message}", ledgerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets all ledgers for a student
    /// </summary>
    public async Task<List<StudentLedger>> GetAllLedgersAsync(string studentId)
    {
        try
        {
            return await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .Where(l => l.StudentId == studentId)
                .OrderByDescending(l => l.SchoolYear)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all ledgers for student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets ledger with outstanding balance from previous school years
    /// Returns the most recent previous ledger with balance > 0
    /// </summary>
    public async Task<StudentLedger?> GetPreviousBalanceLedgerAsync(string studentId, string currentSchoolYear)
    {
        try
        {
            return await _context.StudentLedgers
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .Where(l => l.StudentId == studentId && 
                           l.SchoolYear != currentSchoolYear &&
                           l.Balance > 0)
                .OrderByDescending(l => l.SchoolYear)
                .ThenByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting previous balance ledger for student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets aging report - all ledgers with outstanding balances
    /// </summary>
    public async Task<List<StudentLedger>> GetAgingReportAsync()
    {
        try
        {
            return await _context.StudentLedgers
                .Include(l => l.Student)
                .Include(l => l.Charges)
                .Include(l => l.Payments)
                .Where(l => l.Balance > 0)
                .OrderByDescending(l => l.SchoolYear)
                .ThenBy(l => l.StudentId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting aging report: {Message}", ex.Message);
            throw;
        }
    }
}

