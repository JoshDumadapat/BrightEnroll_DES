using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class EnrolledStudentSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<EnrolledStudentSeeder>? _logger;

    public EnrolledStudentSeeder(AppDbContext context, ILogger<EnrolledStudentSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync(int count = 50)
    {
        try
        {
            _logger?.LogInformation("=== STARTING ENROLLED STUDENT SEEDING ===");

            // Check if enrolled students already exist
            var existingCount = await _context.Students
                .Where(s => s.Status == "Enrolled")
                .CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Enrolled students already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var studentsToCreate = count - existingCount;
            var random = new Random();
            var firstNames = new[] { "Aaron", "Beatrice", "Christian", "Danielle", "Eduardo", "Francesca", "Gian", "Helena", "Ivan", "Jasmine", "Kyle", "Lara", "Matthew", "Nicole", "Oliver", "Patricia", "Quincy", "Rachel", "Samuel", "Theresa" };
            var lastNames = new[] { "Abad", "Bautista", "Cruz", "Dela Rosa", "Espiritu", "Fernandez", "Garcia", "Hernandez", "Ibarra", "Javier", "Kho", "Lopez", "Mendoza", "Nunez", "Ocampo", "Perez", "Quizon", "Ramos", "Santos", "Tan" };
            var genders = new[] { "Male", "Female" };
            var studentTypes = new[] { "New", "Transferee", "Returning" };
            var gradeLevels = new[] { "Pre-School", "Kinder", "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6" };

            // Get grade levels and sections from database
            var gradeLevelList = await _context.GradeLevels
                .Where(g => g.IsActive)
                .ToListAsync();

            if (!gradeLevelList.Any())
            {
                _logger?.LogWarning("No grade levels found. Please seed grade levels first.");
                return;
            }

            var sections = await _context.Sections
                .Include(s => s.GradeLevel)
                .ToListAsync();

            if (!sections.Any())
            {
                _logger?.LogWarning("No sections found. Please seed sections first.");
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
            var enrollments = new List<StudentSectionEnrollment>();
            var currentYear = DateTime.Now.Year;
            var schoolYear = $"{currentYear}-{currentYear + 1}";

            // Get the highest student ID to continue from
            var highestId = await _context.Students
                .Where(s => s.StudentId.Length == 6 && int.TryParse(s.StudentId, out _))
                .Select(s => s.StudentId)
                .ToListAsync();

            int maxId = 0;
            foreach (var id in highestId)
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

                var birthdate = new DateTime(random.Next(2010, 2020), random.Next(1, 13), random.Next(1, 29));
                var age = DateTime.Today.Year - birthdate.Year;
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                var studentId = (maxId + i + 1).ToString("D6");
                var guardian = guardians[random.Next(guardians.Count)];

                // Find matching section for the grade level
                var matchingSection = sections
                    .FirstOrDefault(s => s.GradeLevel?.GradeLevelName == gradeLevel);

                if (matchingSection == null)
                {
                    // Use first available section if no exact match
                    matchingSection = sections[random.Next(sections.Count)];
                }

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
                    DateRegistered = DateTime.Now.AddDays(-random.Next(1, 180)),
                    Status = "Enrolled",
                    ArchiveReason = null,
                    AmountPaid = random.Next(5000, 50000), // Enrolled students have paid
                    PaymentStatus = "Fully Paid",
                    UpdatedAt = DateTime.Now
                };

                students.Add(student);

                // Create enrollment record
                var enrollment = new StudentSectionEnrollment
                {
                    StudentId = studentId,
                    SectionId = matchingSection.SectionId,
                    SchoolYear = schoolYear,
                    Status = "Enrolled",
                    CreatedAt = student.DateRegistered,
                    UpdatedAt = DateTime.Now
                };

                enrollments.Add(enrollment);
            }

            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();

            _context.StudentSectionEnrollments.AddRange(enrollments);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== ENROLLED STUDENT SEEDING COMPLETED: {studentsToCreate} enrolled students created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding enrolled students: {Message}", ex.Message);
            throw new Exception($"Failed to seed enrolled students: {ex.Message}", ex);
        }
    }
}
