using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.HR;

// Handles employee registration - creates user account and related employee records
public class EmployeeService
{
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EmployeeService>? _logger;

    public EmployeeService(AppDbContext context, IUserRepository userRepository, ILogger<EmployeeService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger;
    }

    // Creates a new employee account with all related info
    // Returns the user_ID of the newly created employee
    public async Task<int> RegisterEmployeeAsync(EmployeeRegistrationData employeeData)
    {
        // UserRepository now uses EF Core (same context), so we can use transactions
        // Create user account first, then employee-related records in a transaction
        try
        {
            // Create the user account first
            var user = new User
            {
                system_ID = employeeData.SystemId,
                first_name = employeeData.FirstName,
                mid_name = string.IsNullOrWhiteSpace(employeeData.MiddleName) ? null : employeeData.MiddleName,
                last_name = employeeData.LastName,
                suffix = string.IsNullOrWhiteSpace(employeeData.Suffix) ? null : employeeData.Suffix,
                birthdate = employeeData.BirthDate ?? DateTime.Today,
                age = (byte)(employeeData.Age ?? 0),
                gender = employeeData.Sex,
                contact_num = employeeData.ContactNumber,
                user_role = string.IsNullOrWhiteSpace(employeeData.RoleOther) ? employeeData.Role : employeeData.RoleOther,
                email = employeeData.Email,
                password = BCrypt.Net.BCrypt.HashPassword(employeeData.DefaultPassword),
                date_hired = DateTime.Now,
                status = "active"
            };

            // InsertAsync now returns the generated UserId directly (EF Core)
            int userId = await _userRepository.InsertAsync(user);
            _logger?.LogInformation("User created with ID: {UserId}", userId);

            // Now create the employee-related records in a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Add employee address
                var address = new EmployeeAddress
                {
                    UserId = userId,
                    HouseNo = string.IsNullOrWhiteSpace(employeeData.HouseNo) ? null : employeeData.HouseNo,
                    StreetName = string.IsNullOrWhiteSpace(employeeData.StreetName) ? null : employeeData.StreetName,
                    Province = string.IsNullOrWhiteSpace(employeeData.Province) ? null : employeeData.Province,
                    City = string.IsNullOrWhiteSpace(employeeData.City) ? null : employeeData.City,
                    Barangay = string.IsNullOrWhiteSpace(employeeData.Barangay) ? null : employeeData.Barangay,
                    Country = string.IsNullOrWhiteSpace(employeeData.Country) ? null : employeeData.Country,
                    ZipCode = string.IsNullOrWhiteSpace(employeeData.ZipCode) ? null : employeeData.ZipCode
                };

                _context.EmployeeAddresses.Add(address);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Employee address created for user ID: {UserId}", userId);

                // Add emergency contact info
                var emergencyContact = new EmployeeEmergencyContact
                {
                    UserId = userId,
                    FirstName = employeeData.EmergencyContactFirstName,
                    MiddleName = string.IsNullOrWhiteSpace(employeeData.EmergencyContactMiddleName) ? null : employeeData.EmergencyContactMiddleName,
                    LastName = employeeData.EmergencyContactLastName,
                    Suffix = string.IsNullOrWhiteSpace(employeeData.EmergencyContactSuffix) ? null : employeeData.EmergencyContactSuffix,
                    Relationship = string.IsNullOrWhiteSpace(employeeData.EmergencyContactRelationship) ? null : employeeData.EmergencyContactRelationship,
                    ContactNumber = string.IsNullOrWhiteSpace(employeeData.EmergencyContactNumber) ? null : employeeData.EmergencyContactNumber,
                    Address = string.IsNullOrWhiteSpace(employeeData.EmergencyContactAddress) ? null : employeeData.EmergencyContactAddress
                };

                _context.EmployeeEmergencyContacts.Add(emergencyContact);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Emergency contact created for user ID: {UserId}", userId);

                // Add salary information
                var salaryInfo = new SalaryInfo
                {
                    UserId = userId,
                    BaseSalary = employeeData.BaseSalary,
                    Allowance = employeeData.Allowance,
                    DateEffective = DateTime.Today,
                    IsActive = true
                };

                _context.SalaryInfos.Add(salaryInfo);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Salary information created for user ID: {UserId}", userId);

                await transaction.CommitAsync();

                return userId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Note: User was already created, but employee data failed
                // Can't rollback user since it uses a different DB connection
                _logger?.LogError(ex, "Error inserting employee data: {Message}. User ID {UserId} was created but employee data failed.", ex.Message, userId);
                throw new Exception($"Failed to register employee data: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error registering employee: {Message}", ex.Message);
            throw new Exception($"Failed to register employee: {ex.Message}", ex);
        }
    }

