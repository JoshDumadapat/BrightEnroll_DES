namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Read-only projection for vw_StudentData view.
/// </summary>
public class StudentDataView
{
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}


