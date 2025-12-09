using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

public class PaymentService
{
    private readonly AppDbContext _context;
    private readonly FeeService _feeService;
    private readonly JournalEntryService _journalEntryService;
    private readonly ILogger<PaymentService>? _logger;

    public PaymentService(AppDbContext context, FeeService feeService, JournalEntryService journalEntryService, ILogger<PaymentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _feeService = feeService ?? throw new ArgumentNullException(nameof(feeService));
        _journalEntryService = journalEntryService ?? throw new ArgumentNullException(nameof(journalEntryService));
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

            // Get fee for student's grade level
            decimal totalFee = 0;
            if (!string.IsNullOrWhiteSpace(student.GradeLevel))
            {
                // Try to find grade level by name - handle various formats
                var gradeLevels = await _feeService.GetAllGradeLevelsAsync();
                var studentGradeLevel = student.GradeLevel.Trim();
                
                var gradeLevel = gradeLevels.FirstOrDefault(g => 
                {
                    var gradeName = g.GradeLevelName.Trim();
                    // Exact match
                    if (gradeName.Equals(studentGradeLevel, StringComparison.OrdinalIgnoreCase))
                        return true;
                    
                    // Match without "Grade " prefix
                    var gradeNameNoPrefix = gradeName.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                    var studentGradeNoPrefix = studentGradeLevel.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (gradeNameNoPrefix.Equals(studentGradeNoPrefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                    
                    // Match just the number (e.g., "1" matches "Grade 1")
                    if (int.TryParse(gradeNameNoPrefix, out var gradeNum) && 
                        int.TryParse(studentGradeNoPrefix, out var studentNum) && 
                        gradeNum == studentNum)
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
                        student.GradeLevel, student.StudentId);
                }
            }

            decimal amountPaid = student.AmountPaid;
            decimal balance = totalFee - amountPaid;
            string paymentStatus = student.PaymentStatus ?? "Unpaid";

            // Determine enrollment status based on payment
            string enrollmentStatus = DetermineEnrollmentStatus(paymentStatus);

            return new StudentPaymentInfo
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {(!string.IsNullOrWhiteSpace(student.MiddleName) ? student.MiddleName + " " : "")}{student.LastName}{(!string.IsNullOrWhiteSpace(student.Suffix) ? " " + student.Suffix : "")}".Trim(),
                GradeLevel = student.GradeLevel ?? "N/A",
                TotalFee = totalFee,
                AmountPaid = amountPaid,
                Balance = balance,
                PaymentStatus = paymentStatus,
                EnrollmentStatus = enrollmentStatus,
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

            // Create payment record
            var payment = new StudentPayment
            {
                StudentId = studentId,
                Amount = paymentAmount,
                PaymentMethod = paymentMethod,
                OrNumber = orNumber,
                ProcessedBy = processedBy,
                CreatedAt = DateTime.Now
            };

            _context.StudentPayments.Add(payment);

            // Update student payment information
            student.AmountPaid += paymentAmount;
            
            // Update payment status
            decimal newBalance = currentInfo.Balance - paymentAmount;
            if (newBalance <= 0)
            {
                student.PaymentStatus = "Fully Paid";
                student.Status = "Fully Paid"; // Update enrollment status
            }
            else if (student.AmountPaid > 0)
            {
                student.PaymentStatus = "Partially Paid";
                student.Status = "Partially Paid"; // Update enrollment status
            }

            await _context.SaveChangesAsync();

            // Create journal entry for double-entry bookkeeping
            try
            {
                // Get user ID if processedBy is provided
                int? createdBy = null;
                if (!string.IsNullOrWhiteSpace(processedBy))
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == processedBy || u.SystemId == processedBy);
                    createdBy = user?.UserId;
                }

                await _journalEntryService.CreatePaymentJournalEntryAsync(payment, createdBy);
                _logger?.LogInformation("Journal entry created for payment {PaymentId}", payment.PaymentId);
            }
            catch (Exception ex)
            {
                // Log but don't fail the payment if journal entry creation fails
                _logger?.LogWarning(ex, "Failed to create journal entry for payment {PaymentId}: {Message}", payment.PaymentId, ex.Message);
            }

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

