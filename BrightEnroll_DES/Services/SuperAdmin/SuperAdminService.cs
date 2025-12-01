using BrightEnroll_DES.Models;

namespace BrightEnroll_DES.Services.SuperAdmin
{
    public interface ISuperAdminService
    {
        Task<List<SuperAdminCustomer>> GetCustomersAsync();
        Task AddCustomerAsync(SuperAdminCustomer customer);

        Task<List<SalesLead>> GetLeadsAsync();
        Task AddLeadAsync(SalesLead lead);

        Task<DashboardStats> GetDashboardStatsAsync();
        Task<SubscriptionSummary> GetSubscriptionSummaryAsync();
        Task<List<SupportTicket>> GetSupportTicketsAsync();
        Task<List<ContractSummary>> GetContractsAsync();
        Task<VersionOverview> GetVersionOverviewAsync();
    }

    // High-level service used by Blazor components for Super Admin features
    public class SuperAdminService : ISuperAdminService
    {
        private readonly ISuperAdminRepository _repository;

        public SuperAdminService(ISuperAdminRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<SuperAdminCustomer>> GetCustomersAsync()
        {
            return await _repository.GetCustomersAsync();
        }

        public async Task AddCustomerAsync(SuperAdminCustomer customer)
        {
            // Minimal validation – UI already enforces required fields
            if (string.IsNullOrWhiteSpace(customer.SchoolName))
                throw new ArgumentException("School name is required", nameof(customer));

            if (string.IsNullOrWhiteSpace(customer.ContactEmail))
                throw new ArgumentException("Contact email is required", nameof(customer));

            await _repository.AddCustomerAsync(customer);
        }

        public async Task<List<SalesLead>> GetLeadsAsync()
        {
            return await _repository.GetLeadsAsync();
        }

        public async Task AddLeadAsync(SalesLead lead)
        {
            if (string.IsNullOrWhiteSpace(lead.SchoolName))
                throw new ArgumentException("School name is required", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.Email))
                throw new ArgumentException("Email is required", nameof(lead));

            await _repository.AddLeadAsync(lead);
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var customers = await _repository.GetCustomersAsync();
            var leads = await _repository.GetLeadsAsync();
            var tickets = await _repository.GetSupportTicketsAsync();

            var stats = new DashboardStats
            {
                TotalCustomers = customers.Count,
                ActiveSubscriptions = customers.Count(c => c.Status == "Active"),
                MonthlyRevenue = customers
                    .Where(c => c.Status == "Active")
                    .Sum(c => c.MonthlyFee),
                OpenTickets = tickets.Count(t => t.Status == "Open")
            };

            // Recent sales = last 3 converted leads
            var recentConverted = leads
                .Where(l => l.Status == "Converted")
                .OrderByDescending(l => l.ExpectedCloseDate ?? l.CreatedAt)
                .Take(3)
                .ToList();

            stats.RecentSales = recentConverted.Select(l => new DashboardRecentSale
            {
                SchoolName = l.SchoolName,
                Plan = string.IsNullOrWhiteSpace(l.InterestedPlan) ? "N/A" : l.InterestedPlan,
                Amount = l.InterestedPlan switch
                {
                    "Premium" => 85000,
                    "Standard" => 55000,
                    "Basic" => 35000,
                    _ => 0
                },
                Date = l.ExpectedCloseDate ?? l.CreatedAt
            }).ToList();

            // Expiring subscriptions = customers whose contract ends in next 30 days
            var now = DateTime.Now.Date;
            var soon = now.AddDays(30);

            stats.ExpiringSubscriptions = customers
                .Where(c => c.ContractEndDate.Date >= now && c.ContractEndDate.Date <= soon)
                .OrderBy(c => c.ContractEndDate)
                .Take(5)
                .Select(c => new DashboardExpiringSubscription
                {
                    SchoolName = c.SchoolName,
                    ExpiryDate = c.ContractEndDate
                })
                .ToList();

            return stats;
        }

        public async Task<SubscriptionSummary> GetSubscriptionSummaryAsync()
        {
            var customers = await _repository.GetCustomersAsync();
            var now = DateTime.Now.Date;
            var endOfMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            var summary = new SubscriptionSummary
            {
                ActiveSubscriptions = customers.Count(c => c.Status == "Active"),
                ExpiringThisMonth = customers.Count(c =>
                    c.Status == "Active" &&
                    c.ContractEndDate.Date >= now &&
                    c.ContractEndDate.Date <= endOfMonth),
                MonthlyRecurringRevenue = customers
                    .Where(c => c.Status == "Active")
                    .Sum(c => c.MonthlyFee)
            };

            summary.ExpiringSoon = customers
                .Where(c => c.Status == "Active" && c.ContractEndDate.Date >= now)
                .OrderBy(c => c.ContractEndDate)
                .Take(5)
                .Select(c => new DashboardExpiringSubscription
                {
                    SchoolName = c.SchoolName,
                    ExpiryDate = c.ContractEndDate
                })
                .ToList();

            return summary;
        }

        public async Task<List<SupportTicket>> GetSupportTicketsAsync()
        {
            return await _repository.GetSupportTicketsAsync();
        }

        public async Task<List<ContractSummary>> GetContractsAsync()
        {
            return await _repository.GetContractsAsync();
        }

        public async Task<VersionOverview> GetVersionOverviewAsync()
        {
            var versions = await _repository.GetSystemVersionsAsync();
            var toggles = await _repository.GetFeatureTogglesAsync();

            var overview = new VersionOverview();

            var current = versions.OrderByDescending(v => v.ReleaseDate).FirstOrDefault();
            if (current != null)
            {
                overview.CurrentVersion = current.VersionName;
                overview.CurrentReleaseDate = current.ReleaseDate;
            }

            overview.History = versions
                .OrderByDescending(v => v.ReleaseDate)
                .Take(5)
                .Select(v => new VersionHistoryItem
                {
                    VersionName = v.VersionName,
                    ReleaseDate = v.ReleaseDate,
                    Notes = v.Notes ?? string.Empty
                })
                .ToList();

            overview.FeatureToggles = toggles
                .Select(t => new FeatureToggle
                {
                    FeatureId = t.FeatureId,
                    FeatureName = t.FeatureName,
                    Plan = t.Plan,
                    IsEnabled = t.IsEnabled
                })
                .ToList();

            return overview;
        }
    }
}


