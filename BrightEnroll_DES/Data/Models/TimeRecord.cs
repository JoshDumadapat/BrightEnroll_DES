using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Tracks employee attendance/time records for payroll
[Table("tbl_TimeRecords")]
public class TimeRecord
{
    [Key]
    [Column("time_record_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TimeRecordId { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("period")]
    public string Period { get; set; } = string.Empty; // e.g., "2025-01" for January 2025

    [MaxLength(10)]
    [Column("time_in")]
    public string? TimeIn { get; set; } // HH:mm format

    [MaxLength(10)]
    [Column("time_out")]
    public string? TimeOut { get; set; } // HH:mm format

    [Required]
    [Column("regular_hours", TypeName = "decimal(5,2)")]
    public decimal RegularHours { get; set; } = 0.00m;

    [Required]
    [Column("overtime_hours", TypeName = "decimal(5,2)")]
    public decimal OvertimeHours { get; set; } = 0.00m;

    [Required]
    [Column("leave_days", TypeName = "decimal(4,1)")]
    public decimal LeaveDays { get; set; } = 0.0m;

    [Required]
    [Column("late_minutes")]
    public int LateMinutes { get; set; } = 0;

    [Required]
    [Column("total_days_absent")]
    public int TotalDaysAbsent { get; set; } = 0;

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual UserEntity? User { get; set; }
}

