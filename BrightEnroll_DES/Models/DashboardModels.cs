using System;
using System.Collections.Generic;

namespace BrightEnroll_DES.Models
{
    public class DashboardStats
    {
        public int TotalCustomers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int OpenTickets { get; set; }
        public List<DashboardRecentSale> RecentSales { get; set; } = new();
        public List<DashboardExpiringSubscription> ExpiringSubscriptions { get; set; } = new();
    }

    public class DashboardRecentSale
    {
        public string SchoolName { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class DashboardExpiringSubscription
    {
        public string SchoolName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }

    public class SubscriptionSummary
    {
        public int ActiveSubscriptions { get; set; }
        public int ExpiringThisMonth { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public List<DashboardExpiringSubscription> ExpiringSoon { get; set; } = new();
    }

    public class SupportTicket
    {
        public int TicketId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ContractSummary
    {
        public int ContractId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsers { get; set; }
        public string EnabledModules { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class VersionOverview
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public DateTime CurrentReleaseDate { get; set; }
        public List<VersionHistoryItem> History { get; set; } = new();
        public List<FeatureToggle> FeatureToggles { get; set; } = new();
    }

    public class VersionHistoryItem
    {
        public string VersionName { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class FeatureToggle
    {
        public int FeatureId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}


