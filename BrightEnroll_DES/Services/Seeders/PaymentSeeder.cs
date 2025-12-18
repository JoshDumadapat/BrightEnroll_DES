using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class PaymentSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentSeeder>? _logger;

    public PaymentSeeder(AppDbContext context, ILogger<PaymentSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger?.LogInformation("=== STARTING PAYMENT SEEDING ===");

            // Get enrolled students
            var enrolledStudents = await _context.Students
                .Where(s => s.Status == "Enrolled")
                .ToListAsync();

            if (!enrolledStudents.Any())
            {
                _logger?.LogWarning("No enrolled students found. Please seed enrolled students first.");
                return;
            }

            // Get current school year
            var currentYear = DateTime.Now.Year;
            var schoolYear = $"{currentYear}-{currentYear + 1}";

            // Get fees for enrolled students
            var fees = await _context.Fees
                .Where(f => f.SchoolYear == schoolYear || f.SchoolYear == null)
                .ToListAsync();

            var payments = new List<StudentPayment>();
            var random = new Random();
            var paymentMethods = new[] { "Cash", "Bank Transfer", "GCash", "PayMaya", "Check" };

            // Track used OR numbers to avoid duplicates
            var usedOrNumbers = new HashSet<string>();
            var existingOrNumbers = await _context.StudentPayments
                .Select(p => p.OrNumber)
                .ToListAsync();
            usedOrNumbers.UnionWith(existingOrNumbers);

            foreach (var student in enrolledStudents)
            {
                // Find matching fee for student's grade level
                var gradeLevel = await _context.GradeLevels
                    .FirstOrDefaultAsync(g => g.GradeLevelName == student.GradeLevel);

                if (gradeLevel == null) continue;

                var fee = fees.FirstOrDefault(f => f.GradeLevelId == gradeLevel.GradeLevelId);
                if (fee == null) continue;

                // Enrolled students should have payments
                // Create 1-3 payment records per student
                var paymentCount = random.Next(1, 4);
                var totalPaid = 0m;
                var totalFee = fee.TotalFee;

                for (int i = 0; i < paymentCount; i++)
                {
                    // Generate unique OR number
                    string orNumber;
                    do
                    {
                        var timestamp = DateTime.Now.AddDays(-random.Next(1, 180));
                        var randomSuffix = random.Next(1000, 9999);
                        orNumber = $"OR-{timestamp:yyyyMMdd}-{randomSuffix}";
                    } while (usedOrNumbers.Contains(orNumber));

                    usedOrNumbers.Add(orNumber);

                    // Calculate payment amount
                    decimal paymentAmount;
                    if (i == paymentCount - 1)
                    {
                        // Last payment covers remaining balance
                        paymentAmount = totalFee - totalPaid;
                    }
                    else
                    {
                        // Partial payments
                        var remaining = totalFee - totalPaid;
                        paymentAmount = Math.Min(remaining / (paymentCount - i), remaining * 0.4m);
                        paymentAmount = Math.Round(paymentAmount, 2);
                    }

                    if (paymentAmount <= 0) break;

                    totalPaid += paymentAmount;

                    var payment = new StudentPayment
                    {
                        StudentId = student.StudentId,
                        Amount = paymentAmount,
                        PaymentMethod = paymentMethods[random.Next(paymentMethods.Length)],
                        OrNumber = orNumber,
                        ProcessedBy = "System",
                        SchoolYear = schoolYear,
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 180))
                    };

                    payments.Add(payment);
                }

                // Update student's payment information
                student.AmountPaid = totalPaid;
                student.PaymentStatus = totalPaid >= totalFee ? "Fully Paid" : "Partially Paid";
                student.UpdatedAt = DateTime.Now;
            }

            _context.StudentPayments.AddRange(payments);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== PAYMENT SEEDING COMPLETED: {payments.Count} payments created for {enrolledStudents.Count} enrolled students ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding payments: {Message}", ex.Message);
            throw new Exception($"Failed to seed payments: {ex.Message}", ex);
        }
    }
}
