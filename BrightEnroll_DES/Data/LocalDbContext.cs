using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data.Models;

namespace BrightEnroll_DES.Data;

// Local database context for offline operations (LocalDB)
public class LocalDbContext : DbContext
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
    public DbSet<FinalClassView> FinalClassViews { get; set; }

    // Finance tables
    public DbSet<GradeLevel> GradeLevels { get; set; }
    public DbSet<Fee> Fees { get; set; }
    public DbSet<FeeBreakdown> FeeBreakdowns { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ExpenseAttachment> ExpenseAttachments { get; set; }

    // User status logging
    public DbSet<UserStatusLog> UserStatusLogs { get; set; }

    // Curriculum tables
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<SubjectSection> SubjectSections { get; set; }
    public DbSet<SubjectSchedule> SubjectSchedules { get; set; }
    public DbSet<TeacherSectionAssignment> TeacherSectionAssignments { get; set; }
    public DbSet<ClassSchedule> ClassSchedules { get; set; }
    public DbSet<Building> Buildings { get; set; }

    // Payroll tables (standalone)
    public DbSet<Role> Roles { get; set; }
    public DbSet<Deduction> Deductions { get; set; }

    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
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
            entity.Property(e => e.UserId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.SystemId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsSynced);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId);
            entity.ToTable("tbl_Students");
            entity.HasIndex(e => e.IsSynced);
            
            // Temporarily ignore ArchiveReason property until database column is added
            // Run Database_Scripts/Add_Archive_Reason_Column.sql to add the column, then remove this line
            entity.Ignore(e => e.ArchiveReason);

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
            entity.Property(e => e.GuardianId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.IsSynced);
        });

        modelBuilder.Entity<StudentRequirement>(entity =>
        {
            entity.HasKey(e => e.RequirementId);
            entity.ToTable("tbl_StudentRequirements");
            entity.Property(e => e.RequirementId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.RequirementType);
            entity.HasIndex(e => e.IsSynced);
            
            // Temporarily ignore IsVerified property until database column is added
            // Run Database_Scripts/Add_is_verified_Column.sql to add the column, then remove this line
            entity.Ignore(e => e.IsVerified);
        });

        modelBuilder.Entity<EmployeeAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId);
            entity.ToTable("tbl_employee_address");
            entity.Property(e => e.AddressId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsSynced);

            // Foreign key to UserEntity (no navigation property in model)
            entity.HasOne<UserEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeEmergencyContact>(entity =>
        {
            entity.HasKey(e => e.EmergencyId);
            entity.ToTable("tbl_employee_emergency_contact");
            entity.Property(e => e.EmergencyId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsSynced);

            // Foreign key to UserEntity (no navigation property in model)
            entity.HasOne<UserEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalaryInfo>(entity =>
        {
            entity.HasKey(e => e.SalaryId);
            entity.ToTable("tbl_salary_info");
            entity.Property(e => e.SalaryId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSynced);

            // Foreign key to UserEntity (no navigation property in model)
            entity.HasOne<UserEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<FinalClassView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("tbl_FinalClasses");
        });

        // Configure Finance entities
        modelBuilder.Entity<GradeLevel>(entity =>
        {
            entity.HasKey(e => e.GradeLevelId);
            entity.ToTable("tbl_GradeLevel");
            entity.Property(e => e.GradeLevelId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.GradeLevelName).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSynced);
        });

        modelBuilder.Entity<Fee>(entity =>
        {
            entity.HasKey(e => e.FeeId);
            entity.ToTable("tbl_Fees");
            entity.Property(e => e.FeeId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.GradeLevelId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSynced);

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
            entity.Property(e => e.BreakdownId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.FeeId);
            entity.HasIndex(e => e.BreakdownType);
            entity.HasIndex(e => e.IsSynced);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId);
            entity.ToTable("tbl_Expenses");
            entity.Property(e => e.ExpenseId).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.ExpenseCode).IsUnique();
            entity.HasIndex(e => e.ExpenseDate);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsSynced);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ExpenseAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);
            entity.ToTable("tbl_ExpenseAttachments");
            entity.Property(e => e.AttachmentId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.ExpenseId);
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(a => a.Expense)
                  .WithMany(e => e.Attachments)
                  .HasForeignKey(a => a.ExpenseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserStatusLog entity
        modelBuilder.Entity<UserStatusLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.ToTable("tbl_user_status_logs");
            entity.Property(e => e.LogId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ChangedBy);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ChangedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Curriculum entities
        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasKey(e => e.RoomId);
            entity.ToTable("tbl_Classrooms");
            entity.Property(e => e.RoomId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.RoomName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsSynced);
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.SectionId);
            entity.ToTable("tbl_Sections");
            entity.Property(e => e.SectionId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.SectionName);
            entity.HasIndex(e => e.GradeLevelId);
            entity.HasIndex(e => e.ClassroomId);
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(s => s.GradeLevel)
                  .WithMany()
                  .HasForeignKey(s => s.GradeLevelId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Classroom)
                  .WithMany(c => c.Sections)
                  .HasForeignKey(s => s.ClassroomId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId);
            entity.ToTable("tbl_Subjects");
            entity.Property(e => e.SubjectId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.SubjectName);
            entity.HasIndex(e => e.GradeLevelId);
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(s => s.GradeLevel)
                  .WithMany()
                  .HasForeignKey(s => s.GradeLevelId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubjectSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tbl_SubjectSection");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(ss => ss.Section)
                  .WithMany(s => s.SubjectSections)
                  .HasForeignKey(ss => ss.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ss => ss.Subject)
                  .WithMany(s => s.SubjectSections)
                  .HasForeignKey(ss => ss.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubjectSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);
            entity.ToTable("tbl_SubjectSchedule");
            
            // Explicitly map all column names FIRST before configuring relationships
            entity.Property(e => e.ScheduleId)
                  .HasColumnName("ScheduleID")
                  .ValueGeneratedOnAdd();
            
            entity.Property(e => e.SubjectId)
                  .HasColumnName("SubjectID")
                  .IsRequired();
            
            entity.Property(e => e.GradeLevelId)
                  .HasColumnName("GradeLvlID")
                  .IsRequired();
            
            entity.Property(e => e.DayOfWeek)
                  .HasColumnName("DayOfWeek")
                  .HasMaxLength(10)
                  .IsRequired();
            
            entity.Property(e => e.StartTime)
                  .HasColumnName("StartTime")
                  .HasColumnType("time")
                  .IsRequired();
            
            entity.Property(e => e.EndTime)
                  .HasColumnName("EndTime")
                  .HasColumnType("time")
                  .IsRequired();
            
            entity.Property(e => e.IsDefault)
                  .HasColumnName("IsDefault")
                  .IsRequired()
                  .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("CreatedAt")
                  .HasColumnType("datetime")
                  .IsRequired()
                  .HasDefaultValueSql("GETDATE()");
            
            entity.Property(e => e.UpdatedAt)
                  .HasColumnName("UpdatedAt")
                  .HasColumnType("datetime");
            
            // Configure indexes
            entity.HasIndex(e => e.SubjectId).HasDatabaseName("IX_tbl_SubjectSchedule_SubjectID");
            entity.HasIndex(e => e.GradeLevelId).HasDatabaseName("IX_tbl_SubjectSchedule_GradeLvlID");
            entity.HasIndex(e => new { e.SubjectId, e.GradeLevelId, e.DayOfWeek })
                  .HasDatabaseName("IX_tbl_SubjectSchedule_Subject_Grade_Day");
            entity.HasIndex(e => e.IsDefault).HasDatabaseName("IX_tbl_SubjectSchedule_IsDefault");
            entity.HasIndex(e => e.IsSynced);

            // Configure relationships with explicit foreign key column names
            entity.HasOne(ss => ss.Subject)
                  .WithMany(s => s.SubjectSchedules)
                  .HasForeignKey(ss => ss.SubjectId)
                  .HasConstraintName("FK_tbl_SubjectSchedule_tbl_Subjects")
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ss => ss.GradeLevel)
                  .WithMany()
                  .HasForeignKey(ss => ss.GradeLevelId)
                  .HasConstraintName("FK_tbl_SubjectSchedule_tbl_GradeLevel")
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeacherSectionAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId);
            entity.ToTable("tbl_TeacherSectionAssignment");
            entity.Property(e => e.AssignmentId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(a => a.Teacher)
                  .WithMany()
                  .HasForeignKey(a => a.TeacherId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Section)
                  .WithMany(s => s.TeacherAssignments)
                  .HasForeignKey(a => a.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Subject)
                  .WithMany(s => s.TeacherAssignments)
                  .HasForeignKey(a => a.SubjectId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);
            entity.ToTable("tbl_ClassSchedule");
            entity.Property(e => e.ScheduleId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.AssignmentId);
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.DayOfWeek);
            entity.HasIndex(e => new { e.AssignmentId, e.DayOfWeek, e.StartTime, e.EndTime });
            entity.HasIndex(e => e.IsSynced);

            entity.HasOne(s => s.Assignment)
                  .WithMany(a => a.ClassSchedules)
                  .HasForeignKey(s => s.AssignmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Room)
                  .WithMany(r => r.ClassSchedules)
                  .HasForeignKey(s => s.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.BuildingId);
            entity.ToTable("tbl_Buildings");
            entity.Property(e => e.BuildingId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.BuildingName);
            entity.HasIndex(e => e.IsSynced);
        });

        // Configure Payroll entities
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.ToTable("tbl_roles");
            entity.Property(e => e.RoleId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSynced);
        });

        modelBuilder.Entity<Deduction>(entity =>
        {
            entity.HasKey(e => e.DeductionId);
            entity.ToTable("tbl_deductions");
            entity.Property(e => e.DeductionId).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.DeductionType).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSynced);
        });
    }
}

