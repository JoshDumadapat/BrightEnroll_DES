namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Read-only projection for vw_EmployeeData view.
/// Exact schema is defined in SQL; we include only commonly used fields.
/// </summary>
public class EmployeeDataView
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}


