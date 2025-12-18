using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class TeacherAssignmentSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<TeacherAssignmentSeeder>? _logger;

    public TeacherAssignmentSeeder(AppDbContext context, ILogger<TeacherAssignmentSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync(int count = 30)
    {
        try
        {
            _logger?.LogInformation("=== STARTING TEACHER ASSIGNMENT SEEDING ===");

            // Check if teacher assignments already exist
            var existingCount = await _context.TeacherSectionAssignments
                .Where(t => !t.IsArchived)
                .CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Teacher assignments already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var assignmentsToCreate = count - existingCount;

            // Get teachers (users with Teacher role)
            var teachers = await _context.Users
                .Where(u => u.UserRole == "Teacher" && u.Status == "active")
                .ToListAsync();

            if (teachers.Count == 0)
            {
                _logger?.LogWarning("No teachers found. Please seed employees with Teacher role first.");
                return;
            }

            // Get sections
            var sections = await _context.Sections
                .Include(s => s.GradeLevel)
                .ToListAsync();

            if (sections.Count == 0)
            {
                _logger?.LogWarning("No sections found. Please seed sections first.");
                return;
            }

            // Get subjects
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .ToListAsync();

            if (subjects.Count == 0)
            {
                _logger?.LogWarning("No subjects found. Please seed subjects first.");
                return;
            }

            var assignments = new List<TeacherSectionAssignment>();
            var random = new Random();
            var roles = new[] { "adviser", "subject_teacher" };

            // Create assignments
            for (int i = 0; i < assignmentsToCreate; i++)
            {
                var teacher = teachers[random.Next(teachers.Count)];
                var section = sections[random.Next(sections.Count)];
                var role = roles[random.Next(roles.Length)];

                // For subject_teacher, assign a subject that matches the section's grade level
                int? subjectId = null;
                if (role == "subject_teacher")
                {
                    var matchingSubjects = subjects
                        .Where(s => s.GradeLevelId == section.GradeLevelId)
                        .ToList();

                    if (matchingSubjects.Any())
                    {
                        subjectId = matchingSubjects[random.Next(matchingSubjects.Count)].SubjectId;
                    }
                    else
                    {
                        // If no matching subject, use any subject
                        subjectId = subjects[random.Next(subjects.Count)].SubjectId;
                    }
                }

                // Check if this assignment already exists
                var exists = await _context.TeacherSectionAssignments
                    .AnyAsync(t => t.TeacherId == teacher.UserId &&
                                  t.SectionId == section.SectionId &&
                                  t.Role == role &&
                                  (role == "adviser" || t.SubjectId == subjectId) &&
                                  !t.IsArchived);

                if (!exists)
                {
                    var assignment = new TeacherSectionAssignment
                    {
                        TeacherId = teacher.UserId,
                        SectionId = section.SectionId,
                        SubjectId = subjectId,
                        Role = role,
                        IsArchived = false,
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 180)),
                        UpdatedAt = null
                    };

                    assignments.Add(assignment);
                }
            }

            if (assignments.Any())
            {
                _context.TeacherSectionAssignments.AddRange(assignments);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();

                _logger?.LogInformation($"=== TEACHER ASSIGNMENT SEEDING COMPLETED: {assignments.Count} assignments created ===");
            }
            else
            {
                _logger?.LogInformation("No new teacher assignments to create (all combinations already exist).");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding teacher assignments: {Message}", ex.Message);
            throw new Exception($"Failed to seed teacher assignments: {ex.Message}", ex);
        }
    }
}