    // Retrieves all employee info by user ID
    public async Task<(User? user, EmployeeAddress? address, EmployeeEmergencyContact? emergencyContact, SalaryInfo? salaryInfo)> GetEmployeeByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var address = await _context.EmployeeAddresses.FirstOrDefaultAsync(a => a.UserId == userId);
        var emergencyContact = await _context.EmployeeEmergencyContacts.FirstOrDefaultAsync(e => e.UserId == userId);
        var salaryInfo = await _context.SalaryInfos.FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        return (user, address, emergencyContact, salaryInfo);
    }

    // Fetches all employees with their related data from view using EF Core ORM
    // Returns a list of EmployeeDisplayDto for table display
    // Uses EF Core's FromSqlRaw for secure, parameterized queries
    public async Task<List<EmployeeDisplayDto>> GetAllEmployeesAsync()
    {
        try
        {
            // Use EF Core's FromSqlRaw - secure and uses ORM protections
            // Query is static with no user input, so it's safe from SQL injection
            // Select only the columns that match the EmployeeDataView model
            // Note: ORDER BY cannot be used in views/derived tables without TOP/OFFSET
            // So we order using LINQ after fetching
            var employees = await _context.EmployeeDataViews
                .FromSqlRaw(@"
                    SELECT 
                        UserId,
                        SystemId,
                        FirstName,
                        MiddleName,
                        LastName,
                        Suffix,
                        FullName,
                        BirthDate,
                        Age,
                        Gender,
                        ContactNumber,
                        Role,
                        Email,
                        DateHired,
                        Status,
                        FormattedAddress
                    FROM [dbo].[vw_EmployeeData]")
                .OrderByDescending(e => e.DateHired)
                .ThenByDescending(e => e.UserId)
                .Select(e => new EmployeeDisplayDto
                {
                    UserId = e.UserId,
                    Id = e.SystemId ?? "N/A",
                    Name = e.FullName ?? "N/A",
                    Address = e.FormattedAddress ?? "N/A",
                    Contact = e.ContactNumber ?? "N/A",
                    Email = e.Email ?? "N/A",
                    Role = e.Role ?? "N/A",
                    Status = e.Status ?? "Active"
                })
                .ToListAsync();

            return employees;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching all employees: {Message}", ex.Message);
            throw new Exception($"Failed to fetch employees: {ex.Message}", ex);
        }
    }

    // Updates user status and logs the change
    // Returns true if successful, false otherwise
    public async Task<bool> UpdateUserStatusAsync(int userId, string newStatus, int changedByUserId, string? reason = null)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Get current user status
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger?.LogWarning("User with ID {UserId} not found", userId);
                    return false;
                }

                var oldStatus = (user.status ?? "active").ToLower();

                // Update user status (normalize to lowercase for database)
                var normalizedNewStatus = newStatus.ToLower();
                user.status = normalizedNewStatus;
                await _userRepository.UpdateAsync(user);
                _logger?.LogInformation("User {UserId} status updated from {OldStatus} to {NewStatus}", userId, oldStatus, normalizedNewStatus);

                // Log the status change (store normalized lowercase values)
                var statusLog = new UserStatusLog
                {
                    UserId = userId,
                    ChangedBy = changedByUserId,
                    OldStatus = oldStatus,
                    NewStatus = normalizedNewStatus,
                    Reason = reason,
                    CreatedAt = DateTime.Now
                };

                _context.UserStatusLogs.Add(statusLog);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Status change logged for user {UserId}", userId);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error updating user status: {Message}", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating user status: {Message}", ex.Message);
            throw new Exception($"Failed to update user status: {ex.Message}", ex);
        }
    }

    // Gets employee by UserId from the view
    public async Task<EmployeeDataView?> GetEmployeeDataViewByIdAsync(int userId)
    {
        try
        {
            var employee = await _context.EmployeeDataViews
                .FromSqlRaw(@"
                    SELECT 
                        UserId,
                        SystemId,
                        FirstName,
                        MiddleName,
                        LastName,
                        Suffix,
                        FullName,
                        BirthDate,
                        Age,
                        Gender,
                        ContactNumber,
                        Role,
                        Email,
                        DateHired,
                        Status,
                        FormattedAddress,
                        HouseNo,
                        StreetName,
                        Province,
                        City,
                        Barangay,
                        Country,
                        ZipCode,
                        EmergencyContactFirstName,
                        EmergencyContactMiddleName,
                        EmergencyContactLastName,
                        EmergencyContactSuffix,
                        EmergencyContactRelationship,
                        EmergencyContactNumber,
                        EmergencyContactAddress,
                        BaseSalary,
                        Allowance,
                        TotalSalary,
                        SalaryDateEffective,
                        SalaryIsActive
                    FROM [dbo].[vw_EmployeeData]
                    WHERE UserId = {0}", userId)
                .FirstOrDefaultAsync();

            return employee;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching employee by ID: {Message}", ex.Message);
            throw new Exception($"Failed to fetch employee: {ex.Message}", ex);
        }
    }
}

// DTO for displaying employee data in the HR table
public class EmployeeDisplayDto
{
    public int UserId { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// Holds all the data from the add employee form
// Maps to different tables: tbl_Users, tbl_employee_address, tbl_employee_emergency_contact, tbl_salary_info
public class EmployeeRegistrationData
{
    // Personal info - saved to tbl_Users
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleOther { get; set; } = string.Empty;
    public string SystemId { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;

    // Address info - saved to tbl_employee_address
    public string HouseNo { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    // Emergency contact info - saved to tbl_employee_emergency_contact
    public string EmergencyContactFirstName { get; set; } = string.Empty;
    public string EmergencyContactMiddleName { get; set; } = string.Empty;
    public string EmergencyContactLastName { get; set; } = string.Empty;
    public string EmergencyContactSuffix { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public string EmergencyContactAddress { get; set; } = string.Empty;

    // Salary info - saved to tbl_salary_info
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
}

