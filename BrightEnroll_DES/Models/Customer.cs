namespace BrightEnroll_DES.Models
{
    /// <summary>
    /// System Admin school customer (CRM) mapped to dbo.tbl_Customers.
    /// </summary>
    public class Customer
    {
        public int customer_id { get; set; }
        public string customer_code { get; set; } = string.Empty;
        public string school_name { get; set; } = string.Empty;
        public string? school_type { get; set; }
        public string address { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string? province { get; set; }
        public string contact_person { get; set; } = string.Empty;
        public string? contact_position { get; set; }
        public string contact_email { get; set; } = string.Empty;
        public string contact_phone { get; set; } = string.Empty;
        public string plan { get; set; } = string.Empty;
        public decimal monthly_fee { get; set; }
        public DateTime contract_start_date { get; set; }
        public int contract_duration_months { get; set; }
        public DateTime contract_end_date { get; set; }
        public int student_count { get; set; }
        public string status { get; set; } = "Active";
        public string? notes { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}


