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
    private readonly ILogger<PaymentService>? _logger;

    public PaymentService(
        AppDbContext context, 
        FeeService feeService, 
        StudentLedgerService ledgerService,
        SchoolYearService schoolYearService,
        ILogger<PaymentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _feeService = feeService ?? throw new ArgumentNullException(nameof(feeService));
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _schoolYearService = schoolYearService ?? throw new ArgumentNullException(nameof(schoolYearService));
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
                    return MapLedgerToPaymentInfo(student, previousLedger, hasPreviousBalance: true);
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
                return MapLedgerToPaymentInfo(student, currentLedger, hasPreviousBalance: false);
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
    /// </summary>
    public async Task<StudentPaymentInfo> ProcessPaymentAsync(string studentId, decimal paymentAmount, string paymentMethod, string orNumber, string? processedBy = null)
    {
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

            // Minimum payment validation (retain existing behavior)
            const decimal MINIMUM_PAYMENT = 1700m;
            if (paymentAmount < MINIMUM_PAYMENT)
            {
                throw new Exception($"Minimum payment amount is Php {MINIMUM_PAYMENT:N2}. Please enter at least Php {MINIMUM_PAYMENT:N2}.");
            }

            // Get current payment info to determine which ledger to use
            var currentInfo = await GetStudentPaymentInfoAsync(studentId);
            if (currentInfo == null || !currentInfo.LedgerId.HasValue)
            {
                throw new Exception($"Could not retrieve a payable ledger for student {studentId}.");
            }

            // Check if payment exceeds balance
            if (paymentAmount > currentInfo.Balance)
            {
                throw new Exception($"Payment amount (Php {paymentAmount:N2}) exceeds balance (Php {currentInfo.Balance:N2}).");
            }

            // Add payment to the target ledger (no status or enrollment changes)
            await _ledgerService.AddPaymentAsync(currentInfo.LedgerId.Value, paymentAmount, orNumber, paymentMethod, processedBy);

            // For current/active school year payments (not previous-balance cases), update student.Status to reflect paid state
            if (!currentInfo.HasPreviousBalance)
            {
                var activeSchoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
                var updatedLedger = await _ledgerService.GetLedgerByIdAsync(currentInfo.LedgerId.Value);

                if (updatedLedger != null && !string.IsNullOrWhiteSpace(activeSchoolYear) &&
                    string.Equals(updatedLedger.SchoolYear, activeSchoolYear, StringComparison.OrdinalIgnoreCase))
                {
                    var newStatus = updatedLedger.Status; // "Unpaid", "Partially Paid", "Fully Paid"
                    if (!string.Equals(student.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        student.Status = newStatus;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            _logger?.LogInformation(
                "Payment processed for student {StudentId}: Amount {Amount}, Method {Method}, OR Number {OrNumber}, Ledger {LedgerId}",
                studentId, paymentAmount, paymentMethod, orNumber, currentInfo.LedgerId.Value);

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
            return await _context.LedgerPayments
                .Include(p => p.Ledger)
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(p => p.OrNumber == orNumber);
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

            return MapLedgerToPaymentInfo(student, ledger, hasPreviousBalance: false);
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
    private StudentPaymentInfo MapLedgerToPaymentInfo(Student student, StudentLedger ledger, bool hasPreviousBalance)
    {
        return new StudentPaymentInfo
        {
            StudentId = student.StudentId,
            StudentName = $"{student.FirstName} {(!string.IsNullOrWhiteSpace(student.MiddleName) ? student.MiddleName + " " : "")}{student.LastName}{(!string.IsNullOrWhiteSpace(student.Suffix) ? " " + student.Suffix : "")}".Trim(),
            GradeLevel = ledger.GradeLevel ?? student.GradeLevel ?? "N/A",
            TotalFee = ledger.TotalCharges,
            AmountPaid = ledger.TotalPayments,
            Balance = ledger.Balance,
            PaymentStatus = ledger.Status,
            EnrollmentStatus = DetermineEnrollmentStatus(ledger.Status),
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
