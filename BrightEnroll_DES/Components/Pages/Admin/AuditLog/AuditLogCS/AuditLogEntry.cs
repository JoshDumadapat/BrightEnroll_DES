namespace BrightEnroll_DES.Components.Pages.Admin.AuditLog.AuditLogCS;

public class AuditLogEntry
{
    public string Id { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = "";
    public string Role { get; set; } = "";
    public string Action { get; set; } = "";
    public string Module { get; set; } = "";
    public string Description { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string Status { get; set; } = ""; // Success, Failed, Warning
    public string Severity { get; set; } = ""; // Low, Medium, High, Critical
}

