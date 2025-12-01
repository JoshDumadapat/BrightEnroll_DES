using System;

namespace BrightEnroll_DES.Models
{
    // DTO used by Super Admin portal for sales leads
    public class SalesLead
    {
        public int LeadId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string SchoolType { get; set; } = string.Empty;
        public int EstimatedStudents { get; set; }
        public string Website { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactPosition { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AlternativePhone { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string InterestLevel { get; set; } = string.Empty;
        public string InterestedPlan { get; set; } = string.Empty;
        public DateTime? ExpectedCloseDate { get; set; }
        public string AssignedAgent { get; set; } = string.Empty;
        public string BudgetRange { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "New";
        public DateTime CreatedAt { get; set; }
    }
}


