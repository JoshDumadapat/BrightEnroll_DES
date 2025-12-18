using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class ArchivedStudentSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<ArchivedStudentSeeder>? _logger;

    public ArchivedStudentSeeder(AppDbContext context, ILogger<ArchivedStudentSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync(int count = 50)
    {
        try
        {
            _logger?.LogInformation("=== STARTING ARCHIVED STUDENT SEEDING ===");

            var archivedStatuses = new[] { "Rejected by School", "Application Withdrawn", "Withdrawn", "Graduated", "Transferred" };
            var archiveReasons = new Dictionary<string, string>
            {
                { "Rejected by School", "Did not meet admission requirements" },
                { "Application Withdrawn", "Parent requested withdrawal of application" },
                { "Withdrawn", "Student withdrew from school" },
                { "Graduated", "Successfully completed grade level" },
                { "Transferred", "Transferred to another school" }
            };

            // Check if archived students already exist
            var existingCount = await _context.Students
                .Where(s => archivedStatuses.Contains(s.Status ?? ""))
                .CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Archived students already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var studentsToCreate = count - existingCount;
            var random = new Random();
            var firstNames = new[] { "Adrian", "Bella", "Cyrus", "Diana", "Ethan", "Faith", "Gavin", "Hope", "Isaac", "Joy", "Kai", "Lily", "Mason", "Nora", "Oscar", "Penny", "Quinn", "Ruby", "Simon", "Tara" };
            var lastNames = new[] { "Acosta", "Beltran", "Cortez", "Domingo", "Enriquez", "Ferrer", "Gonzales", "Herrera", "Ignacio", "Jose", "Kalaw", "Luna", "Mallari", "Nolasco", "Ortega", "Pascual", "Quiambao", "Reyes", "Salazar", "Tolentino" };
            var genders = new[] { "Male", "Female" };
            var studentTypes = new[] { "New", "Transferee", "Returning" };
            var gradeLevels = new[] { "Pre-School", "Kinder", "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6" };

            // Get grade levels from database
            var gradeLevelList = await _context.GradeLevels
                .Where(g => g.IsActive)
                .ToListAsync();

            if (!gradeLevelList.Any())
            {
                _logger?.LogWarning("No grade levels found. Please seed grade levels first.");
                return;
            }

            // Get or create guardians
            var guardians = await _context.Guardians.ToListAsync();
            if (guardians.Count < studentsToCreate)
            {
                var guardiansToCreate = studentsToCreate - guardians.Count;
                var newGuardians = new List<Guardian>();

                for (int i = 0; i < guardiansToCreate; i++)
                {
                    var guardian = new Guardian
                    {
                        FirstName = $"Guardian{guardians.Count + i + 1}",
                        MiddleName = null,
                        LastName = lastNames[random.Next(lastNames.Length)],
                        Suffix = null,
                        ContactNum = $"09{random.Next(100000000, 999999999)}",
                        Relationship = "Parent"
                    };
                    newGuardians.Add(guardian);
                }

                _context.Guardians.AddRange(newGuardians);
                await _context.SaveChangesAsync();
                guardians.AddRange(newGuardians);
            }

            var students = new List<Student>();
            var currentYear = DateTime.Now.Year;
            var schoolYear = $"{currentYear - 1}-{currentYear}"; // Previous school year for archived students

            // Get the highest student ID to continue from
            var allStudentIds = await _context.Students
                .Where(s => s.StudentId.Length == 6)
                .Select(s => s.StudentId)
                .ToListAsync();

            int maxId = 0;
            foreach (var id in allStudentIds)
            {
                if (int.TryParse(id, out int idValue) && idValue > maxId)
                {
                    maxId = idValue;
                }
            }

            for (int i = 0; i < studentsToCreate; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var gender = genders[random.Next(genders.Length)];
                var studentType = studentTypes[random.Next(studentTypes.Length)];
                var gradeLevel = gradeLevels[random.Next(gradeLevels.Length)];
                var status = archivedStatuses[random.Next(archivedStatuses.Length)];
                var archiveReason = archiveReasons[status];

                var birthdate = new DateTime(random.Next(2010, 2020), random.Next(1, 13), random.Next(1, 29));
                var age = DateTime.Today.Year - birthdate.Year;
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                var studentId = (maxId + i + 1).ToString("D6");
                var guardian = guardians[random.Next(guardians.Count)];

                var student = new Student
                {
                    StudentId = studentId,
                    FirstName = firstName,
                    MiddleName = null,
                    LastName = lastName,
                    Suffix = null,
                    Birthdate = birthdate,
                    Age = age,
                    PlaceOfBirth = "Manila",
                    Sex = gender,
                    MotherTongue = "Filipino",
                    IpComm = random.Next(2) == 0,
                    IpSpecify = null,
                    FourPs = random.Next(2) == 0,
                    FourPsHseId = null,
                    HseNo = $"{random.Next(1, 999)}",
                    Street = $"{random.Next(1, 999)} Main Street",
                    Brngy = "Barangay " + random.Next(1, 100),
                    Province = "Metro Manila",
                    City = "Manila",
                    Country = "Philippines",
                    ZipCode = $"{random.Next(1000, 9999)}",
                    PhseNo = null,
                    Pstreet = null,
                    Pbrngy = null,
                    Pprovince = null,
                    Pcity = null,
                    Pcountry = null,
                    PzipCode = null,
                    StudentType = studentType,
                    Lrn = $"LRN{random.Next(100000, 999999)}",
                    SchoolYr = schoolYear,
                    GradeLevel = gradeLevel,
                    GuardianId = guardian.GuardianId,
                    DateRegistered = DateTime.Now.AddDays(-random.Next(180, 730)), // Older dates for archived
                    Status = status,
                    ArchiveReason = archiveReason,
                    AmountPaid = status == "Graduated" ? random.Next(5000, 50000) : 0,
                    PaymentStatus = status == "Graduated" ? "Fully Paid" : "Unpaid",
                    UpdatedAt = DateTime.Now.AddDays(-random.Next(1, 180))
                };

                students.Add(student);
            }

            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== ARCHIVED STUDENT SEEDING COMPLETED: {studentsToCreate} archived students created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding archived students: {Message}", ex.Message);
            throw new Exception($"Failed to seed archived students: {ex.Message}", ex);
        }
    }
}
