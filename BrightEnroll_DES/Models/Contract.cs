namespace BrightEnroll_DES.Models
{
    /// <summary>
    /// System Admin contract model mapping to dbo.tbl_Contracts.
    /// </summary>
    public class Contract
    {
        public int contract_id { get; set; }
        public string school_name { get; set; } = string.Empty;
        public string? customer_code { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public int max_users { get; set; }
        public bool modules_admission { get; set; }
        public bool modules_finance { get; set; }
        public bool modules_hr { get; set; }
        public bool modules_grades { get; set; }
        public bool modules_enrollment { get; set; }
        public string status { get; set; } = "Active";
        public string? contract_file_path { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}


