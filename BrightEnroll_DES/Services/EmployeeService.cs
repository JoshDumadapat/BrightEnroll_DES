using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services
{
    /// <summary>
    /// Service for Employee business logic operations
    /// </summary>
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<IEnumerable<Employee>> GetEmployeesByStatusAsync(string status);
        Task<IEnumerable<Employee>> GetEmployeesByRoleAsync(string role);
        Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm);
        Task<Employee?> GetEmployeeByIdAsync(int employeeId);
        Task<Employee?> GetEmployeeByEmailAsync(string email);
        Task<Employee?> GetEmployeeBySystemIdAsync(string systemId);
        Task<bool> CreateEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int employeeId);
        Task<bool> UpdateEmployeeStatusAsync(int employeeId, string status, string? inactiveReason = null);
        Task<IEnumerable<string>> GetDistinctRolesAsync();
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _employeeRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return await GetAllEmployeesAsync();
            }

            return await _employeeRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByRoleAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return await GetAllEmployeesAsync();
            }

            return await _employeeRepository.GetByRoleAsync(role);
        }

        public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm)
        {
            return await _employeeRepository.SearchAsync(searchTerm);
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
        {
            return await _employeeRepository.GetByIdAsync(employeeId);
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _employeeRepository.GetByEmailAsync(email);
        }

        public async Task<Employee?> GetEmployeeBySystemIdAsync(string systemId)
        {
            return await _employeeRepository.GetBySystemIdAsync(systemId);
        }

        public async Task<bool> CreateEmployeeAsync(Employee employee)
        {
            // Check if email already exists
            if (await _employeeRepository.ExistsByEmailAsync(employee.email))
            {
                throw new InvalidOperationException($"An employee with email {employee.email} already exists.");
            }

            // Check if system ID already exists
            if (await _employeeRepository.ExistsBySystemIdAsync(employee.system_ID))
            {
                throw new InvalidOperationException($"An employee with system ID {employee.system_ID} already exists.");
            }

            // Set default status if not provided
            if (string.IsNullOrWhiteSpace(employee.status))
            {
                employee.status = "Active";
            }

            // Set default date_hired if not provided
            if (employee.date_hired == default)
            {
                employee.date_hired = DateTime.Now;
            }

            var result = await _employeeRepository.InsertAsync(employee);
            
            if (result <= 0)
            {
                throw new InvalidOperationException("Failed to insert employee. No rows were affected.");
            }
            
            return true;
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                // Check if employee exists
                if (!await _employeeRepository.ExistsByIdAsync(employee.employee_ID))
                {
                    throw new InvalidOperationException($"Employee with ID {employee.employee_ID} does not exist.");
                }

                var result = await _employeeRepository.UpdateAsync(employee);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int employeeId)
        {
            try
            {
                var result = await _employeeRepository.DeleteAsync(employeeId);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateEmployeeStatusAsync(int employeeId, string status, string? inactiveReason = null)
        {
            try
            {
                var result = await _employeeRepository.UpdateStatusAsync(employeeId, status, inactiveReason);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetDistinctRolesAsync()
        {
            var employees = await _employeeRepository.GetAllAsync();
            return employees
                .Select(e => e.role)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .OrderBy(r => r)
                .ToList();
        }
    }
}

