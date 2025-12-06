using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data.Models;

namespace BrightEnroll_DES.Data;

// EF Core database context - handles student and employee tables
public class AppDbContext : DbContext
{
    // User table
    public DbSet<UserEntity> Users { get; set; }
    
    public DbSet<Student> Students { get; set; }
    public DbSet<Guardian> Guardians { get; set; }
    public DbSet<StudentRequirement> StudentRequirements { get; set; }
    public DbSet<StudentSectionEnrollment> StudentSectionEnrollments { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<GradeWeight> GradeWeights { get; set; }
    public DbSet<GradeHistory> GradeHistories { get; set; }
    
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
    public DbSet<StudentPayment> StudentPayments { get; set; }

    // User status logging
    public DbSet<UserStatusLog> UserStatusLogs { get; set; }
    
    // Student status logging
    public DbSet<StudentStatusLog> StudentStatusLogs { get; set; }
    
    // Audit logging
    public DbSet<AuditLog> AuditLogs { get; set; }

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

    // Inventory & Asset Management tables
    public DbSet<Asset> Assets { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<AssetAssignment> AssetAssignments { get; set; }

    // Teacher-specific tables
    public DbSet<TeacherActivityLog> TeacherActivityLogs { get; set; }
    public DbSet<Attendance> Attendances { get; set; }

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

            entity.HasMany(s => s.SectionEnrollments)
                  .WithOne(e => e.Student)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Configure payment-related properties
            entity.Property(e => e.AmountPaid)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0m);
            
            entity.Property(e => e.PaymentStatus)
                  .HasMaxLength(20)
                  .HasDefaultValue("Unpaid");
            
            // Configure archive reason
            entity.Property(e => e.ArchiveReason)
                  .HasColumnType("nvarchar(max)");
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
            
            entity.Property(e => e.IsVerified)
                  .HasDefaultValue(false);
        });

        modelBuilder.Entity<StudentSectionEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId);
            entity.ToTable("tbl_StudentSectionEnrollment");

            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => new { e.SectionId, e.SchoolYear });
            entity.HasIndex(e => new { e.StudentId, e.SchoolYear }).IsUnique();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.SectionEnrollments)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Section)
                  .WithMany()
                  .HasForeignKey(e => e.SectionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId);
            entity.ToTable("tbl_Grades");

            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => new { e.StudentId, e.SubjectId, e.SectionId, e.GradingPeriod, e.SchoolYear });
            entity.HasIndex(e => new { e.StudentId, e.SubjectId, e.SectionId, e.GradingPeriod, e.SchoolYear }).IsUnique();

            entity.Property(e => e.Quiz)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.Exam)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.Project)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.Participation)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.FinalGrade)
                  .HasColumnType("decimal(5,2)");

            entity.HasOne(g => g.Student)
                  .WithMany()
                  .HasForeignKey(g => g.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.Subject)
                  .WithMany()
                  .HasForeignKey(g => g.SubjectId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.Section)
                  .WithMany()
                  .HasForeignKey(g => g.SectionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.Teacher)
                  .WithMany()
                  .HasForeignKey(g => g.TeacherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure GradeWeight entity
        modelBuilder.Entity<GradeWeight>(entity =>
        {
            entity.HasKey(e => e.WeightId);
            entity.ToTable("tbl_GradeWeights");
            entity.HasIndex(e => e.SubjectId).IsUnique();

            entity.Property(e => e.QuizWeight)
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(0.30m);

            entity.Property(e => e.ExamWeight)
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(0.40m);

            entity.Property(e => e.ProjectWeight)
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(0.20m);

            entity.Property(e => e.ParticipationWeight)
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(0.10m);

            entity.HasOne(w => w.Subject)
                  .WithMany()
                  .HasForeignKey(w => w.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure GradeHistory entity
        modelBuilder.Entity<GradeHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);
            entity.ToTable("tbl_GradeHistory");
            entity.HasIndex(e => e.GradeId);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => e.ChangedBy);

            entity.Property(e => e.QuizOld)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.QuizNew)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ExamOld)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ExamNew)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ProjectOld)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ProjectNew)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ParticipationOld)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ParticipationNew)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.FinalGradeOld)
                  .HasColumnType("decimal(5,2)");

            entity.Property(e => e.FinalGradeNew)
                  .HasColumnType("decimal(5,2)");

            entity.HasOne(h => h.Grade)
                  .WithMany()
                  .HasForeignKey(h => h.GradeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(h => h.ChangedBy)
                  .OnDelete(DeleteBehavior.Restrict);
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
            
            // Configure decimal precision for view columns
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(12,2)");
            entity.Property(e => e.Allowance).HasColumnType("decimal(12,2)");
            entity.Property(e => e.TotalSalary).HasColumnType("decimal(12,2)");
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

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId);
            entity.ToTable("tbl_Expenses");

            entity.HasIndex(e => e.ExpenseCode).IsUnique();
            entity.HasIndex(e => e.ExpenseDate);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ExpenseAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);
            entity.ToTable("tbl_ExpenseAttachments");
            entity.HasIndex(e => e.ExpenseId);

            entity.HasOne(a => a.Expense)
                  .WithMany(e => e.Attachments)
                  .HasForeignKey(a => a.ExpenseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.ToTable("tbl_StudentPayments");
            
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.OrNumber).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(p => p.Student)
                  .WithMany()
                  .HasForeignKey(p => p.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // Configure StudentStatusLog entity
        modelBuilder.Entity<StudentStatusLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.ToTable("tbl_student_status_logs");
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ChangedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Curriculum entities
        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasKey(e => e.RoomId);
            entity.ToTable("tbl_Classrooms");
            entity.HasIndex(e => e.RoomName);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.SectionId);
            entity.ToTable("tbl_Sections");
            entity.HasIndex(e => e.SectionName);
            entity.HasIndex(e => e.GradeLevelId);
            entity.HasIndex(e => e.ClassroomId);
            entity.HasIndex(e => e.AdviserId);

            entity.Property(e => e.SectionId)
                  .HasColumnName("SectionID")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.SectionName)
                  .HasColumnName("SectionName")
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.GradeLevelId)
                  .HasColumnName("GradeLvlID");

            entity.Property(e => e.ClassroomId)
                  .HasColumnName("ClassroomID");

            entity.Property(e => e.AdviserId)
                  .HasColumnName("AdviserID");

            entity.HasOne(s => s.GradeLevel)
                  .WithMany()
                  .HasForeignKey(s => s.GradeLevelId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Classroom)
                  .WithMany(c => c.Sections)
                  .HasForeignKey(s => s.ClassroomId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.Adviser)
                  .WithMany()
                  .HasForeignKey(s => s.AdviserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId);
            entity.ToTable("tbl_Subjects");
            entity.HasIndex(e => e.SubjectName);
            entity.HasIndex(e => e.GradeLevelId);
            entity.HasIndex(e => e.SubjectCode);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.SubjectId)
                  .HasColumnName("SubjectID")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.SubjectCode)
                  .HasMaxLength(50)
                  .HasColumnName("SubjectCode");

            entity.Property(e => e.GradeLevelId)
                  .HasColumnName("GradeLvlID");

            entity.Property(e => e.SubjectName)
                  .HasMaxLength(100)
                  .HasColumnName("SubjectName")
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(500)
                  .HasColumnName("Description");

            entity.Property(e => e.IsActive)
                  .HasColumnName("IsActive")
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("CreatedAt")
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnName("UpdatedAt")
                  .HasColumnType("datetime");

            entity.HasOne(s => s.GradeLevel)
                  .WithMany()
                  .HasForeignKey(s => s.GradeLevelId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubjectSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tbl_SubjectSection");
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.SubjectId);

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
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsArchived);

            entity.Property(e => e.AssignmentId)
                  .HasColumnName("AssignmentID")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.TeacherId)
                  .HasColumnName("TeacherID");

            entity.Property(e => e.SectionId)
                  .HasColumnName("SectionID");

            entity.Property(e => e.SubjectId)
                  .HasColumnName("SubjectID");

            entity.Property(e => e.Role)
                  .HasColumnName("Role")
                  .HasMaxLength(50);

            entity.Property(e => e.IsArchived)
                  .HasColumnName("IsArchived")
                  .HasDefaultValue(false);

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
            entity.HasIndex(e => e.AssignmentId);
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.DayOfWeek);
            entity.HasIndex(e => new { e.AssignmentId, e.DayOfWeek, e.StartTime, e.EndTime });

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
            entity.HasIndex(e => e.BuildingName);
        });

        // Configure Payroll entities
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.ToTable("tbl_roles");
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Deduction>(entity =>
        {
            entity.HasKey(e => e.DeductionId);
            entity.ToTable("tbl_deductions");
            entity.HasIndex(e => e.DeductionType).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Configure Inventory & Asset entities
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId);
            entity.ToTable("tbl_Assets");
            entity.HasIndex(e => e.AssetId).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.ToTable("tbl_InventoryItems");
            entity.HasIndex(e => e.ItemCode).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<AssetAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId);
            entity.ToTable("tbl_AssetAssignments");
            entity.HasIndex(e => e.AssetId);
            entity.HasIndex(e => e.AssignedToId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AssignedDate);

            entity.HasOne(a => a.Asset)
                  .WithMany()
                  .HasForeignKey(a => a.AssetId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Audit Log entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.ToTable("tbl_audit_logs");
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Module);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.RegistrarId);
            entity.HasIndex(e => new { e.Module, e.Timestamp });

            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Registrar)
                  .WithMany()
                  .HasForeignKey(a => a.RegistrarId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure TeacherActivityLog entity
        modelBuilder.Entity<TeacherActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tbl_TeacherActivityLogs");
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.TeacherId, e.CreatedAt });

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.TeacherId)
                  .HasColumnName("teacher_id");

            entity.Property(e => e.Action)
                  .HasColumnName("action")
                  .HasMaxLength(100);

            entity.Property(e => e.Details)
                  .HasColumnName("details")
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(a => a.Teacher)
                  .WithMany()
                  .HasForeignKey(a => a.TeacherId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Attendance entity
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId);
            entity.ToTable("tbl_Attendance");
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.AttendanceDate);
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => new { e.SectionId, e.AttendanceDate });
            entity.HasIndex(e => new { e.StudentId, e.AttendanceDate });

            entity.Property(e => e.AttendanceId)
                  .HasColumnName("AttendanceID")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.StudentId)
                  .HasColumnName("StudentID")
                  .HasMaxLength(6);

            entity.Property(e => e.SectionId)
                  .HasColumnName("SectionID");

            entity.Property(e => e.SubjectId)
                  .HasColumnName("SubjectID");

            entity.Property(e => e.AttendanceDate)
                  .HasColumnName("AttendanceDate")
                  .HasColumnType("date");

            entity.Property(e => e.Status)
                  .HasColumnName("Status")
                  .HasMaxLength(20);

            entity.Property(e => e.TimeIn)
                  .HasColumnName("TimeIn")
                  .HasColumnType("time");

            entity.Property(e => e.TimeOut)
                  .HasColumnName("TimeOut")
                  .HasColumnType("time");

            entity.Property(e => e.Remarks)
                  .HasColumnName("Remarks")
                  .HasMaxLength(500);

            entity.Property(e => e.TeacherId)
                  .HasColumnName("TeacherID");

            entity.Property(e => e.SchoolYear)
                  .HasColumnName("SchoolYear")
                  .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("CreatedAt")
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnName("UpdatedAt")
                  .HasColumnType("datetime");

            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Section)
                  .WithMany()
                  .HasForeignKey(a => a.SectionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Subject)
                  .WithMany()
                  .HasForeignKey(a => a.SubjectId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Teacher)
                  .WithMany()
                  .HasForeignKey(a => a.TeacherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

