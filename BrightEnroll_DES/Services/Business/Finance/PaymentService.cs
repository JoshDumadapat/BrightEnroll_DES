using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Academic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

public class PaymentService
{
    private readonly AppDbContext _context;
    private readonly FeeService _feeService;
    private readonly StudentLedgerService _ledgerService;
    private readonly SchoolYearService _schoolYearService;
    private readonly JournalEntryService _journalEntryService;
    private readonly ILogger<PaymentService>? _logger;

    public PaymentService(
        AppDbContext context, 
        FeeService feeService, 
        StudentLedgerService ledgerService,
        SchoolYearService schoolYearService,
        JournalEntryService journalEntryService,
        ILogger<PaymentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _feeService = feeService ?? throw new ArgumentNullException(nameof(feeService));
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _schoolYearService = schoolYearService ?? throw new ArgumentNullException(nameof(schoolYearService));
        _journalEntryService = journalEntryService ?? throw new ArgumentNullException(nameof(journalEntryService));
        _logger = logger;
    }

    /// <summary>
    /// Search for a student by ID and get their payment information using ledger system.
    /// Priority: previous ledger with balance > 0, otherwise current/open school year ledger (if exists).
    /// </summary>
    public async Task<StudentPaymentInfo?> GetStudentPaymentInfoAsync(string studentId)
    {
        try
        {
            var student = await _context.Students
                .Include(s => s.Guardian)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                _logger?.LogWarning("Student not found: {StudentId}", studentId);
                return null;
            }

            var activeSchoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();

            // 1) If previous ledger has balance, show that
            if (!string.IsNullOrEmpty(activeSchoolYear))
            {
                var previousLedger = await _ledgerService.GetPreviousBalanceLedgerAsync(studentId, activeSchoolYear);
                if (previousLedger != null && previousLedger.Balance > 0)
                {
                    return await MapLedgerToPaymentInfoAsync(student, previousLedger, hasPreviousBalance: true, activeSchoolYear);
                }
            }

            // 2) Otherwise show current/open school year ledger (if exists, or create if needed)
            StudentLedger? currentLedger = null;
            if (!string.IsNullOrEmpty(activeSchoolYear))
            {
                currentLedger = await _ledgerService.GetLedgerBySchoolYearAsync(studentId, activeSchoolYear);
                
                // If no ledger exists for the current school year, create one automatically
                if (currentLedger == null)
                {
                    _logger?.LogInformation("No ledger found for student {StudentId} for school year {SchoolYear}. Creating new ledger.", studentId, activeSchoolYear);
                    currentLedger = await _ledgerService.GetOrCreateLedgerForCurrentSYAsync(studentId, student.GradeLevel);
                    _logger?.LogInformation("Created new ledger {LedgerId} for student {StudentId} for school year {SchoolYear}.", currentLedger.Id, studentId, activeSchoolYear);
                }
            }

            if (currentLedger != null)
            {
                return await MapLedgerToPaymentInfoAsync(student, currentLedger, hasPreviousBalance: false, activeSchoolYear);
            }

            // No ledger found and no active school year to create one
            _logger?.LogWarning("No ledger found for student {StudentId} and no active school year available.", studentId);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student payment info for {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Process a payment for a student using ledger system (no enrollment/status side effects)
    /// Uses database transaction to ensure atomicity of all operations
    /// </summary>
    public async Task<StudentPaymentInfo> ProcessPaymentAsync(string studentId, decimal paymentAmount, string paymentMethod, string orNumber, string? processedBy = null)
    {
        // Use database transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId)
                ?? throw new Exception($"Student with ID {studentId} not found.");

            if (paymentAmount <= 0)
            {
                throw new Exception("Payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(orNumber))
            {
                throw new Exception("OR number is required.");
            }

            // Minimum payment validation
            // NOTE: UI already validates minimum payment and allows exceptions when discounts are applied
            // This service-level validation is a safety net, but we allow amounts below minimum
            // since UI handles discount cases where amount due might be below minimum
            // The UI ensures minimum is enforced unless discount reduces amount due below minimum
            const decimal MINIMUM_PAYMENT = 1700m;
            // Only enforce minimum if amount is very small (likely an error)
            // Allow amounts below minimum since UI handles discount scenarios
            if (paymentAmount < 100m)
            {
                throw new Exception($"Payment amount is too small. Minimum payment is Php {MINIMUM_PAYMENT:N2}.");
            }

            // Get current payment info to determine which ledger to use
            var currentInfo = await GetStudentPaymentInfoAsync(studentId);
            if (currentInfo == null || !currentInfo.LedgerId.HasValue)
            {
                throw new Exception($"Could not retrieve a payable ledger for student {studentId}.");
            }

            // FIXED: Removed payment amount > balance validation
            // The UI now handles overpayment (POS-style) and calculates amountToApply
            // This service receives the amount to apply (already validated in UI)
            // Overpayment is handled at UI level - we only apply what's needed to the ledger
            // If paymentAmount > balance, we cap it at balance in the ledger calculation
            // This allows POS-style payments where student can pay more than balance (change is returned)
            
            // Ensure we don't apply more than the balance to the ledger
            decimal amountToApplyToLedger = paymentAmount;
            if (amountToApplyToLedger > currentInfo.Balance)
            {
                // Cap at balance - the excess will be change (handled in UI/receipt)
                amountToApplyToLedger = currentInfo.Balance;
                _logger?.LogWarning(
                    "Payment amount {PaymentAmount} exceeds balance {Balance} for student {StudentId}. " +
                    "Applying only {AmountToApply} to ledger. Excess will be change.",
                    paymentAmount, currentInfo.Balance, studentId, amountToApplyToLedger);
            }

            // Add payment to the target ledger (no status or enrollment changes)
            // Use amountToApplyToLedger (capped at balance) instead of paymentAmount
            var ledgerPayment = await _ledgerService.AddPaymentAsync(currentInfo.LedgerId.Value, amountToApplyToLedger, orNumber, paymentMethod, processedBy);

            // Get ledger to retrieve SchoolYear for StudentPayment
            var ledger = await _ledgerService.GetLedgerByIdAsync(currentInfo.LedgerId.Value);
            
            // Create StudentPayment record for Dashboard and Finance Reports
            // This MUST be in the same transaction to ensure data consistency
            if (ledger != null)
            {
                var studentPayment = new StudentPayment
                {
                    StudentId = studentId,
                    Amount = amountToApplyToLedger, // Record the amount actually applied to ledger
                    PaymentMethod = paymentMethod,
                    OrNumber = orNumber,
                    ProcessedBy = processedBy,
                    SchoolYear = ledger.SchoolYear ?? await _schoolYearService.GetActiveSchoolYearNameAsync(), // Set school year from ledger
                    CreatedAt = ledgerPayment.CreatedAt
                };

                _context.StudentPayments.Add(studentPayment);
                
                // Save StudentPayment first to get PaymentId
                await _context.SaveChangesAsync();
                
                _logger?.LogInformation(
                    "Created StudentPayment record for student {StudentId}, Amount {Amount}, SchoolYear {SchoolYear}",
                    studentId, amountToApplyToLedger, studentPayment.SchoolYear);
                
                // Create journal entry for double-entry bookkeeping
                // This ensures Balance Sheet and General Ledger reflect the payment
                try
                {
                    // Get the user ID from processedBy if it's a numeric string
                    int? createdByUserId = null;
                    if (!string.IsNullOrWhiteSpace(processedBy) && int.TryParse(processedBy, out int userId))
                    {
                        createdByUserId = userId;
                    }
                    
                    await _journalEntryService.CreatePaymentJournalEntryAsync(studentPayment, createdByUserId);
                    _logger?.LogInformation(
                        "Created journal entry for payment {PaymentId}, Student {StudentId}, Amount {Amount}",
                        studentPayment.PaymentId, studentId, amountToApplyToLedger);
                }
                catch (Exception journalEx)
                {
                    // Log error but don't fail the payment - journal entry creation is important but not critical
                    // This allows backward compatibility if Chart of Accounts isn't set up yet
                    _logger?.LogError(journalEx, 
                        "Failed to create journal entry for payment {PaymentId}, Student {StudentId}: {Message}",
                        studentPayment.PaymentId, studentId, journalEx.Message);
                    // Continue with payment processing even if journal entry fails
                }
            }
            else
            {
                _logger?.LogWarning("Could not retrieve ledger {LedgerId} to create StudentPayment record", currentInfo.LedgerId.Value);
            }

            // For current/active school year payments (not previous-balance cases), update student.Status and enrollment records
            if (!currentInfo.HasPreviousBalance)
            {
                var activeSchoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
                var updatedLedger = await _ledgerService.GetLedgerByIdAsync(currentInfo.LedgerId.Value);

                if (updatedLedger != null && !string.IsNullOrWhiteSpace(activeSchoolYear) &&
                    string.Equals(updatedLedger.SchoolYear, activeSchoolYear, StringComparison.OrdinalIgnoreCase))
                {
                    var newStatus = updatedLedger.Status; // "Unpaid", "Partially Paid", "Fully Paid"
                    
                    // Update student.Status to match ledger status
                    if (!string.Equals(student.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        student.Status = newStatus;
                    }

                    // Update StudentSectionEnrollment status for current school year to match payment status
                    var enrollments = await _context.StudentSectionEnrollments
                        .Where(e => e.StudentId == studentId && 
                                   e.SchoolYear == activeSchoolYear &&
                                   (e.Status == "For Payment" || e.Status == "Pending" || 
                                    e.Status == "Partially Paid" || e.Status == "Fully Paid" || 
                                    e.Status == "Unpaid"))
                        .ToListAsync();

                    foreach (var enrollment in enrollments)
                    {
                        var enrollmentStatus = newStatus switch
                        {
                            "Fully Paid" => "Fully Paid",
                            "Partially Paid" => "Partially Paid",
                            "Unpaid" => "For Payment",
                            _ => enrollment.Status // Keep existing if unknown status
                        };

                        if (!string.Equals(enrollment.Status, enrollmentStatus, StringComparison.OrdinalIgnoreCase))
                        {
                            enrollment.Status = enrollmentStatus;
                            enrollment.UpdatedAt = DateTime.Now;
                        }
                    }
                }
            }

            // Save all changes within transaction (StudentPayment already saved above if journal entry was created)
            // Only save again if there were other changes (student status, enrollment status updates)
            await _context.SaveChangesAsync();
            
            // Verify StudentPayment was created
            var createdStudentPayment = await _context.StudentPayments
                .FirstOrDefaultAsync(sp => sp.OrNumber == orNumber);
            
            if (createdStudentPayment != null)
            {
                _logger?.LogInformation(
                    "StudentPayment verified: PaymentId={PaymentId}, StudentId={StudentId}, Amount={Amount}, SchoolYear={SchoolYear}, OR={OrNumber}",
                    createdStudentPayment.PaymentId, createdStudentPayment.StudentId, 
                    createdStudentPayment.Amount, createdStudentPayment.SchoolYear, createdStudentPayment.OrNumber);
            }
            else
            {
                _logger?.LogWarning(
                    "StudentPayment NOT found after SaveChangesAsync for OR {OrNumber}. This may indicate a transaction issue.",
                    orNumber);
            }
            
            // Commit transaction
            await transaction.CommitAsync();

            _logger?.LogInformation(
                "Payment processed for student {StudentId}: Amount Applied {AmountApplied} (Requested: {RequestedAmount}), Method {Method}, OR Number {OrNumber}, Ledger {LedgerId}",
                studentId, amountToApplyToLedger, paymentAmount, paymentMethod, orNumber, currentInfo.LedgerId.Value);

            // Return updated payment info (GetStudentPaymentInfoAsync will reload and recalculate)
            // No need for manual reload/recalculation here as GetStudentPaymentInfoAsync handles it
            return await GetStudentPaymentInfoAsync(studentId) ?? currentInfo;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error processing payment for {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get all students with their payment status
    /// </summary>
    public async Task<List<StudentPaymentInfo>> GetAllStudentsWithPaymentStatusAsync()
    {
        try
        {
            var students = await _context.Students
                .Include(s => s.Guardian)
                .ToListAsync();

            var result = new List<StudentPaymentInfo>();

            foreach (var student in students)
            {
                var paymentInfo = await GetStudentPaymentInfoAsync(student.StudentId);
                if (paymentInfo != null)
                {
                    result.Add(paymentInfo);
                }
            }

            // Save any ledger updates that were made during mapping (status recalculation)
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception saveEx)
            {
                _logger?.LogWarning(saveEx, "Some ledger updates may not have been saved: {Message}", saveEx.Message);
                // Continue even if save fails - the calculated status is still correct in the returned data
            }

            return result.OrderBy(s => s.StudentId).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all students with payment status: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Determine enrollment status based on payment status (display only)
    /// </summary>
    private string DetermineEnrollmentStatus(string paymentStatus)
    {
        return paymentStatus switch
        {
            "Fully Paid" => "Fully Paid",
            "Partially Paid" => "Partially Paid",
            "Unpaid" => "For Payment",
            _ => "For Payment"
        };
    }

    /// <summary>
    /// Get all payments for a specific student from all ledgers
    /// </summary>
    public async Task<List<LedgerPayment>> GetPaymentsByStudentIdAsync(string studentId)
    {
        try
        {
            var ledgers = await _ledgerService.GetAllLedgersAsync(studentId);
            var allPayments = new List<LedgerPayment>();
            
            foreach (var ledger in ledgers)
            {
                allPayments.AddRange(ledger.Payments);
            }
            
            return allPayments.OrderByDescending(p => p.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting payments for student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get the latest payment for a specific student
    /// </summary>
    public async Task<LedgerPayment?> GetLatestPaymentAsync(string studentId)
    {
        try
        {
            var payments = await GetPaymentsByStudentIdAsync(studentId);
            return payments.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting latest payment for student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get payment by OR number (for receipt lookup)
    /// </summary>
    public async Task<LedgerPayment?> GetPaymentByOrNumberAsync(string orNumber)
    {
        try
        {
            var payment = await _context.LedgerPayments
                .Include(p => p.Ledger)
                .FirstOrDefaultAsync(p => p.OrNumber == orNumber);
            
            if (payment?.Ledger != null)
            {
                await _context.Entry(payment.Ledger)
                    .Reference(l => l.Student)
                    .LoadAsync();
            }
            
            return payment;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting payment by OR number {OrNumber}: {Message}", orNumber, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get payment information for a student for a specific school year using ledger system
    /// </summary>
    public async Task<StudentPaymentInfo?> GetStudentPaymentInfoBySchoolYearAsync(string studentId, string schoolYear)
    {
        try
        {
            var student = await _context.Students
                .Include(s => s.Guardian)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                _logger?.LogWarning("Student not found: {StudentId}", studentId);
                return null;
            }

            var ledger = await _ledgerService.GetLedgerBySchoolYearAsync(studentId, schoolYear);
            if (ledger == null)
            {
                return null;
            }

            // Get active school year to determine if this is the current school year
            var activeSchoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
            return await MapLedgerToPaymentInfoAsync(student, ledger, hasPreviousBalance: false, activeSchoolYear);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student payment info for {StudentId} in school year {SchoolYear}: {Message}", studentId, schoolYear, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Helper method to calculate next grade level (e.g., Grade 1 → Grade 2)
    /// </summary>
    private string? CalculateNextGradeLevel(string? currentGradeLevel)
    {
        if (string.IsNullOrWhiteSpace(currentGradeLevel))
            return null;

        // Check if original format contains "Grade" (case-insensitive)
        bool isGradeFormat = currentGradeLevel.Contains("Grade", StringComparison.OrdinalIgnoreCase);

        // Normalize the grade level
        var normalized = currentGradeLevel.ToUpper()
            .Replace(" ", "")
            .Replace("GRADE", "")
            .Trim();

        // Extract the grade number
        int? gradeNumber = null;
        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d+$"))
        {
            if (int.TryParse(normalized, out int num))
            {
                gradeNumber = num;
            }
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^G\d+$"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(normalized, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int num))
            {
                gradeNumber = num;
            }
        }
        else if (normalized == "KINDER" || normalized == "K")
        {
            gradeNumber = 0; // Kinder = 0, promotes to Grade 1
        }

        if (!gradeNumber.HasValue)
            return null;

        int nextGradeNum = gradeNumber.Value + 1;

        if (nextGradeNum > 6)
            return null; // Beyond Grade 6 - graduated

        // Return in the same format as input
        if (isGradeFormat)
        {
            return $"Grade {nextGradeNum}";
        }
        else
        {
            return $"G{nextGradeNum}";
        }
    }

    /// <summary>
    /// Maps a ledger to StudentPaymentInfo
    /// </summary>
    private async Task<StudentPaymentInfo> MapLedgerToPaymentInfoAsync(Student student, StudentLedger ledger, bool hasPreviousBalance, string? activeSchoolYear = null)
    {
        // Always reload payments and charges from database to ensure we have the latest data
        // This is critical after payment processing to avoid stale data
        await _context.Entry(ledger)
            .Collection(l => l.Payments)
            .Query()
            .LoadAsync();
        
        await _context.Entry(ledger)
            .Collection(l => l.Charges)
            .Query()
            .LoadAsync();

        // Recalculate totals to ensure status is up-to-date based on actual payments and charges
        var totalCharges = ledger.Charges?.Sum(c => c.Amount) ?? 0m;
        var totalPayments = ledger.Payments?.Sum(p => p.Amount) ?? 0m;
        var balance = totalCharges - totalPayments;

        // Determine payment status based on current calculations (not stored status)
        // This ensures the status is always accurate even if ledger.Status is stale
        string paymentStatus;
        if (totalPayments == 0)
        {
            paymentStatus = "Unpaid";
        }
        else if (balance > 0)
        {
            paymentStatus = "Partially Paid";
        }
        else
        {
            paymentStatus = "Fully Paid";
        }

        // Update ledger totals and status if they don't match calculated values
        // This ensures the database stays in sync
        bool needsUpdate = false;
        if (Math.Abs(ledger.TotalCharges - totalCharges) > 0.01m)
        {
            ledger.TotalCharges = totalCharges;
            needsUpdate = true;
        }
        if (Math.Abs(ledger.TotalPayments - totalPayments) > 0.01m)
        {
            ledger.TotalPayments = totalPayments;
            needsUpdate = true;
        }
        if (Math.Abs(ledger.Balance - balance) > 0.01m)
        {
            ledger.Balance = balance;
            needsUpdate = true;
        }
        if (ledger.Status != paymentStatus)
        {
            ledger.Status = paymentStatus;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            ledger.UpdatedAt = DateTime.Now;
            try
            {
                await _context.SaveChangesAsync();
                _logger?.LogInformation(
                    "Updated ledger {LedgerId} totals: Charges={Charges}, Payments={Payments}, Balance={Balance}, Status={Status}",
                    ledger.Id, ledger.TotalCharges, ledger.TotalPayments, ledger.Balance, ledger.Status);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving ledger updates for ledger {LedgerId}: {Message}", ledger.Id, ex.Message);
                // Don't throw - continue with calculated values even if save fails
            }
        }

        // Determine the correct grade level to display
        string gradeLevel;
        
        if (hasPreviousBalance)
        {
            // For previous balance ledgers, use the ledger's grade level (historical accuracy)
            gradeLevel = ledger.GradeLevel ?? student.GradeLevel ?? "N/A";
        }
        else
        {
            // For current school year ledgers, prioritize student's current grade level
            // This ensures re-enrolled students show their new grade level
            if (!string.IsNullOrWhiteSpace(activeSchoolYear) && ledger.SchoolYear == activeSchoolYear)
            {
                // Current school year - use student's current grade level (may have been updated via re-enrollment)
                gradeLevel = student.GradeLevel ?? ledger.GradeLevel ?? "N/A";
            }
            else
            {
                // Historical ledger or no active school year - use ledger's grade level
                gradeLevel = ledger.GradeLevel ?? student.GradeLevel ?? "N/A";
            }
        }
        
        return new StudentPaymentInfo
        {
            StudentId = student.StudentId,
            StudentName = $"{student.FirstName} {(!string.IsNullOrWhiteSpace(student.MiddleName) ? student.MiddleName + " " : "")}{student.LastName}{(!string.IsNullOrWhiteSpace(student.Suffix) ? " " + student.Suffix : "")}".Trim(),
            GradeLevel = gradeLevel,
            TotalFee = totalCharges,
            AmountPaid = totalPayments,
            Balance = balance,
            PaymentStatus = paymentStatus,
            EnrollmentStatus = DetermineEnrollmentStatus(paymentStatus),
            Student = student,
            HasPreviousBalance = hasPreviousBalance,
            PreviousSchoolYear = hasPreviousBalance ? ledger.SchoolYear : null,
            SchoolYearForPayment = ledger.SchoolYear,
            LedgerId = ledger.Id
        };
    }
}

/// <summary>
/// Student payment information DTO
/// </summary>
public class StudentPaymentInfo
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public string EnrollmentStatus { get; set; } = "For Payment";
    public Student? Student { get; set; }
    
    // Properties for handling previous school year balances
    public bool HasPreviousBalance { get; set; } = false;
    public string? PreviousSchoolYear { get; set; }
    public string? SchoolYearForPayment { get; set; } // The school year this payment info is for
    
    // Ledger system properties
    public int? LedgerId { get; set; }
}
