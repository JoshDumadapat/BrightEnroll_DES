using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services
{
    public interface IContractService
    {
        Task<IEnumerable<Contract>> GetAllAsync();
        Task<int> CreateAsync(Contract contract);
    }

    /// <summary>
    /// Business logic for contracts & licensing in the System Admin portal.
    /// </summary>
    public class ContractService : IContractService
    {
        private readonly IContractRepository _repository;

        public ContractService(IContractRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task<IEnumerable<Contract>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public async Task<int> CreateAsync(Contract contract)
        {
            if (string.IsNullOrWhiteSpace(contract.school_name))
                throw new ArgumentException("School name is required.", nameof(contract));

            if (contract.start_date == default)
                throw new ArgumentException("Start date is required.", nameof(contract));

            if (contract.end_date == default || contract.end_date <= contract.start_date)
                throw new ArgumentException("End date must be after start date.", nameof(contract));

            if (contract.max_users < 0)
                contract.max_users = 0;

            contract.school_name = contract.school_name.Trim();
            contract.status = string.IsNullOrWhiteSpace(contract.status) ? "Active" : contract.status.Trim();

            return await _repository.InsertAsync(contract);
        }
    }
}


