using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

public class CurriculumSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<CurriculumSeeder>? _logger;

    public CurriculumSeeder(AppDbContext context, ILogger<CurriculumSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Seeds all curriculum-related data
    public async Task SeedAllAsync()
    {
        try
        {
            _logger?.LogInformation("=== STARTING CURRICULUM SEEDING ===");
            
            await SeedGradeLevelsAsync();
            await SeedClassroomsAsync();
            await SeedSectionsAsync();
            await SeedSubjectsAsync();
            
            _logger?.LogInformation("=== CURRICULUM SEEDING COMPLETED SUCCESSFULLY ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding curriculum data: {Message}", ex.Message);
            throw new Exception($"Failed to seed curriculum data: {ex.Message}", ex);
        }
    }

    // Seeds grade levels
    private async Task SeedGradeLevelsAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding Grade Levels...");

            var gradeLevels = new List<GradeLevel>
            {
                new GradeLevel { GradeLevelName = "Pre-School", IsActive = true },
                new GradeLevel { GradeLevelName = "Kinder", IsActive = true },
                new GradeLevel { GradeLevelName = "Grade 1", IsActive = true },
                new GradeLevel { GradeLevelName = "Grade 2", IsActive = true },
                new GradeLevel { GradeLevelName = "Grade 3", IsActive = true },
                new GradeLevel { GradeLevelName = "Grade 4", IsActive = true },
                new GradeLevel { GradeLevelName = "Grade 5", IsActive = true },
                new GradeLevel { GradeLevelName = "Grade 6", IsActive = true }
            };

            foreach (var gradeLevel in gradeLevels)
            {
                var exists = await _context.GradeLevels
                    .AnyAsync(g => g.GradeLevelName == gradeLevel.GradeLevelName);
                
                if (!exists)
                {
                    _context.GradeLevels.Add(gradeLevel);
                    _logger?.LogInformation("Added Grade Level: {GradeLevelName}", gradeLevel.GradeLevelName);
                }
            }

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            _logger?.LogInformation("Grade Levels seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding grade levels: {Message}", ex.Message);
            throw;
        }
    }

    // Seeds classrooms for different grade levels
    private async Task SeedClassroomsAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding Classrooms...");

            var classrooms = new List<Classroom>
            {
                // Preschool Classrooms
                new Classroom
                {
                    RoomName = "Preschool Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 1,
                    RoomType = "Regular Classroom",
                    Capacity = 25,
                    Status = "Active",
                    Notes = "Preschool classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Preschool Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 1,
                    RoomType = "Regular Classroom",
                    Capacity = 25,
                    Status = "Active",
                    Notes = "Preschool classroom",
                    CreatedAt = DateTime.Now
                },
                // Kinder Classrooms
                new Classroom
                {
                    RoomName = "Kinder Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 1,
                    RoomType = "Regular Classroom",
                    Capacity = 30,
                    Status = "Active",
                    Notes = "Kindergarten classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Kinder Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 1,
                    RoomType = "Regular Classroom",
                    Capacity = 30,
                    Status = "Active",
                    Notes = "Kindergarten classroom",
                    CreatedAt = DateTime.Now
                },
                // Grade 1-3 Classrooms (Lower Elementary)
                new Classroom
                {
                    RoomName = "Grade 1 Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Regular Classroom",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Grade 1 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 1 Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Regular Classroom",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Grade 1 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 2 Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Regular Classroom",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Grade 2 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 2 Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Regular Classroom",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Grade 2 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 3 Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Regular Classroom",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Grade 3 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 3 Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Regular Classroom",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Grade 3 classroom",
                    CreatedAt = DateTime.Now
                },
                // Grade 4-6 Classrooms (Upper Elementary)
                new Classroom
                {
                    RoomName = "Grade 4 Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Regular Classroom",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Grade 4 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 4 Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Regular Classroom",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Grade 4 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 5 Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Regular Classroom",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Grade 5 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 5 Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Regular Classroom",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Grade 5 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 6 Room 1",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Regular Classroom",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Grade 6 classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Grade 6 Room 2",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Regular Classroom",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Grade 6 classroom",
                    CreatedAt = DateTime.Now
                },
                // Special Rooms
                new Classroom
                {
                    RoomName = "Computer Laboratory",
                    BuildingName = "Elementary Building",
                    FloorNumber = 2,
                    RoomType = "Laboratory",
                    Capacity = 30,
                    Status = "Active",
                    Notes = "Computer lab for all grade levels",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Science Laboratory",
                    BuildingName = "Elementary Building",
                    FloorNumber = 3,
                    RoomType = "Laboratory",
                    Capacity = 30,
                    Status = "Active",
                    Notes = "Science lab for upper elementary",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Music Room",
                    BuildingName = "Elementary Building",
                    FloorNumber = 1,
                    RoomType = "Special Room",
                    Capacity = 40,
                    Status = "Active",
                    Notes = "Music classroom",
                    CreatedAt = DateTime.Now
                },
                new Classroom
                {
                    RoomName = "Art Room",
                    BuildingName = "Elementary Building",
                    FloorNumber = 1,
                    RoomType = "Special Room",
                    Capacity = 35,
                    Status = "Active",
                    Notes = "Art classroom",
                    CreatedAt = DateTime.Now
                }
            };

            foreach (var classroom in classrooms)
            {
                var exists = await _context.Classrooms
                    .AnyAsync(c => c.RoomName == classroom.RoomName);
                
                if (!exists)
                {
                    _context.Classrooms.Add(classroom);
                    _logger?.LogInformation("Added Classroom: {RoomName}", classroom.RoomName);
                }
            }

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            _logger?.LogInformation("Classrooms seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding classrooms: {Message}", ex.Message);
            throw;
        }
    }

    // Seeds sections for each grade level
    private async Task SeedSectionsAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding Sections...");

            // Get all grade levels
            var gradeLevels = await _context.GradeLevels
                .Where(g => g.IsActive)
                .OrderBy(g => g.GradeLevelId)
                .ToListAsync();

            // Get all classrooms grouped by name pattern
            var allClassrooms = await _context.Classrooms
                .Where(c => c.Status == "Active")
                .ToListAsync();

            var sectionsAdded = 0;

            foreach (var gradeLevel in gradeLevels)
            {
                // Find matching classroom for this grade level
                var matchingClassroom = allClassrooms
                    .FirstOrDefault(c => c.RoomName.Contains(gradeLevel.GradeLevelName.Replace("Grade ", "Grade ")) ||
                                        (gradeLevel.GradeLevelName == "Preschool" && c.RoomName.Contains("Preschool")) ||
                                        (gradeLevel.GradeLevelName == "Kinder" && c.RoomName.Contains("Kinder")));

                // Create default section for each grade level
                var sectionName = $"{gradeLevel.GradeLevelName} - Section A";
                var exists = await _context.Sections
                    .AnyAsync(s => s.SectionName == sectionName && s.GradeLevelId == gradeLevel.GradeLevelId);
                
                if (!exists)
                {
                    var section = new Section
                    {
                        SectionName = sectionName,
                        GradeLevelId = gradeLevel.GradeLevelId,
                        ClassroomId = matchingClassroom?.RoomId,
                        AdviserId = null, // To be assigned later
                        Capacity = matchingClassroom?.Capacity ?? 35,
                        Notes = $"Default section for {gradeLevel.GradeLevelName}",
                        CreatedAt = DateTime.Now
                    };

                    _context.Sections.Add(section);
                    sectionsAdded++;
                    _logger?.LogInformation("Added Section: {SectionName} for {GradeLevel}", sectionName, gradeLevel.GradeLevelName);
                }
            }

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            _logger?.LogInformation($"Sections seeded successfully. Added {sectionsAdded} sections.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding sections: {Message}", ex.Message);
            throw;
        }
    }

    // Seeds subjects according to DepEd K-12 curriculum
    private async Task SeedSubjectsAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding Subjects...");

            // Get all grade levels
            var gradeLevels = await _context.GradeLevels
                .Where(g => g.IsActive)
                .ToListAsync();

            var gradeLevelDict = gradeLevels.ToDictionary(g => g.GradeLevelName, g => g.GradeLevelId);

            var subjects = new List<Subject>();

            // Preschool Subjects
            if (gradeLevelDict.TryGetValue("Pre-School", out int preschoolId))
            {
                subjects.AddRange(new[]
                {
                    new Subject { SubjectCode = "PRE-LANG", SubjectName = "Language and Literacy", GradeLevelId = preschoolId, Description = "Foundational language and literacy skills", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "PRE-MATH", SubjectName = "Mathematics", GradeLevelId = preschoolId, Description = "Basic mathematics concepts", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "PRE-PE", SubjectName = "Physical Education", GradeLevelId = preschoolId, Description = "Physical development and movement", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "PRE-ART", SubjectName = "Art", GradeLevelId = preschoolId, Description = "Creative arts and expression", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "PRE-MUSIC", SubjectName = "Music", GradeLevelId = preschoolId, Description = "Music and rhythm", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "PRE-SED", SubjectName = "Social and Emotional Development", GradeLevelId = preschoolId, Description = "Social skills and emotional intelligence", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "PRE-UTW", SubjectName = "Understanding the World", GradeLevelId = preschoolId, Description = "Exploring the environment and world", IsActive = true, CreatedAt = DateTime.Now }
                });
            }

            // Kindergarten Subjects
            if (gradeLevelDict.TryGetValue("Kinder", out int kinderId))
            {
                subjects.AddRange(new[]
                {
                    new Subject { SubjectCode = "K-LANG", SubjectName = "Language and Literacy", GradeLevelId = kinderId, Description = "Language and literacy development", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "K-MATH", SubjectName = "Mathematics", GradeLevelId = kinderId, Description = "Mathematics fundamentals", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "K-PE", SubjectName = "Physical Education", GradeLevelId = kinderId, Description = "Physical development activities", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "K-ART", SubjectName = "Art", GradeLevelId = kinderId, Description = "Arts and crafts", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "K-MUSIC", SubjectName = "Music", GradeLevelId = kinderId, Description = "Music appreciation and activities", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "K-SED", SubjectName = "Social and Emotional Development", GradeLevelId = kinderId, Description = "Social and emotional learning", IsActive = true, CreatedAt = DateTime.Now },
                    new Subject { SubjectCode = "K-UTW", SubjectName = "Understanding the World", GradeLevelId = kinderId, Description = "Science and social concepts", IsActive = true, CreatedAt = DateTime.Now }
                });
            }

            // Grade 1-3 Subjects (Lower Elementary - DepEd K-12)
            foreach (var gradeNum in new[] { 1, 2, 3 })
            {
                var gradeName = $"Grade {gradeNum}";
                if (gradeLevelDict.TryGetValue(gradeName, out int gradeId))
                {
                    subjects.AddRange(new[]
                    {
                        new Subject { SubjectCode = $"G{gradeNum}-MT", SubjectName = "Mother Tongue", GradeLevelId = gradeId, Description = "Mother tongue-based multilingual education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-FIL", SubjectName = "Filipino", GradeLevelId = gradeId, Description = "Filipino language", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-ENG", SubjectName = "English", GradeLevelId = gradeId, Description = "English language", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-MATH", SubjectName = "Mathematics", GradeLevelId = gradeId, Description = "Mathematics", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-AP", SubjectName = "Araling Panlipunan", GradeLevelId = gradeId, Description = "Social Studies", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-ESP", SubjectName = "Edukasyon sa Pagpapakatao", GradeLevelId = gradeId, Description = "Values Education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-MUSIC", SubjectName = "Music", GradeLevelId = gradeId, Description = "Music education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-ARTS", SubjectName = "Arts", GradeLevelId = gradeId, Description = "Arts education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-PE", SubjectName = "Physical Education", GradeLevelId = gradeId, Description = "Physical Education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-HEALTH", SubjectName = "Health", GradeLevelId = gradeId, Description = "Health education", IsActive = true, CreatedAt = DateTime.Now }
                    });
                }
            }

            // Grade 4-6 Subjects (Upper Elementary - DepEd K-12)
            foreach (var gradeNum in new[] { 4, 5, 6 })
            {
                var gradeName = $"Grade {gradeNum}";
                if (gradeLevelDict.TryGetValue(gradeName, out int gradeId))
                {
                    subjects.AddRange(new[]
                    {
                        new Subject { SubjectCode = $"G{gradeNum}-FIL", SubjectName = "Filipino", GradeLevelId = gradeId, Description = "Filipino language", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-ENG", SubjectName = "English", GradeLevelId = gradeId, Description = "English language", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-MATH", SubjectName = "Mathematics", GradeLevelId = gradeId, Description = "Mathematics", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-SCI", SubjectName = "Science", GradeLevelId = gradeId, Description = "Science", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-AP", SubjectName = "Araling Panlipunan", GradeLevelId = gradeId, Description = "Social Studies", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-ESP", SubjectName = "Edukasyon sa Pagpapakatao", GradeLevelId = gradeId, Description = "Values Education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-MUSIC", SubjectName = "Music", GradeLevelId = gradeId, Description = "Music education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-ARTS", SubjectName = "Arts", GradeLevelId = gradeId, Description = "Arts education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-PE", SubjectName = "Physical Education", GradeLevelId = gradeId, Description = "Physical Education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-HEALTH", SubjectName = "Health", GradeLevelId = gradeId, Description = "Health education", IsActive = true, CreatedAt = DateTime.Now },
                        new Subject { SubjectCode = $"G{gradeNum}-EPP", SubjectName = "Edukasyong Pantahanan at Pangkabuhayan", GradeLevelId = gradeId, Description = "Home Economics and Livelihood Education (EPP)", IsActive = true, CreatedAt = DateTime.Now }
                    });
                }
            }

            // Add subjects that don't exist yet
            foreach (var subject in subjects)
            {
                var exists = await _context.Subjects
                    .AnyAsync(s => s.SubjectCode == subject.SubjectCode && 
                                  s.GradeLevelId == subject.GradeLevelId);
                
                if (!exists)
                {
                    _context.Subjects.Add(subject);
                    _logger?.LogInformation("Added Subject: {SubjectName} ({SubjectCode}) for Grade Level ID {GradeLevelId}", 
                        subject.SubjectName, subject.SubjectCode, subject.GradeLevelId);
                }
            }

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            _logger?.LogInformation($"Subjects seeded successfully. Total subjects: {subjects.Count}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding subjects: {Message}", ex.Message);
            throw;
        }
    }
}
