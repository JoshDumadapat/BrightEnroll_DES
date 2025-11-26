using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<int> CreateAsync(Customer customer);
    }

    /// <summary>
    /// Business logic for System Admin customers (CRM).
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;

        public CustomerService(ICustomerRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task<IEnumerable<Customer>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public async Task<int> CreateAsync(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.school_name))
                throw new ArgumentException("School name is required.", nameof(customer));

            if (string.IsNullOrWhiteSpace(customer.contact_person))
                throw new ArgumentException("Contact person is required.", nameof(customer));

            if (string.IsNullOrWhiteSpace(customer.contact_email))
                throw new ArgumentException("Contact email is required.", nameof(customer));

            if (string.IsNullOrWhiteSpace(customer.contact_phone))
                throw new ArgumentException("Contact phone is required.", nameof(customer));

            if (string.IsNullOrWhiteSpace(customer.plan))
                throw new ArgumentException("Subscription plan is required.", nameof(customer));

            if (customer.contract_start_date == default)
                throw new ArgumentException("Contract start date is required.", nameof(customer));

            if (customer.contract_duration_months <= 0)
                throw new ArgumentException("Contract duration must be greater than 0.", nameof(customer));

            // Normalize & derive fields
            customer.school_name = customer.school_name.Trim();
            customer.address = customer.address.Trim();
            customer.city = customer.city.Trim();
            customer.contact_person = customer.contact_person.Trim();
            customer.contact_email = customer.contact_email.Trim().ToLowerInvariant();
            customer.contact_phone = customer.contact_phone.Trim();
            customer.status = string.IsNullOrWhiteSpace(customer.status) ? "Active" : customer.status.Trim();
            customer.contract_end_date = customer.contract_start_date.AddMonths(customer.contract_duration_months);

            if (string.IsNullOrWhiteSpace(customer.customer_code))
            {
                // Simple unique-ish code using timestamp
                customer.customer_code = $"CUST-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            return await _repository.InsertAsync(customer);
        }
    }
}


