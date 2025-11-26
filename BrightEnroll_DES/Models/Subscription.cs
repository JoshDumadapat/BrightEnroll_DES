namespace BrightEnroll_DES.Models
{
    /// <summary>
    /// Subscription billing record mapped to dbo.tbl_Subscriptions.
    /// </summary>
    public class Subscription
    {
        public int subscription_id { get; set; }
        public int customer_id { get; set; }
        public string plan { get; set; } = string.Empty;
        public decimal monthly_fee { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string status { get; set; } = "Active";
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}


