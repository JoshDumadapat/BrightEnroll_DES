using System;

namespace BrightEnroll_DES.Models
{
    // DTO used by Super Admin portal for customer management
    public class SuperAdminCustomer
    {
        public int CustomerId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string SchoolType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactPosition { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public decimal MonthlyFee { get; set; }
        public DateTime ContractStartDate { get; set; }
        public DateTime ContractEndDate { get; set; }
        public int StudentCount { get; set; }
        public string Status { get; set; } = "Active";
        public string Notes { get; set; } = string.Empty;
    }
}


