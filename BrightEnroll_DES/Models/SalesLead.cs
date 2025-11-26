namespace BrightEnroll_DES.Models
{
    /// <summary>
    /// Model representing a sales lead for the System Admin Sales module.
    /// Maps to dbo.tbl_SalesLeads.
    /// </summary>
    public class SalesLead
    {
        public int lead_id { get; set; }
        public string school_name { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string? school_type { get; set; }
        public int estimated_students { get; set; }
        public string? website { get; set; }
        public string contact_name { get; set; } = string.Empty;
        public string? contact_position { get; set; }
        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string? alternative_phone { get; set; }
        public string lead_source { get; set; } = string.Empty;
        public string interest_level { get; set; } = string.Empty;
        public string? interested_plan { get; set; }
        public DateTime? expected_close_date { get; set; }
        public string? assigned_agent { get; set; }
        public string? budget_range { get; set; }
        public string? notes { get; set; }
        public string status { get; set; } = "Active";
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}


