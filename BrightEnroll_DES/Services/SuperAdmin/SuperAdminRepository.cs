using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.SuperAdmin
{
    public interface ISuperAdminRepository
    {
        Task<List<SuperAdminCustomer>> GetCustomersAsync();
        Task<int> AddCustomerAsync(SuperAdminCustomer customer);

        Task<List<SalesLead>> GetLeadsAsync();
        Task<int> AddLeadAsync(SalesLead lead);

        Task<List<SupportTicket>> GetSupportTicketsAsync();
        Task<List<ContractSummary>> GetContractsAsync();
        Task<List<SystemVersionEntity>> GetSystemVersionsAsync();
        Task<List<FeatureToggleEntity>> GetFeatureTogglesAsync();
    }

    // EF Core-based repository for Super Admin portal data
    public class SuperAdminRepository : ISuperAdminRepository
    {
        private readonly AppDbContext _context;

        public SuperAdminRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<SuperAdminCustomer>> GetCustomersAsync()
        {
            var entities = await _context.SchoolCustomers
                .OrderBy(c => c.SchoolName)
                .ToListAsync();

            return entities.Select(MapCustomerEntityToModel).ToList();
        }

        public async Task<int> AddCustomerAsync(SuperAdminCustomer customer)
        {
            if (customer == null) throw new ArgumentNullException(nameof(customer));

            var entity = new SchoolCustomerEntity
            {
                SchoolName = customer.SchoolName.Trim(),
                SchoolType = string.IsNullOrWhiteSpace(customer.SchoolType) ? null : customer.SchoolType.Trim(),
                Address = customer.Address.Trim(),
                City = customer.City.Trim(),
                Province = string.IsNullOrWhiteSpace(customer.Province) ? null : customer.Province.Trim(),
                ContactPerson = customer.ContactPerson.Trim(),
                ContactPosition = string.IsNullOrWhiteSpace(customer.ContactPosition) ? null : customer.ContactPosition.Trim(),
                ContactEmail = customer.ContactEmail.Trim(),
                ContactPhone = customer.ContactPhone.Trim(),
                Plan = customer.Plan.Trim(),
                MonthlyFee = customer.MonthlyFee,
                ContractStartDate = customer.ContractStartDate,
                ContractEndDate = customer.ContractEndDate,
                StudentCount = customer.StudentCount == 0 ? null : customer.StudentCount,
                Status = string.IsNullOrWhiteSpace(customer.Status) ? "Active" : customer.Status.Trim(),
                Notes = string.IsNullOrWhiteSpace(customer.Notes) ? null : customer.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.SchoolCustomers.Add(entity);
            await _context.SaveChangesAsync();

            return entity.CustomerId;
        }

        public async Task<List<SalesLead>> GetLeadsAsync()
        {
            var entities = await _context.SalesLeads
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return entities.Select(MapLeadEntityToModel).ToList();
        }

        public async Task<int> AddLeadAsync(SalesLead lead)
        {
            if (lead == null) throw new ArgumentNullException(nameof(lead));

            var entity = new SalesLeadEntity
            {
                SchoolName = lead.SchoolName.Trim(),
                Location = lead.Location.Trim(),
                SchoolType = string.IsNullOrWhiteSpace(lead.SchoolType) ? null : lead.SchoolType.Trim(),
                EstimatedStudents = lead.EstimatedStudents == 0 ? null : lead.EstimatedStudents,
                Website = string.IsNullOrWhiteSpace(lead.Website) ? null : lead.Website.Trim(),
                ContactName = lead.ContactName.Trim(),
                ContactPosition = string.IsNullOrWhiteSpace(lead.ContactPosition) ? null : lead.ContactPosition.Trim(),
                Email = lead.Email.Trim(),
                Phone = lead.Phone.Trim(),
                AlternativePhone = string.IsNullOrWhiteSpace(lead.AlternativePhone) ? null : lead.AlternativePhone.Trim(),
                LeadSource = lead.LeadSource.Trim(),
                InterestLevel = lead.InterestLevel.Trim(),
                InterestedPlan = string.IsNullOrWhiteSpace(lead.InterestedPlan) ? null : lead.InterestedPlan.Trim(),
                ExpectedCloseDate = lead.ExpectedCloseDate,
                AssignedAgent = string.IsNullOrWhiteSpace(lead.AssignedAgent) ? null : lead.AssignedAgent.Trim(),
                BudgetRange = string.IsNullOrWhiteSpace(lead.BudgetRange) ? null : lead.BudgetRange.Trim(),
                Notes = string.IsNullOrWhiteSpace(lead.Notes) ? null : lead.Notes.Trim(),
                Status = string.IsNullOrWhiteSpace(lead.Status) ? "New" : lead.Status.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.SalesLeads.Add(entity);
            await _context.SaveChangesAsync();

            return entity.LeadId;
        }

        public async Task<List<SupportTicket>> GetSupportTicketsAsync()
        {
            var tickets = await _context.SupportTickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();

            return tickets.Select(t => new SupportTicket
            {
                TicketId = t.TicketId,
                SchoolName = t.SchoolName,
                Issue = t.Issue,
                Priority = t.Priority,
                Status = t.Status,
                AssignedTo = t.AssignedTo ?? string.Empty,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<List<ContractSummary>> GetContractsAsync()
        {
            var contracts = await _context.Contracts
                .OrderBy(c => c.SchoolName)
                .ToListAsync();

            return contracts.Select(c => new ContractSummary
            {
                ContractId = c.ContractId,
                SchoolName = c.SchoolName,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                MaxUsers = c.MaxUsers,
                EnabledModules = c.EnabledModules,
                Status = c.Status
            }).ToList();
        }

        public async Task<List<SystemVersionEntity>> GetSystemVersionsAsync()
        {
            return await _context.SystemVersions
                .OrderByDescending(v => v.ReleaseDate)
                .ToListAsync();
        }

        public async Task<List<FeatureToggleEntity>> GetFeatureTogglesAsync()
        {
            return await _context.FeatureToggles
                .OrderBy(f => f.FeatureName)
                .ThenBy(f => f.Plan)
                .ToListAsync();
        }

        private static SuperAdminCustomer MapCustomerEntityToModel(SchoolCustomerEntity entity)
        {
            return new SuperAdminCustomer
            {
                CustomerId = entity.CustomerId,
                SchoolName = entity.SchoolName,
                SchoolType = entity.SchoolType ?? string.Empty,
                Address = entity.Address,
                City = entity.City,
                Province = entity.Province ?? string.Empty,
                ContactPerson = entity.ContactPerson,
                ContactPosition = entity.ContactPosition ?? string.Empty,
                ContactEmail = entity.ContactEmail,
                ContactPhone = entity.ContactPhone,
                Plan = entity.Plan,
                MonthlyFee = entity.MonthlyFee,
                ContractStartDate = entity.ContractStartDate,
                ContractEndDate = entity.ContractEndDate,
                StudentCount = entity.StudentCount ?? 0,
                Status = entity.Status,
                Notes = entity.Notes ?? string.Empty
            };
        }

        private static SalesLead MapLeadEntityToModel(SalesLeadEntity entity)
        {
            return new SalesLead
            {
                LeadId = entity.LeadId,
                SchoolName = entity.SchoolName,
                Location = entity.Location,
                SchoolType = entity.SchoolType ?? string.Empty,
                EstimatedStudents = entity.EstimatedStudents ?? 0,
                Website = entity.Website ?? string.Empty,
                ContactName = entity.ContactName,
                ContactPosition = entity.ContactPosition ?? string.Empty,
                Email = entity.Email,
                Phone = entity.Phone,
                AlternativePhone = entity.AlternativePhone ?? string.Empty,
                LeadSource = entity.LeadSource,
                InterestLevel = entity.InterestLevel,
                InterestedPlan = entity.InterestedPlan ?? string.Empty,
                ExpectedCloseDate = entity.ExpectedCloseDate,
                AssignedAgent = entity.AssignedAgent ?? string.Empty,
                BudgetRange = entity.BudgetRange ?? string.Empty,
                Notes = entity.Notes ?? string.Empty,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}


