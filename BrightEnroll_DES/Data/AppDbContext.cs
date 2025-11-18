using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data.Models;
using Microsoft.Extensions.Configuration;

namespace BrightEnroll_DES.Data;

/// <summary>
/// EF Core DbContext for the BrightEnroll_DES application
/// Configured for SQL Server with proper relationships
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Guardian> Guardians { get; set; }
    public DbSet<StudentRequirement> StudentRequirements { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId);
            entity.ToTable("students_tbl");

            // Configure relationship with Guardian
            entity.HasOne(s => s.Guardian)
                  .WithMany(g => g.Students)
                  .HasForeignKey(s => s.GuardianId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure relationship with Requirements
            entity.HasMany(s => s.Requirements)
                  .WithOne(r => r.Student)
                  .HasForeignKey(r => r.StudentId)
                  .OnDelete(DeleteBehavior.Cascade); // Delete requirements when student is deleted
        });

        // Configure Guardian entity
        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.HasKey(e => e.GuardianId);
            entity.ToTable("guardians_tbl");
        });

        // Configure StudentRequirement entity
        modelBuilder.Entity<StudentRequirement>(entity =>
        {
            entity.HasKey(e => e.RequirementId);
            entity.ToTable("student_requirements_tbl");

            // Index for better query performance
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.RequirementType);
        });
    }
}

