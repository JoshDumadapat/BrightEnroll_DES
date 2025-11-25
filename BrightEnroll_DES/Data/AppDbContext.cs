using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data.Models;
using Microsoft.Extensions.Configuration;

namespace BrightEnroll_DES.Data;

// EF Core database context - handles student and employee tables
public class AppDbContext : DbContext
{
    // User table
    public DbSet<UserEntity> Users { get; set; }
    
    public DbSet<Student> Students { get; set; }
    public DbSet<Guardian> Guardians { get; set; }
    public DbSet<StudentRequirement> StudentRequirements { get; set; }
    
    // Employee tables
    public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
    public DbSet<EmployeeEmergencyContact> EmployeeEmergencyContacts { get; set; }
    public DbSet<SalaryInfo> SalaryInfos { get; set; }

    // View entities (keyless, read-only)
    public DbSet<EmployeeDataView> EmployeeDataViews { get; set; }
    public DbSet<StudentDataView> StudentDataViews { get; set; }

    // Finance tables
    public DbSet<GradeLevel> GradeLevels { get; set; }
    public DbSet<Fee> Fees { get; set; }
    public DbSet<FeeBreakdown> FeeBreakdowns { get; set; }

    // User status logging
    public DbSet<UserStatusLog> UserStatusLogs { get; set; }

    // Classes table
    public DbSet<Class> Classes { get; set; }
    
    // Schedules table
    public DbSet<Schedule> Schedules { get; set; }
    
    // Class enrollments table
    public DbSet<ClassEnrollment> ClassEnrollments { get; set; }
    
    // Grades table
    public DbSet<Grade> Grades { get; set; }
    
    // Reports table
    public DbSet<Report> Reports { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.ToTable("tbl_Users");
            entity.HasIndex(e => e.SystemId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId);
            entity.ToTable("tbl_Students");

            entity.HasOne(s => s.Guardian)
                  .WithMany(g => g.Students)
                  .HasForeignKey(s => s.GuardianId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.Requirements)
                  .WithOne(r => r.Student)
                  .HasForeignKey(r => r.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.HasKey(e => e.GuardianId);
            entity.ToTable("tbl_Guardians");
        });

        modelBuilder.Entity<StudentRequirement>(entity =>
        {
            entity.HasKey(e => e.RequirementId);
            entity.ToTable("tbl_StudentRequirements");
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.RequirementType);
        });

        modelBuilder.Entity<EmployeeAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId);
            entity.ToTable("tbl_employee_address");
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<EmployeeEmergencyContact>(entity =>
        {
            entity.HasKey(e => e.EmergencyId);
            entity.ToTable("tbl_employee_emergency_contact");
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<SalaryInfo>(entity =>
        {
            entity.HasKey(e => e.SalaryId);
            entity.ToTable("tbl_salary_info");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
        });

        // Configure view entities as keyless (read-only)
        modelBuilder.Entity<EmployeeDataView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_EmployeeData");
        });

        modelBuilder.Entity<StudentDataView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_StudentData");
        });

        // Configure Finance entities
        modelBuilder.Entity<GradeLevel>(entity =>
        {
            entity.HasKey(e => e.GradeLevelId);
            entity.ToTable("tbl_GradeLevel");
            entity.HasIndex(e => e.GradeLevelName).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Fee>(entity =>
        {
            entity.HasKey(e => e.FeeId);
            entity.ToTable("tbl_Fees");
            entity.HasIndex(e => e.GradeLevelId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(f => f.GradeLevel)
                  .WithMany()
                  .HasForeignKey(f => f.GradeLevelId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(f => f.Breakdowns)
                  .WithOne(b => b.Fee)
                  .HasForeignKey(b => b.FeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FeeBreakdown>(entity =>
        {
            entity.HasKey(e => e.BreakdownId);
            entity.ToTable("tbl_FeeBreakdown");
            entity.HasIndex(e => e.FeeId);
            entity.HasIndex(e => e.BreakdownType);
        });

        // Configure UserStatusLog entity
        modelBuilder.Entity<UserStatusLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.ToTable("tbl_user_status_logs");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ChangedBy);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ChangedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Class entity
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId);
            entity.ToTable("tbl_Classes");
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.SchoolYear);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Subject);

            entity.HasOne(c => c.Teacher)
                  .WithMany()
                  .HasForeignKey(c => c.TeacherId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Schedule entity
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);
            entity.ToTable("tbl_Schedules");
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => e.DayOfWeek);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.StartTime);

            entity.HasOne(s => s.Class)
                  .WithMany()
                  .HasForeignKey(s => s.ClassId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ClassEnrollment entity
        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId);
            entity.ToTable("tbl_ClassEnrollments");
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Class)
                  .WithMany()
                  .HasForeignKey(e => e.ClassId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Grade entity
        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId);
            entity.ToTable("tbl_Grades");
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.SchoolYear);

            entity.HasOne(g => g.Class)
                  .WithMany()
                  .HasForeignKey(g => g.ClassId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.Student)
                  .WithMany()
                  .HasForeignKey(g => g.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Report entity
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId);
            entity.ToTable("tbl_Reports");
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.ReportType);
            entity.HasIndex(e => e.GeneratedDate);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(r => r.Teacher)
                  .WithMany()
                  .HasForeignKey(r => r.TeacherId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

