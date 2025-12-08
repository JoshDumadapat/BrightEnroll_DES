using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Authentication;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Business.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Students;

/// <summary>
/// Service for managing student re-enrollment, promotion, and eligibility processes.
/// Handles automatic grade level promotion, school year transitions, and eligibility marking.
/// </summary>
public class ReEnrollmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReEnrollmentService>? _logger;
    private readonly IAuthService? _authService;
    private readonly AuditLogService? _auditLogService;
    private readonly EnrollmentStatusService _enrollmentStatusService;
    private readonly SchoolYearService _schoolYearService;
    private readonly PaymentService? _paymentService;

    public ReEnrollmentService(
        AppDbContext context,
        EnrollmentStatusService enrollmentStatusService,
        SchoolYearService schoolYearService,
        ILogger<ReEnrollmentService>? logger = null,
        IAuthService? authService = null,
        AuditLogService? auditLogService = null,
        PaymentService? paymentService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _enrollmentStatusService = enrollmentStatusService ?? throw new ArgumentNullException(nameof(enrollmentStatusService));
        _schoolYearService = schoolYearService ?? throw new ArgumentNullException(nameof(schoolYearService));
        _logger = logger;
        _authService = authService;
        _auditLogService = auditLogService;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Gets the next grade level for promotion (e.g., G1 → G2, G2 → G3, Grade 1 → Grade 2)
    /// Handles multiple formats: "G1", "Grade 1", "Grade1", etc.
    /// </summary>
    public string? GetNextGradeLevel(string? currentGradeLevel)
    {
        if (string.IsNullOrWhiteSpace(currentGradeLevel))
            return null;

        // Check if original format contains "Grade" (case-insensitive)
        bool isGradeFormat = currentGradeLevel.Contains("Grade", StringComparison.OrdinalIgnoreCase);

        // Normalize the grade level - convert to uppercase first, then remove spaces and "GRADE"
        var normalized = currentGradeLevel.ToUpper()
            .Replace(" ", "")
            .Replace("GRADE", "")
            .Trim();

        // Extract the grade number
        int? gradeNumber = null;
        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d+$"))
        {
            // Pure number like "1", "2", etc.
            if (int.TryParse(normalized, out int num))
            {
                gradeNumber = num;
            }
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^G\d+$"))
        {
            // Format like "G1", "G2", etc.
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
        {
            _logger?.LogWarning("Unable to parse grade level: {GradeLevel}", currentGradeLevel);
            return null;
        }

        // Determine next grade
        int nextGradeNum = gradeNumber.Value + 1;

        if (nextGradeNum > 6)
        {
            return null; // Beyond Grade 6 - graduated
        }

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
    /// Gets the next school year (e.g., "2023-2024" → "2024-2025")
    /// </summary>
    public string? GetNextSchoolYear(string? currentSchoolYear)
    {
        if (string.IsNullOrWhiteSpace(currentSchoolYear))
            return null;

        var parts = currentSchoolYear.Split('-');
        if (parts.Length != 2)
            return null;

        if (int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
        {
            return $"{startYear + 1}-{endYear + 1}";
        }

        return null;
    }

    /// <summary>
    /// Gets the previous school year (e.g., "2024-2025" → "2023-2024")
    /// </summary>
    public string? GetPreviousSchoolYear(string? currentSchoolYear)
    {
        if (string.IsNullOrWhiteSpace(currentSchoolYear))
            return null;

        var parts = currentSchoolYear.Split('-');
        if (parts.Length != 2)
            return null;

        if (int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
        {
            return $"{startYear - 1}-{endYear - 1}";
        }

        return null;
    }

    /// <summary>
    /// Checks if a student has complete grades for all subjects (Q1-Q4) for a given school year
    /// </summary>
    public async Task<bool> HasCompleteGradesAsync(string studentId, string schoolYear, int sectionId)
    {
        try
        {
            // First, try to get subjects assigned to the section
            var sectionSubjectIds = await _context.SubjectSections
                .Where(ss => ss.SectionId == sectionId)
                .Select(ss => ss.SubjectId)
                .Distinct()
                .ToListAsync();

            // Get subjects the student actually has grades for
            var studentSubjectIds = await _context.Grades
                .Where(g => g.StudentId == studentId
                         && g.SectionId == sectionId
                         && g.SchoolYear == schoolYear)
                .Select(g => g.SubjectId)
                .Distinct()
                .ToListAsync();

            // Use section subjects if available, otherwise use student's actual subjects
            var subjectIds = sectionSubjectIds.Any() ? sectionSubjectIds : studentSubjectIds;

            if (!subjectIds.Any())
            {
                // If no subjects found in section assignment, check if student has any grades at all
                if (!studentSubjectIds.Any())
                {
                    _logger?.LogWarning(
                        "No subjects found for section {SectionId} and no grades found for student {StudentId} in school year {SchoolYear}",
                        sectionId, studentId, schoolYear);
                    return false;
                }
                // Use student's subjects if section has no subjects assigned
                subjectIds = studentSubjectIds;
                _logger?.LogInformation(
                    "No subjects assigned to section {SectionId}, using student's actual subjects: {SubjectCount} subjects",
                    sectionId, subjectIds.Count);
            }

            // Check if student has grades for all subjects for all quarters
            foreach (var subjectId in subjectIds)
            {
                var grades = await _context.Grades
                    .Where(g => g.StudentId == studentId
                             && g.SubjectId == subjectId
                             && g.SectionId == sectionId
                             && g.SchoolYear == schoolYear)
                    .ToListAsync();

                var hasQ1 = grades.Any(g => g.GradingPeriod == "Q1" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                var hasQ2 = grades.Any(g => g.GradingPeriod == "Q2" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                var hasQ3 = grades.Any(g => g.GradingPeriod == "Q3" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                var hasQ4 = grades.Any(g => g.GradingPeriod == "Q4" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);

                if (!hasQ1 || !hasQ2 || !hasQ3 || !hasQ4)
                {
                    _logger?.LogInformation(
                        "Student {StudentId} missing grades for subject {SubjectId}. Q1: {Q1}, Q2: {Q2}, Q3: {Q3}, Q4: {Q4}",
                        studentId, subjectId, hasQ1, hasQ2, hasQ3, hasQ4);
                    return false;
                }
            }

            _logger?.LogInformation(
                "Student {StudentId} has complete grades for all {SubjectCount} subjects (Q1-Q4) in school year {SchoolYear}",
                studentId, subjectIds.Count, schoolYear);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking complete grades for student {StudentId}: {Message}", studentId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Calculates the general average for a student in a given section and school year
    /// </summary>
    public async Task<decimal> CalculateGeneralAverageAsync(string studentId, int sectionId, string schoolYear)
    {
        try
        {
            var grades = await _context.Grades
                .Where(g => g.StudentId == studentId
                         && g.SectionId == sectionId
                         && g.SchoolYear == schoolYear)
                .ToListAsync();

            // Group by subject and calculate final grade per subject
            var subjectAverages = grades
                .GroupBy(g => g.SubjectId)
                .Select(g =>
                {
                    var q1 = g.FirstOrDefault(x => x.GradingPeriod == "Q1")?.FinalGrade ?? 0;
                    var q2 = g.FirstOrDefault(x => x.GradingPeriod == "Q2")?.FinalGrade ?? 0;
                    var q3 = g.FirstOrDefault(x => x.GradingPeriod == "Q3")?.FinalGrade ?? 0;
                    var q4 = g.FirstOrDefault(x => x.GradingPeriod == "Q4")?.FinalGrade ?? 0;

                    // Only calculate if all quarters have grades
                    if (q1 > 0 && q2 > 0 && q3 > 0 && q4 > 0)
                    {
                        return (q1 + q2 + q3 + q4) / 4.0m;
                    }
                    return 0m;
                })
                .Where(avg => avg > 0)
                .ToList();

            if (!subjectAverages.Any())
                return 0;

            return subjectAverages.Average();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating general average for student {StudentId}: {Message}", studentId, ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// Marks students as "Eligible" for re-enrollment after completing a school year.
    /// Checks for complete grades (Q1-Q4) and calculates final averages.
    /// </summary>
    public async Task<MarkEligibilityResult> MarkStudentsEligibleForReEnrollmentAsync(string completedSchoolYear, int? performedByUserId = null)
    {
        var result = new MarkEligibilityResult
        {
            CompletedSchoolYear = completedSchoolYear
        };

        try
        {
            // Get all enrolled students for the completed school year
            var enrolledStudents = await _context.StudentSectionEnrollments
                .Include(e => e.Student)
                .Include(e => e.Section)
                .Where(e => e.SchoolYear == completedSchoolYear && e.Status == "Enrolled")
                .ToListAsync();

            _logger?.LogInformation("Found {Count} enrolled students for school year {SchoolYear}", enrolledStudents.Count, completedSchoolYear);

            string? performedByName = "System";
            string? userRole = null;
            int? userId = null;

            if (performedByUserId.HasValue)
            {
                var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == performedByUserId.Value);
                if (userEntity != null)
                {
                    performedByName = $"{userEntity.FirstName} {userEntity.LastName}".Trim();
                    userRole = userEntity.UserRole;
                    userId = userEntity.UserId;
                }
            }
            else if (_authService?.CurrentUser != null)
            {
                var user = _authService.CurrentUser;
                performedByName = $"{user.first_name} {user.last_name}".Trim();
                userRole = user.user_role;
                userId = user.user_ID;
            }

            foreach (var enrollment in enrolledStudents)
            {
                try
                {
                    var student = enrollment.Student;
                    if (student == null)
                        continue;

                    // Check if student already has "Eligible" status
                    if (student.Status == "Eligible")
                    {
                        result.AlreadyEligible++;
                        continue;
                    }

                    // Check if student has complete grades
                    var hasCompleteGrades = await HasCompleteGradesAsync(
                        student.StudentId,
                        completedSchoolYear,
                        enrollment.SectionId);

                    if (!hasCompleteGrades)
                    {
                        result.IncompleteGrades++;
                        _logger?.LogInformation(
                            "Student {StudentId} does not have complete grades for school year {SchoolYear}",
                            student.StudentId, completedSchoolYear);
                        continue;
                    }

                    // Calculate general average
                    var generalAverage = await CalculateGeneralAverageAsync(
                        student.StudentId,
                        enrollment.SectionId,
                        completedSchoolYear);

                    // Check if passing (general average >= 75 is passing in DepEd)
                    if (generalAverage < 75)
                    {
                        result.FailedStudents++;
                        _logger?.LogInformation(
                            "Student {StudentId} failed with average {Average}",
                            student.StudentId, generalAverage);
                        continue;
                    }

                    // Mark as Eligible
                    var oldStatus = student.Status;
                    student.Status = "Eligible";
                    student.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Create status log
                    await _enrollmentStatusService.UpdateStudentStatusAsync(student.StudentId, "Eligible");

                    // Create audit log
                    if (_auditLogService != null)
                    {
                        await _auditLogService.CreateLogAsync(
                            action: "Mark Student Eligible for Re-Enrollment",
                            module: "Re-Enrollment",
                            description: $"Student {student.StudentId} ({student.FirstName} {student.LastName}) marked as Eligible for re-enrollment. " +
                                        $"Completed school year: {completedSchoolYear}, General Average: {generalAverage:F2}",
                            userName: performedByName,
                            userRole: userRole,
                            userId: userId,
                            status: "Success",
                            severity: "Medium");

                        await _auditLogService.CreateStudentRegistrationLogAsync(
                            studentId: student.StudentId,
                            studentName: $"{student.FirstName} {student.LastName}",
                            grade: student.GradeLevel,
                            studentStatus: "Eligible",
                            registrarId: userId,
                            registrarName: performedByName);
                    }

                    result.MarkedEligible++;
                    _logger?.LogInformation(
                        "Student {StudentId} marked as Eligible. Average: {Average}",
                        student.StudentId, generalAverage);
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    _logger?.LogError(ex,
                        "Error processing student {StudentId} for eligibility: {Message}",
                        enrollment.StudentId, ex.Message);
                }
            }

            result.Success = true;
            _logger?.LogInformation(
                "Mark eligibility process completed. Eligible: {Eligible}, Failed: {Failed}, Incomplete: {Incomplete}, Errors: {Errors}",
                result.MarkedEligible, result.FailedStudents, result.IncompleteGrades, result.Errors);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error marking students eligible for re-enrollment: {Message}", ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Re-enrolls a student to the next grade level and school year.
    /// Automatically promotes grade level, updates school year, and creates new enrollment record.
    /// </summary>
    public async Task<ReEnrollmentResult> ReEnrollStudentAsync(
        string studentId,
        int? sectionId = null,
        int? performedByUserId = null)
    {
        var result = new ReEnrollmentResult
        {
            StudentId = studentId
        };

        try
        {
            var student = await _context.Students
                .Include(s => s.SectionEnrollments)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Student {studentId} not found";
                return result;
            }

            // Note: Eligibility is now checked in the UI before re-enrollment
            // We allow re-enrollment for students from previous school years
            // The UI will show eligibility status based on grades and payment history

            // Get the most recent enrollment to determine previous school year and check balance
            var mostRecentEnrollmentForBalance = student.SectionEnrollments
                .OrderByDescending(e => e.SchoolYear)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            if (_paymentService != null && mostRecentEnrollmentForBalance != null)
            {
                // Check balance for the PREVIOUS school year (the one they were last enrolled in)
                // Students must have paid all balances from their previous enrollment before re-enrolling
                var enrollmentSchoolYear = mostRecentEnrollmentForBalance.SchoolYear;
                var paymentInfo = await _paymentService.GetStudentPaymentInfoBySchoolYearAsync(
                    studentId, 
                    enrollmentSchoolYear);
                
                if (paymentInfo != null && paymentInfo.Balance > 0)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Student {studentId} has outstanding balance of Php {paymentInfo.Balance:N2} for school year {enrollmentSchoolYear}. " +
                                        $"Total Fee: Php {paymentInfo.TotalFee:N2}, Amount Paid: Php {paymentInfo.AmountPaid:N2}. " +
                                        $"Please settle all balances from previous school year before re-enrollment.";
                    return result;
                }
            }

            // Get the most recent enrollment to determine current grade level and school year
            // This ensures we get the grade level from the enrollment record, not the student's main record
            var mostRecentEnrollment = student.SectionEnrollments
                .OrderByDescending(e => e.SchoolYear)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            string? currentGradeLevel = null;
            string? currentSchoolYear = null;

            if (mostRecentEnrollment != null)
            {
                // Load section to get grade level
                await _context.Entry(mostRecentEnrollment).Reference(e => e.Section).LoadAsync();
                if (mostRecentEnrollment.Section != null)
                {
                    await _context.Entry(mostRecentEnrollment.Section).Reference(s => s.GradeLevel).LoadAsync();
                    currentGradeLevel = mostRecentEnrollment.Section.GradeLevel?.GradeLevelName;
                }
                currentSchoolYear = mostRecentEnrollment.SchoolYear;
            }
            else
            {
                // Fallback to student's main record if no enrollment found
                currentGradeLevel = student.GradeLevel;
                currentSchoolYear = student.SchoolYr;
            }

            // Get next grade level
            var nextGradeLevel = GetNextGradeLevel(currentGradeLevel);
            if (nextGradeLevel == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot promote student from grade {currentGradeLevel}. Student may have graduated or grade level is invalid.";
                return result;
            }

            // Get the CURRENT active school year (not the next from previous)
            // Students are re-enrolling for the current school year, not necessarily the next sequential year
            var currentActiveSchoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
            if (string.IsNullOrWhiteSpace(currentActiveSchoolYear))
            {
                result.Success = false;
                result.ErrorMessage = "Cannot determine current active school year. Please ensure a school year is active.";
                return result;
            }

            // Check if enrollment already exists for current school year
            var existingEnrollment = await _context.StudentSectionEnrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SchoolYear == currentActiveSchoolYear);

            if (existingEnrollment != null)
            {
                result.Success = false;
                result.ErrorMessage = $"Student {studentId} already has an enrollment record for school year {currentActiveSchoolYear}";
                return result;
            }

            string? performedByName = "System";
            string? userRole = null;
            int? userId = null;

            if (performedByUserId.HasValue)
            {
                var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == performedByUserId.Value);
                if (userEntity != null)
                {
                    performedByName = $"{userEntity.FirstName} {userEntity.LastName}".Trim();
                    userRole = userEntity.UserRole;
                    userId = userEntity.UserId;
                }
            }
            else if (_authService?.CurrentUser != null)
            {
                var user = _authService.CurrentUser;
                performedByName = $"{user.first_name} {user.last_name}".Trim();
                userRole = user.user_role;
                userId = user.user_ID;
            }

            // Store previous values for logging
            var previousGradeLevel = currentGradeLevel;
            var previousSchoolYear = currentSchoolYear;
            
            // Update student's GradeLevel to the next grade level (e.g., Grade 1 → Grade 2)
            // This is needed so payment service can show the correct grade level for new enrollment
            // Note: We preserve historical enrollment records, but update the student's current grade level
            student.GradeLevel = nextGradeLevel;
            
            // Update student type to "Returnee" since they're re-enrolling
            // Only update if it's not already set to "Returnee" or "Old Student"
            if (string.IsNullOrWhiteSpace(student.StudentType) || 
                (!student.StudentType.Equals("Returnee", StringComparison.OrdinalIgnoreCase) &&
                 !student.StudentType.Equals("Old Student", StringComparison.OrdinalIgnoreCase)))
            {
                student.StudentType = "Returnee";
            }
            
            // Set status to "For Payment" - student needs to pay downpayment for new school year
            // Section assignment will happen after payment in the "For Enrollment" tab
            student.Status = "For Payment";
            
            // DO NOT reset AmountPaid or PaymentStatus - these are cumulative and should remain
            // Payment calculations will be done per school year using the payment records
            
            student.UpdatedAt = DateTime.Now;

            // DO NOT create enrollment record yet if no section is provided
            // Enrollment record will be created when registrar assigns section in "For Enrollment" tab
            // This prevents foreign key constraint violations (SectionId is required)
            if (sectionId.HasValue)
            {
                // Only create enrollment record if section is provided (for bulk operations)
                var newEnrollment = new StudentSectionEnrollment
                {
                    StudentId = studentId,
                    SectionId = sectionId.Value,
                    SchoolYear = currentActiveSchoolYear, // Use current active school year
                    Status = "Pending", // Pending until payment is made and enrollment is confirmed
                    CreatedAt = DateTime.Now
                };

                _context.StudentSectionEnrollments.Add(newEnrollment);
                result.NewEnrollmentCreated = true;
            }
            else
            {
                // No section provided - enrollment record will be created later when section is assigned
                // Student will appear in "For Enrollment" tab based on their Status = "For Payment"
                result.NewEnrollmentCreated = false;
            }

            await _context.SaveChangesAsync();

            // Create status log - student is now "For Payment" and will appear in "For Enrollment" tab
            // This will also automatically create a ledger for the current school year
            await _enrollmentStatusService.UpdateStudentStatusAsync(studentId, "For Payment");

                     var ledgerExists = await _context.StudentLedgers
                .AnyAsync(l => l.StudentId == studentId && l.SchoolYear == currentActiveSchoolYear);

            if (!ledgerExists)
            {
                // Get total fees for the NEXT grade level
                var feeService = new FeeService(_context);
                var totalFees = await feeService.CalculateTotalFeesAsync(nextGradeLevel);

                var newLedger = new StudentLedger
                {
                    StudentId = studentId,
                    SchoolYear = currentActiveSchoolYear,
                    GradeLevel = nextGradeLevel,
                    TotalCharges = totalFees,
                    TotalPayments = 0,
                    Balance = totalFees,
                    Status = "Unpaid",
                    CreatedAt = DateTime.Now
                };

                _context.StudentLedgers.Add(newLedger);
                await _context.SaveChangesAsync();

                _logger?.LogInformation(
                    "Created NEW LEDGER for student {StudentId} for SY {SY}, Grade {Grade}, Amount {Amount}",
                    studentId, currentActiveSchoolYear, nextGradeLevel, totalFees);
            }
            else
            {
                _logger?.LogWarning(
                    "Ledger for student {StudentId} in SY {SY} already exists. Skipping creation.",
                    studentId, currentActiveSchoolYear);
            }


            // Create audit log
            if (_auditLogService != null)
            {
                await _auditLogService.CreateLogAsync(
                    action: "Student Re-Enrollment",
                    module: "Re-Enrollment",
                    description: $"Student {studentId} ({student.FirstName} {student.LastName}) re-enrolled for {currentActiveSchoolYear}. " +
                                $"Promoted from {previousGradeLevel} to {nextGradeLevel}. " +
                                $"Status set to 'For Payment' - awaiting downpayment. " +
                                $"New independent enrollment record created for school year {currentActiveSchoolYear}.",
                    userName: performedByName,
                    userRole: userRole,
                    userId: userId,
                    status: "Success",
                    severity: "High");

                await _auditLogService.CreateStudentRegistrationLogAsync(
                    studentId: studentId,
                    studentName: $"{student.FirstName} {student.LastName}",
                    grade: nextGradeLevel,
                    studentStatus: "For Payment", // Updated to reflect new status
                    registrarId: userId,
                    registrarName: performedByName);
            }

            result.Success = true;
            result.PreviousGradeLevel = previousGradeLevel;
            result.NewGradeLevel = nextGradeLevel;
            result.PreviousSchoolYear = currentSchoolYear;
            result.NewSchoolYear = currentActiveSchoolYear; // Use current active school year

            _logger?.LogInformation(
                "Student {StudentId} successfully re-enrolled: {PreviousGrade} → {NewGrade}, {PreviousSY} → {NewSY}. Status: For Payment (awaiting downpayment)",
                studentId, previousGradeLevel, nextGradeLevel, currentSchoolYear, currentActiveSchoolYear);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error re-enrolling student {StudentId}: {Message}", studentId, ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Bulk re-enrolls multiple eligible students
    /// </summary>
    public async Task<BulkReEnrollmentResult> BulkReEnrollStudentsAsync(
        List<string> studentIds,
        Dictionary<string, int>? studentSectionMap = null, // studentId -> sectionId
        int? performedByUserId = null)
    {
        var result = new BulkReEnrollmentResult();

        foreach (var studentId in studentIds)
        {
            var sectionId = studentSectionMap?.GetValueOrDefault(studentId);
            var reEnrollResult = await ReEnrollStudentAsync(studentId, sectionId, performedByUserId);

            if (reEnrollResult.Success)
            {
                result.Successful++;
            }
            else
            {
                result.Failed++;
                result.FailedStudents.Add(new FailedReEnrollment
                {
                    StudentId = studentId,
                    ErrorMessage = reEnrollResult.ErrorMessage ?? "Unknown error"
                });
            }
        }

        result.TotalProcessed = studentIds.Count;
        return result;
    }
}

/// <summary>
/// Result of marking students as eligible for re-enrollment
/// </summary>
public class MarkEligibilityResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string CompletedSchoolYear { get; set; } = string.Empty;
    public int MarkedEligible { get; set; }
    public int AlreadyEligible { get; set; }
    public int FailedStudents { get; set; }
    public int IncompleteGrades { get; set; }
    public int Errors { get; set; }
}

/// <summary>
/// Result of re-enrolling a student
/// </summary>
public class ReEnrollmentResult
{
    public bool Success { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? PreviousGradeLevel { get; set; }
    public string? NewGradeLevel { get; set; }
    public string? PreviousSchoolYear { get; set; }
    public string? NewSchoolYear { get; set; }
    public bool NewEnrollmentCreated { get; set; }
    public bool ExistingEnrollmentUpdated { get; set; }
}

/// <summary>
/// Result of bulk re-enrollment
/// </summary>
public class BulkReEnrollmentResult
{
    public int TotalProcessed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<FailedReEnrollment> FailedStudents { get; set; } = new();
}

/// <summary>
/// Information about a failed re-enrollment
/// </summary>
public class FailedReEnrollment
{
    public string StudentId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

