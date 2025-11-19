using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Keyless entity for vw_StudentData view - read-only
[Table("vw_StudentData")]
public class StudentDataView
{
    [Column("StudentId")]
    public string StudentId { get; set; } = string.Empty;

    [Column("FirstName")]
    public string? FirstName { get; set; }

    [Column("MiddleName")]
    public string? MiddleName { get; set; }

    [Column("LastName")]
    public string? LastName { get; set; }

    [Column("Suffix")]
    public string? Suffix { get; set; }

    [Column("FullName")]
    public string? FullName { get; set; }

    [Column("BirthDate")]
    public DateTime? BirthDate { get; set; }

    [Column("Age")]
    public int? Age { get; set; }

    [Column("LRN")]
    public string? LRN { get; set; }

    [Column("GradeLevel")]
    public string? GradeLevel { get; set; }

    [Column("SchoolYear")]
    public string? SchoolYear { get; set; }

    [Column("DateRegistered")]
    public DateTime? DateRegistered { get; set; }

    [Column("Status")]
    public string? Status { get; set; }

    [Column("StudentType")]
    public string? StudentType { get; set; }
}

