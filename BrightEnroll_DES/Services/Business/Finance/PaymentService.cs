using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

public class PaymentService
{
    private readonly AppDbContext _context;
    private readonly FeeService _feeService;
    private readonly ILogger<PaymentService>? _logger;

    public PaymentService(AppDbContext context, FeeService feeService, ILogger<PaymentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _feeService = feeService ?? throw new ArgumentNullException(nameof(feeService));
        _logger = logger;
    }

    /// <summary>
    /// Search for a student by ID and get their payment information
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

            // Get the active school year FIRST
            var activeSchoolYear = await _context.SchoolYears
                .Where(sy => sy.IsActive && sy.IsOpen)
                .Select(sy => sy.SchoolYearName)
                .FirstOrDefaultAsync();

            // Get the enrollment for the ACTIVE school year to determine grade level
            // This ensures we get the correct grade level for the current school year, not a closed one
            var activeEnrollment = await _context.StudentSectionEnrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s.GradeLevel)
                .Where(e => e.StudentId == studentId && 
                           (!string.IsNullOrEmpty(activeSchoolYear) ? e.SchoolYear == activeSchoolYear : true))
                .OrderByDescending(e => e.SchoolYear)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            // Get fee for student's grade level for the ACTIVE school year
            // For re-enrolled students, we need to calculate the next grade level if no enrollment exists yet
            decimal totalFee = 0;
            string? gradeLevelName = null;

            if (activeEnrollment?.Section?.GradeLevel != null)
            {
                // Student has enrollment for active school year - use that grade level
                gradeLevelName = activeEnrollment.Section.GradeLevel.GradeLevelName;
                var fee = await _feeService.GetFeeByGradeLevelIdAsync(activeEnrollment.Section.GradeLevel.GradeLevelId);
                if (fee != null)
                {
                    totalFee = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                }
            }
            else
            {
                // No enrollment for active school year - check if student has previous enrollments (re-enrolled student)
                var previousEnrollments = await _context.StudentSectionEnrollments
                    .Include(e => e.Section)
                        .ThenInclude(s => s.GradeLevel)
                    .Where(e => e.StudentId == studentId)
                    .OrderByDescending(e => e.SchoolYear)
                    .ThenByDescending(e => e.CreatedAt)
                    .ToListAsync();

                if (previousEnrollments.Any())
                {
                    // Re-enrolled student - calculate next grade level from most recent enrollment
                    var mostRecentEnrollment = previousEnrollments.First();
                    if (mostRecentEnrollment.Section?.GradeLevel != null)
                    {
                        var currentGradeLevel = mostRecentEnrollment.Section.GradeLevel.GradeLevelName;
                        // Calculate next grade level (e.g., Grade 1 → Grade 2)
                        // We'll use a simple calculation here - in production, use ReEnrollmentService
                        gradeLevelName = CalculateNextGradeLevel(currentGradeLevel);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(student.GradeLevel))
                {
                    // New student - use their registered grade level
                    gradeLevelName = student.GradeLevel;
                }

                // Get fee for the calculated/registered grade level
                if (!string.IsNullOrWhiteSpace(gradeLevelName))
                {
                    var gradeLevels = await _feeService.GetAllGradeLevelsAsync();
                    var targetGradeLevel = gradeLevelName.Trim();
                    
                    var gradeLevel = gradeLevels.FirstOrDefault(g => 
                    {
                        var gradeName = g.GradeLevelName.Trim();
                        // Exact match
                        if (gradeName.Equals(targetGradeLevel, StringComparison.OrdinalIgnoreCase))
                            return true;
                        
                        // Match without "Grade " prefix
                        var gradeNameNoPrefix = gradeName.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                        var targetGradeNoPrefix = targetGradeLevel.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                        if (gradeNameNoPrefix.Equals(targetGradeNoPrefix, StringComparison.OrdinalIgnoreCase))
                            return true;
                        
                        // Match just the number (e.g., "1" matches "Grade 1")
                        if (int.TryParse(gradeNameNoPrefix, out var gradeNum) && 
                            int.TryParse(targetGradeNoPrefix, out var targetNum) && 
                            gradeNum == targetNum)
                            return true;
                        
                        return false;
                    });

                    if (gradeLevel != null)
                    {
                        var fee = await _feeService.GetFeeByGradeLevelIdAsync(gradeLevel.GradeLevelId);
                        if (fee != null)
                        {
                            totalFee = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Fee not found for grade level: {GradeLevel} (Student ID: {StudentId})", 
                            gradeLevelName, student.StudentId);
                    }
                }
            }

            // Use active school year for payment calculations
            // This ensures we only show payments and fees for the current school year
            var schoolYearForPayment = activeSchoolYear;
            if (string.IsNullOrEmpty(schoolYearForPayment))
            {
                // Fallback to enrollment's school year or student's school year
                schoolYearForPayment = activeEnrollment?.SchoolYear ?? student.SchoolYr;
            }

            // Sum payments for this school year only
            decimal amountPaid = 0;
            if (!string.IsNullOrEmpty(schoolYearForPayment))
            {
                amountPaid = await _context.StudentPayments
                    .Where(p => p.StudentId == studentId && p.SchoolYear == schoolYearForPayment)
                    .SumAsync(p => p.Amount);
            }

            decimal balance = totalFee - amountPaid;
            
            // Determine payment status based on balance
            string paymentStatus = "Unpaid";
            if (balance <= 0 && amountPaid > 0)
            {
                paymentStatus = "Fully Paid";
            }
            else if (amountPaid > 0)
            {
                paymentStatus = "Partially Paid";
            }

            // Determine enrollment status based on payment
            string enrollmentStatus = DetermineEnrollmentStatus(paymentStatus);

            return new StudentPaymentInfo
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {(!string.IsNullOrWhiteSpace(student.MiddleName) ? student.MiddleName + " " : "")}{student.LastName}{(!string.IsNullOrWhiteSpace(student.Suffix) ? " " + student.Suffix : "")}".Trim(),
                GradeLevel = gradeLevelName ?? student.GradeLevel ?? "N/A",
                TotalFee = totalFee,
                AmountPaid = amountPaid,
                Balance = balance,
                PaymentStatus = paymentStatus,
                EnrollmentStatus = DetermineEnrollmentStatus(paymentStatus),
                Student = student
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student payment info for {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Process a payment for a student
    /// </summary>
    public async Task<StudentPaymentInfo> ProcessPaymentAsync(string studentId, decimal paymentAmount, string paymentMethod, string orNumber, string? processedBy = null)
    {
        try
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                throw new Exception($"Student with ID {studentId} not found.");
            }

            if (paymentAmount <= 0)
            {
                throw new Exception("Payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(orNumber))
            {
                throw new Exception("OR number is required.");
            }

            // Validate OR number uniqueness
            var existingPayment = await _context.StudentPayments
                .FirstOrDefaultAsync(p => p.OrNumber == orNumber);

            if (existingPayment != null)
            {
                throw new Exception($"OR number {orNumber} already exists. Please use a different OR number.");
            }

            // Minimum payment validation
            const decimal MINIMUM_PAYMENT = 1700m;
            if (paymentAmount < MINIMUM_PAYMENT)
            {
                throw new Exception($"Minimum payment amount is Php {MINIMUM_PAYMENT:N2}. Please enter at least Php {MINIMUM_PAYMENT:N2}.");
            }

            // Get current payment info to calculate new balance
            var currentInfo = await GetStudentPaymentInfoAsync(studentId);
            if (currentInfo == null)
            {
                throw new Exception($"Could not retrieve payment information for student {studentId}.");
            }

            // Check if payment exceeds balance
            if (paymentAmount > currentInfo.Balance)
            {
                throw new Exception($"Payment amount (Php {paymentAmount:N2}) exceeds balance (Php {currentInfo.Balance:N2}).");
            }

            // ALWAYS use the active school year for new payments
            // This ensures payments are linked to the current school year, not a closed one
            var activeSchoolYear = await _context.SchoolYears
                .Where(sy => sy.IsActive && sy.IsOpen)
                .Select(sy => sy.SchoolYearName)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(activeSchoolYear))
            {
                throw new Exception("No active school year is open. Please open a school year before processing payments.");
            }

            // Get enrollment for the active school year (if exists)
            // This is used for fee calculation, but payment is ALWAYS linked to active school year
            var activeEnrollment = await _context.StudentSectionEnrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s.GradeLevel)
                .Where(e => e.StudentId == studentId && e.SchoolYear == activeSchoolYear)
                .FirstOrDefaultAsync();

            // Payment MUST be linked to the active school year
            // Never use student.SchoolYr as it's historical data from previous enrollments
            var paymentSchoolYear = activeSchoolYear;

            // Create payment record
            var payment = new StudentPayment
            {
                StudentId = studentId,
                Amount = paymentAmount,
                PaymentMethod = paymentMethod,
                OrNumber = orNumber,
                ProcessedBy = processedBy,
                SchoolYear = paymentSchoolYear,
                CreatedAt = DateTime.Now
            };

            _logger?.LogInformation(
                "Processing payment for student {StudentId}. School Year: {SchoolYear} (Active School Year). Enrollment exists: {HasEnrollment}",
                studentId, paymentSchoolYear, activeEnrollment != null ? "Yes" : "No");

            // Calculate existing payments for this school year BEFORE adding the new payment
            // This ensures we get the correct sum from the database
            var existingPaymentsForSchoolYear = await _context.StudentPayments
                .Where(p => p.StudentId == studentId && p.SchoolYear == paymentSchoolYear)
                .SumAsync(p => p.Amount);
            
            // Add the new payment amount to the total
            var totalPaymentsForSchoolYear = existingPaymentsForSchoolYear + paymentAmount;
            
            // Get fee for the active school year
            // Use enrollment if exists, otherwise calculate from student's next grade level (for re-enrolled students)
            decimal totalFeeForSchoolYear = 0;
            
            if (activeEnrollment?.Section?.GradeLevel != null)
            {
                // Student has enrollment for active school year - use that grade level's fee
                var fee = await _feeService.GetFeeByGradeLevelIdAsync(activeEnrollment.Section.GradeLevel.GradeLevelId);
                if (fee != null)
                {
                    totalFeeForSchoolYear = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                }
            }
            else
            {
                // No enrollment yet - calculate fee based on next grade level (for re-enrolled students)
                // Get most recent enrollment to determine current grade
                var previousEnrollments = await _context.StudentSectionEnrollments
                    .Include(e => e.Section)
                        .ThenInclude(s => s.GradeLevel)
                    .Where(e => e.StudentId == studentId)
                    .OrderByDescending(e => e.SchoolYear)
                    .ThenByDescending(e => e.CreatedAt)
                    .ToListAsync();

                if (previousEnrollments.Any())
                {
                    // Re-enrolled student - calculate next grade level
                    var mostRecentEnrollment = previousEnrollments.First();
                    if (mostRecentEnrollment.Section?.GradeLevel != null)
                    {
                        var currentGradeLevel = mostRecentEnrollment.Section.GradeLevel.GradeLevelName;
                        var nextGradeLevel = CalculateNextGradeLevel(currentGradeLevel);
                        
                        if (!string.IsNullOrWhiteSpace(nextGradeLevel))
                        {
                            var gradeLevels = await _feeService.GetAllGradeLevelsAsync();
                            var gradeLevel = gradeLevels.FirstOrDefault(g => 
                                g.GradeLevelName.Trim().Equals(nextGradeLevel.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                g.GradeLevelName.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim()
                                    .Equals(nextGradeLevel.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim(), StringComparison.OrdinalIgnoreCase));
                            
                            if (gradeLevel != null)
                            {
                                var fee = await _feeService.GetFeeByGradeLevelIdAsync(gradeLevel.GradeLevelId);
                                if (fee != null)
                                {
                                    totalFeeForSchoolYear = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                                }
                            }
                        }
                    }
                }
                
                // Fallback to currentInfo if calculation failed
                if (totalFeeForSchoolYear == 0)
                {
                    totalFeeForSchoolYear = currentInfo.TotalFee;
                }
            }
            
            // Calculate new balance including the payment being processed
            decimal newBalance = totalFeeForSchoolYear - totalPaymentsForSchoolYear;
            
            // Add payment to context
            _context.StudentPayments.Add(payment);
            
            // Determine new status based on this school year's balance
            string newStatus = student.Status; // Keep current status as default
            if (newBalance <= 0 && totalPaymentsForSchoolYear > 0)
            {
                newStatus = "Fully Paid";
            }
            else if (totalPaymentsForSchoolYear > 0)
            {
                newStatus = "Partially Paid";
            }
            
            // Update student status if it changed
            // Note: We update student.Status but NOT student.AmountPaid or student.PaymentStatus
            // These should remain as historical data or be calculated per school year
            if (student.Status != newStatus)
            {
                student.Status = newStatus;
            }

            await _context.SaveChangesAsync();
            
            _logger?.LogInformation(
                "Payment processed for student {StudentId}: Amount {Amount}, School Year {SchoolYear}, " +
                "Total Paid: {TotalPaid}, Balance: {Balance}, Status updated to: {Status}",
                studentId, paymentAmount, paymentSchoolYear, totalPaymentsForSchoolYear, newBalance, newStatus);

            _logger?.LogInformation("Payment processed for student {StudentId}: Amount {Amount}, Method {Method}, OR Number {OrNumber}", 
                studentId, paymentAmount, paymentMethod, orNumber);

            // Return updated payment info
            return await GetStudentPaymentInfoAsync(studentId) ?? currentInfo;
        }
        catch (Exception ex)
        {
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

            return result.OrderBy(s => s.StudentId).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all students with payment status: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Determine enrollment status based on payment status
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
    /// Get all payments for a specific student
    /// </summary>
    public async Task<List<StudentPayment>> GetPaymentsByStudentIdAsync(string studentId)
    {
        try
        {
            return await _context.StudentPayments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
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
    public async Task<StudentPayment?> GetLatestPaymentAsync(string studentId)
    {
        try
        {
            return await _context.StudentPayments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
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
    public async Task<StudentPayment?> GetPaymentByOrNumberAsync(string orNumber)
    {
        try
        {
            return await _context.StudentPayments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.OrNumber == orNumber);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting payment by OR number {OrNumber}: {Message}", orNumber, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get payment information for a student for a specific school year
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

            // Get enrollment for this school year (needed for grade level and date matching)
            var enrollment = await _context.StudentSectionEnrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s.GradeLevel)
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SchoolYear == schoolYear);

            // Get all payments for this school year
            // First, try to get payments with matching school_year
            var paymentsWithSchoolYear = await _context.StudentPayments
                .Where(p => p.StudentId == studentId && p.SchoolYear == schoolYear)
                .ToListAsync();

            decimal amountPaid = paymentsWithSchoolYear.Sum(p => p.Amount);

            // Log for debugging
            _logger?.LogInformation(
                "Found {Count} payments with school_year={SchoolYear} for student {StudentId}. Total: {Amount}",
                paymentsWithSchoolYear.Count, schoolYear, studentId, amountPaid);

            // Also get payments with NULL school_year that might belong to this school year
            // We'll match them based on the enrollment date range
            if (enrollment != null)
            {
                // Get all enrollments to determine date ranges
                var allEnrollments = await _context.StudentSectionEnrollments
                    .Where(e => e.StudentId == studentId)
                    .OrderBy(e => e.SchoolYear)
                    .ToListAsync();

                var paymentsWithoutSchoolYear = await _context.StudentPayments
                    .Where(p => p.StudentId == studentId && p.SchoolYear == null)
                    .ToListAsync();

                // Find the enrollment index for this school year
                var currentEnrollmentIndex = allEnrollments.FindIndex(e => e.SchoolYear == schoolYear);
                
                if (currentEnrollmentIndex >= 0)
                {
                    var enrollmentStartDate = enrollment.CreatedAt;
                    // End date is either the next enrollment's creation date, or current date if this is the latest
                    var enrollmentEndDate = currentEnrollmentIndex < allEnrollments.Count - 1
                        ? allEnrollments[currentEnrollmentIndex + 1].CreatedAt
                        : DateTime.Now.AddDays(1); // Add buffer for current enrollment

                    // For same-day enrollments, if this is the earlier enrollment, use end of day
                    // If this is the later enrollment, use start of day
                    if (currentEnrollmentIndex < allEnrollments.Count - 1 &&
                        allEnrollments[currentEnrollmentIndex + 1].CreatedAt.Date == enrollment.CreatedAt.Date)
                    {
                        // Same day - use time component to distinguish
                        enrollmentEndDate = enrollment.CreatedAt.Date.AddDays(1);
                    }

                    var matchedPayments = paymentsWithoutSchoolYear
                        .Where(p => p.CreatedAt >= enrollmentStartDate && p.CreatedAt < enrollmentEndDate)
                        .ToList();

                    amountPaid += matchedPayments.Sum(p => p.Amount);

                    // Log for debugging
                    if (matchedPayments.Any())
                    {
                        _logger?.LogInformation(
                            "Matched {Count} payments without school_year to school year {SchoolYear} for student {StudentId}. Total matched amount: {Amount}",
                            matchedPayments.Count, schoolYear, studentId, matchedPayments.Sum(p => p.Amount));
                    }
                }
            }

            decimal totalFee = 0;
            string gradeLevel = "N/A";

            if (enrollment?.Section?.GradeLevel != null)
            {
                gradeLevel = enrollment.Section.GradeLevel.GradeLevelName;
                var fee = await _feeService.GetFeeByGradeLevelIdAsync(enrollment.Section.GradeLevel.GradeLevelId);
                if (fee != null)
                {
                    totalFee = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                }
            }
            else
            {
                // Fallback to current student grade level
                if (!string.IsNullOrWhiteSpace(student.GradeLevel))
                {
                    gradeLevel = student.GradeLevel;
                    var gradeLevels = await _feeService.GetAllGradeLevelsAsync();
                    var gradeLevelEntity = gradeLevels.FirstOrDefault(g => 
                        g.GradeLevelName.Trim().Equals(student.GradeLevel.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        g.GradeLevelName.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim()
                            .Equals(student.GradeLevel.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim(), StringComparison.OrdinalIgnoreCase));

                    if (gradeLevelEntity != null)
                    {
                        var fee = await _feeService.GetFeeByGradeLevelIdAsync(gradeLevelEntity.GradeLevelId);
                        if (fee != null)
                        {
                            totalFee = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                        }
                    }
                }
            }

            decimal balance = totalFee - amountPaid;
            
            // Determine payment status
            string paymentStatus = "Unpaid";
            if (balance <= 0 && amountPaid > 0)
            {
                paymentStatus = "Fully Paid";
            }
            else if (amountPaid > 0)
            {
                paymentStatus = "Partially Paid";
            }

            return new StudentPaymentInfo
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {(!string.IsNullOrWhiteSpace(student.MiddleName) ? student.MiddleName + " " : "")}{student.LastName}{(!string.IsNullOrWhiteSpace(student.Suffix) ? " " + student.Suffix : "")}".Trim(),
                GradeLevel = gradeLevel,
                TotalFee = totalFee,
                AmountPaid = amountPaid,
                Balance = balance,
                PaymentStatus = paymentStatus,
                EnrollmentStatus = DetermineEnrollmentStatus(paymentStatus),
                Student = student
            };
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
}

