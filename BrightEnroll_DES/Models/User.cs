namespace BrightEnroll_DES.Models
{
    public class User
    {
        public int user_ID { get; set; }
        public string system_ID { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string? mid_name { get; set; }
        public string last_name { get; set; } = string.Empty;
        public string? suffix { get; set; }
        public DateTime birthdate { get; set; }
        public byte age { get; set; }
    public string gender { get; set; } = string.Empty;
    public string contact_num { get; set; } = string.Empty;
    public string user_role { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public DateTime date_hired { get; set; }
        public string status { get; set; } = "active";
    }
}

