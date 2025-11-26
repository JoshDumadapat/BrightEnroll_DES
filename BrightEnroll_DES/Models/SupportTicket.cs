namespace BrightEnroll_DES.Models
{
    /// <summary>
    /// Support ticket for System Admin help desk (dbo.tbl_SupportTickets).
    /// </summary>
    public class SupportTicket
    {
        public int ticket_id { get; set; }
        public string school_name { get; set; } = string.Empty;
        public string subject { get; set; } = string.Empty;
        public string? description { get; set; }
        public string priority { get; set; } = "Medium";
        public string status { get; set; } = "Open";
        public string? assigned_agent { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}


