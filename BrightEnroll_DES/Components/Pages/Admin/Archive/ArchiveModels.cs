namespace BrightEnroll_DES.Components.Pages.Admin.Archive;

public class ArchivedStudent
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string LRN { get; set; } = "";
    public string Date { get; set; } = "";
    public string Reason { get; set; } = ""; // Reason for archiving (replaces Notes)
    public string Status { get; set; } = ""; // Archive reason: "Rejected by School", "Application Withdrawn", "Withdrawn", "Graduated", "Transferred"
    public string ArchivedDate { get; set; } = "";
    public string ArchivedReason { get; set; } = "";
}

public class ArchivedEmployee
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public string ArchivedDate { get; set; } = "";
    public string ArchivedReason { get; set; } = "";
}

