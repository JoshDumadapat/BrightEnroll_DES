using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services
{
    /// <summary>
    /// Business logic layer for System Admin sales leads.
    /// </summary>
    public interface ISalesLeadService
    {
        Task<int> CreateLeadAsync(SalesLead lead);
        Task<IEnumerable<SalesLead>> GetAllLeadsAsync();
    }

    public class SalesLeadService : ISalesLeadService
    {
        private readonly ISalesLeadRepository _repository;

        public SalesLeadService(ISalesLeadRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<int> CreateLeadAsync(SalesLead lead)
        {
            // Basic server-side validation
            if (string.IsNullOrWhiteSpace(lead.school_name))
                throw new ArgumentException("School name is required.", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.location))
                throw new ArgumentException("Location is required.", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.contact_name))
                throw new ArgumentException("Contact person is required.", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.email))
                throw new ArgumentException("Email is required.", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.phone))
                throw new ArgumentException("Phone number is required.", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.lead_source))
                throw new ArgumentException("Lead source is required.", nameof(lead));

            if (string.IsNullOrWhiteSpace(lead.interest_level))
                throw new ArgumentException("Interest level is required.", nameof(lead));

            // Normalize some fields
            lead.school_name = lead.school_name.Trim();
            lead.location = lead.location.Trim();
            lead.contact_name = lead.contact_name.Trim();
            lead.email = lead.email.Trim().ToLowerInvariant();
            lead.phone = lead.phone.Trim();
            lead.status = string.IsNullOrWhiteSpace(lead.status) ? "Active" : lead.status.Trim();

            return await _repository.InsertAsync(lead);
        }

        public Task<IEnumerable<SalesLead>> GetAllLeadsAsync()
        {
            return _repository.GetAllAsync();
        }
    }
}


