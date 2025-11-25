namespace BrightEnroll_DES.Models
{
    public class Employee
    {
        public int employee_ID { get; set; }
        public string system_ID { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string? mid_name { get; set; }
        public string last_name { get; set; } = string.Empty;
        public string? suffix { get; set; }
        public DateTime birthdate { get; set; }
        public byte age { get; set; }
        public string gender { get; set; } = string.Empty;
        public string contact_num { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty; // For tbl_Users compatibility
        public string status { get; set; } = "Active";
        public string? inactive_reason { get; set; }
        public DateTime date_hired { get; set; }
        
        // Address fields
        public string? house_no { get; set; }
        public string? street_name { get; set; }
        public string? barangay { get; set; }
        public string? city { get; set; }
        public string? province { get; set; }
        public string? country { get; set; } = "Philippines";
        public string? zip_code { get; set; }
        
        // Emergency Contact fields
        public string? emergency_contact_first_name { get; set; }
        public string? emergency_contact_mid_name { get; set; }
        public string? emergency_contact_last_name { get; set; }
        public string? emergency_contact_suffix { get; set; }
        public string? emergency_contact_relationship { get; set; }
        public string? emergency_contact_number { get; set; }
        public string? emergency_contact_address { get; set; }
        
        // Salary fields
        public decimal? base_salary { get; set; }
        public decimal? allowance { get; set; }
        public decimal? bonus { get; set; }
        public decimal? deductions { get; set; }
        public decimal? total_salary { get; set; }
        
        // Computed properties for display
        public string FullName => $"{first_name} {mid_name} {last_name} {suffix}".Trim();
        public string Address
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(house_no)) parts.Add(house_no);
                if (!string.IsNullOrWhiteSpace(street_name)) parts.Add(street_name);
                if (!string.IsNullOrWhiteSpace(barangay)) parts.Add(barangay);
                if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
                if (!string.IsNullOrWhiteSpace(province)) parts.Add(province);
                if (!string.IsNullOrWhiteSpace(zip_code)) parts.Add(zip_code);
                
                var address = string.Join(", ", parts);
                return string.IsNullOrWhiteSpace(address) ? "No address provided" : address;
            }
        }
    }
}

