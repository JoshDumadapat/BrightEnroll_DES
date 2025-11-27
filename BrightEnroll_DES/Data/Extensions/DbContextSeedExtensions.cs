using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Seeders;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BrightEnroll_DES.Data.Extensions;

/// <summary>
/// Extension methods for seeding database after migrations
/// </summary>
public static class DbContextSeedExtensions
{
    /// <summary>
    /// Seeds initial data required for the application to function
    /// Call this after running migrations: await context.SeedDatabaseAsync(serviceProvider);
    /// </summary>
    public static async Task SeedDatabaseAsync(this AppDbContext context, IServiceProvider serviceProvider)
    {
        // Seed Grade Levels
        if (!await context.GradeLevels.AnyAsync())
        {
            var gradeLevels = new List<GradeLevel>
            {
                new GradeLevel { GradeLevelName = "Pre-School", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Kinder", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Grade 1", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Grade 2", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Grade 3", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Grade 4", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Grade 5", IsActive = true, IsSynced = false },
                new GradeLevel { GradeLevelName = "Grade 6", IsActive = true, IsSynced = false }
            };

            await context.GradeLevels.AddRangeAsync(gradeLevels);
            await context.SaveChangesAsync();
        }

        // Seed Default Admin User
        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        var exists = await userRepository.ExistsBySystemIdAsync("BDES-0001");
        
        if (!exists)
        {
            var seeder = serviceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedInitialAdminAsync();
        }

        // Seed Deductions
        if (!await context.Deductions.AnyAsync())
        {
            var seeder = serviceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedDeductionsAsync();
        }

        // Seed Student ID Sequence table (if using stored procedure approach)
        // Note: This table is created via SQL scripts, not EF Core
        // The sequence initialization is handled by DatabaseInitializer
    }
}

