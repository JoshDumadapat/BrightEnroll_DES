using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.HR;

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

    // Registers a new employee - creates user account and employee-related records
    public async Task<int> RegisterEmployeeAsync(EmployeeRegistrationData employeeData)
    {
        try
        {
            // Step 1: Create User account (via UserRepository)
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(employeeData.DefaultPassword);
            
            var user = new User
            {
                system_ID = employeeData.SystemId,
                first_name = employeeData.FirstName,
                mid_name = string.IsNullOrWhiteSpace(employeeData.MiddleName) ? null : employeeData.MiddleName,
                last_name = employeeData.LastName,
                suffix = string.IsNullOrWhiteSpace(employeeData.Suffix) ? null : employeeData.Suffix,
                birthdate = employeeData.BirthDate ?? throw new ArgumentException("Birth date is required"),
                age = (byte)(employeeData.Age ?? throw new ArgumentException("Age is required")),
                gender = employeeData.Sex,
                contact_num = employeeData.ContactNumber,
                user_role = employeeData.Role,
                email = employeeData.Email,
                password = hashedPassword,
                date_hired = DateTime.Now,
                status = "active"
            };

            await _userRepository.InsertAsync(user);

            // Step 2: Get user_ID
            var insertedUser = await _userRepository.GetBySystemIdAsync(user.system_ID);
            if (insertedUser == null)
            {
                throw new Exception("Failed to retrieve created user");
            }
            int userId = insertedUser.user_ID;

            // Step 3: Create employee-related records (EF Core transaction)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create address
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

                // Create emergency contact
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

                // Create salary info
                var salaryInfo = new SalaryInfo
                {
                    UserId = userId,
                    BaseSalary = employeeData.BaseSalary,
                    Allowance = employeeData.Allowance,
                    DateEffective = DateTime.Today,
                    IsActive = true
                };

                _context.SalaryInfos.Add(salaryInfo);

                // Save all changes
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                _logger?.LogInformation("Employee registered successfully: {SystemId} (User ID: {UserId})", user.system_ID, userId);
                return userId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error creating employee records for user {UserId}: {Message}", userId, ex.Message);
                throw new Exception($"Failed to create employee records: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error registering employee: {Message}", ex.Message);
            throw new Exception($"Failed to register employee: {ex.Message}", ex);
        }
    }

    // Checks for duplicate employees based on name matching
    public async Task<EmployeeDuplicateCheckResult?> CheckForDuplicateEmployeeAsync(EmployeeRegistrationData employeeData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(employeeData.FirstName) || string.IsNullOrWhiteSpace(employeeData.LastName))
            {
                return null;
            }

            // Get employee data view with matching first and last name (case-insensitive)
            var matchingEmployees = await _context.EmployeeDataViews
                .Where(e => e.FirstName != null && e.LastName != null &&
                           e.FirstName.ToLower() == employeeData.FirstName.ToLower() &&
                           e.LastName.ToLower() == employeeData.LastName.ToLower())
                .ToListAsync();

            if (!matchingEmployees.Any())
            {
                return new EmployeeDuplicateCheckResult
                {
                    IsDuplicate = false
                };
            }

            // Return first match as potential duplicate
            var duplicateEmployee = matchingEmployees.First();
            
            // Build full name with suffix if available
            var fullName = duplicateEmployee.FullName ?? 
                $"{duplicateEmployee.FirstName} {duplicateEmployee.MiddleName ?? ""} {duplicateEmployee.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(duplicateEmployee.Suffix))
            {
                fullName += $" {duplicateEmployee.Suffix}";
            }

            return new EmployeeDuplicateCheckResult
            {
                IsDuplicate = true,
                ExistingEmployeeId = duplicateEmployee.UserId,
                ExistingSystemId = duplicateEmployee.SystemId ?? "",
                ExistingName = $"{duplicateEmployee.FirstName} {duplicateEmployee.LastName}",
                ExistingFullName = fullName,
                ExistingEmail = duplicateEmployee.Email ?? "",
                ExistingContactNumber = duplicateEmployee.ContactNumber ?? "",
                ExistingBirthDate = duplicateEmployee.BirthDate,
                ExistingAddress = duplicateEmployee.FormattedAddress ?? 
                    BuildAddressString(duplicateEmployee.HouseNo, duplicateEmployee.StreetName, 
                        duplicateEmployee.Barangay, duplicateEmployee.City, 
                        duplicateEmployee.Province, duplicateEmployee.Country, duplicateEmployee.ZipCode)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking for duplicate employee: {Message}", ex.Message);
            return null;
        }
    }

    // Helper method to build address string
    private string BuildAddressString(string? houseNo, string? streetName, string? barangay, 
        string? city, string? province, string? country, string? zipCode)
    {
        var addressParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(houseNo)) addressParts.Add(houseNo);
        if (!string.IsNullOrWhiteSpace(streetName)) addressParts.Add(streetName);
        if (!string.IsNullOrWhiteSpace(barangay)) addressParts.Add(barangay);
        if (!string.IsNullOrWhiteSpace(city)) addressParts.Add(city);
        if (!string.IsNullOrWhiteSpace(province)) addressParts.Add(province);
        if (!string.IsNullOrWhiteSpace(country)) addressParts.Add(country);
        if (!string.IsNullOrWhiteSpace(zipCode)) addressParts.Add(zipCode);
        
        return string.Join(", ", addressParts);
    }

    // Gets all employees as display DTOs
    public async Task<List<EmployeeDisplayDto>> GetAllEmployeesAsync()
    {
        try
        {
            var employeeViews = await _context.EmployeeDataViews
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            return employeeViews.Select(e => new EmployeeDisplayDto
            {
                UserId = e.UserId,
                Id = e.SystemId ?? "",
                Name = e.FullName ?? $"{e.FirstName} {e.LastName}",
                Email = e.Email ?? "",
                Contact = e.ContactNumber ?? "",
                Role = e.Role ?? "",
                Status = e.Status ?? "Active",
                Address = e.FormattedAddress ?? BuildAddressString(e.HouseNo, e.StreetName, e.Barangay, e.City, e.Province, e.Country, e.ZipCode)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all employees: {Message}", ex.Message);
            throw;
        }
    }

    // Gets employee by ID
    public async Task<EmployeeDataView?> GetEmployeeByIdAsync(int userId)
    {
        try
        {
            return await _context.EmployeeDataViews
                .FirstOrDefaultAsync(e => e.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting employee by ID {UserId}: {Message}", userId, ex.Message);
            throw;
        }
    }

    // Gets employee data view by ID (alias for GetEmployeeByIdAsync for consistency)
    public async Task<EmployeeDataView?> GetEmployeeDataViewByIdAsync(int userId)
    {
        return await GetEmployeeByIdAsync(userId);
    }

    // Gets employee by system ID
    public async Task<EmployeeDataView?> GetEmployeeBySystemIdAsync(string systemId)
    {
        try
        {
            return await _context.EmployeeDataViews
                .FirstOrDefaultAsync(e => e.SystemId == systemId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting employee by system ID {SystemId}: {Message}", systemId, ex.Message);
            throw;
        }
    }

    // Updates user status and logs the change
    public async Task<bool> UpdateUserStatusAsync(int userId, string newStatus, int changedByUserId, string? reason)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger?.LogWarning("User with ID {UserId} not found for status update", userId);
                return false;
            }

            var oldStatus = user.status ?? "active";
            var normalizedNewStatus = newStatus.Trim();

            // Update user status
            user.status = normalizedNewStatus;
            await _userRepository.UpdateAsync(user);

            // Log the status change
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

            _logger?.LogInformation("User {UserId} status updated from {OldStatus} to {NewStatus} by {ChangedBy}", 
                userId, oldStatus, normalizedNewStatus, changedByUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating user status for user {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    // Gets the inactive reason for an employee
    public async Task<string?> GetInactiveReasonAsync(int userId)
    {
        try
        {
            var latestInactiveLog = await _context.UserStatusLogs
                .Where(log => log.UserId == userId && 
                             log.NewStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(log => log.CreatedAt)
                .FirstOrDefaultAsync();

            return latestInactiveLog?.Reason;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting inactive reason for user {UserId}: {Message}", userId, ex.Message);
            return null;
        }
    }

    // Updates employee contact information (email and contact number only)
    public async Task<bool> UpdateEmployeeContactInfoAsync(int userId, string contactNumber, string email)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger?.LogWarning("User with ID {UserId} not found for contact info update", userId);
                return false;
            }

            // Update contact information
            user.contact_num = contactNumber;
            user.email = email;

            await _userRepository.UpdateAsync(user);

            _logger?.LogInformation("Employee {UserId} contact information updated successfully", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating employee contact info for user {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    // Updates employee information
    public async Task<bool> UpdateEmployeeInfoAsync(int userId, EmployeeUpdateData updateData)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger?.LogWarning("User with ID {UserId} not found for update", userId);
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update user entity
                user.first_name = updateData.FirstName;
                user.mid_name = string.IsNullOrWhiteSpace(updateData.MiddleName) ? null : updateData.MiddleName;
                user.last_name = updateData.LastName;
                user.suffix = string.IsNullOrWhiteSpace(updateData.Suffix) ? null : updateData.Suffix;
                user.birthdate = updateData.BirthDate ?? user.birthdate;
                user.age = (byte)(updateData.Age ?? user.age);
                user.gender = updateData.Sex;
                user.contact_num = updateData.ContactNumber;
                user.email = updateData.Email;
                user.user_role = updateData.Role;

                await _userRepository.UpdateAsync(user);

                // Update address
                var address = await _context.EmployeeAddresses
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (address != null)
                {
                    address.HouseNo = string.IsNullOrWhiteSpace(updateData.HouseNo) ? null : updateData.HouseNo;
                    address.StreetName = string.IsNullOrWhiteSpace(updateData.StreetName) ? null : updateData.StreetName;
                    address.Province = string.IsNullOrWhiteSpace(updateData.Province) ? null : updateData.Province;
                    address.City = string.IsNullOrWhiteSpace(updateData.City) ? null : updateData.City;
                    address.Barangay = string.IsNullOrWhiteSpace(updateData.Barangay) ? null : updateData.Barangay;
                    address.Country = string.IsNullOrWhiteSpace(updateData.Country) ? null : updateData.Country;
                    address.ZipCode = string.IsNullOrWhiteSpace(updateData.ZipCode) ? null : updateData.ZipCode;
                }
                else
                {
                    address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = string.IsNullOrWhiteSpace(updateData.HouseNo) ? null : updateData.HouseNo,
                        StreetName = string.IsNullOrWhiteSpace(updateData.StreetName) ? null : updateData.StreetName,
                        Province = string.IsNullOrWhiteSpace(updateData.Province) ? null : updateData.Province,
                        City = string.IsNullOrWhiteSpace(updateData.City) ? null : updateData.City,
                        Barangay = string.IsNullOrWhiteSpace(updateData.Barangay) ? null : updateData.Barangay,
                        Country = string.IsNullOrWhiteSpace(updateData.Country) ? null : updateData.Country,
                        ZipCode = string.IsNullOrWhiteSpace(updateData.ZipCode) ? null : updateData.ZipCode
                    };
                    _context.EmployeeAddresses.Add(address);
                }

                // Update emergency contact
                var emergencyContact = await _context.EmployeeEmergencyContacts
                    .FirstOrDefaultAsync(ec => ec.UserId == userId);

                if (emergencyContact != null)
                {
                    emergencyContact.FirstName = updateData.EmergencyContactFirstName;
                    emergencyContact.MiddleName = string.IsNullOrWhiteSpace(updateData.EmergencyContactMiddleName) ? null : updateData.EmergencyContactMiddleName;
                    emergencyContact.LastName = updateData.EmergencyContactLastName;
                    emergencyContact.Suffix = string.IsNullOrWhiteSpace(updateData.EmergencyContactSuffix) ? null : updateData.EmergencyContactSuffix;
                    emergencyContact.Relationship = string.IsNullOrWhiteSpace(updateData.EmergencyContactRelationship) ? null : updateData.EmergencyContactRelationship;
                    emergencyContact.ContactNumber = string.IsNullOrWhiteSpace(updateData.EmergencyContactNumber) ? null : updateData.EmergencyContactNumber;
                    emergencyContact.Address = string.IsNullOrWhiteSpace(updateData.EmergencyContactAddress) ? null : updateData.EmergencyContactAddress;
                }
                else
                {
                    emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = updateData.EmergencyContactFirstName,
                        MiddleName = string.IsNullOrWhiteSpace(updateData.EmergencyContactMiddleName) ? null : updateData.EmergencyContactMiddleName,
                        LastName = updateData.EmergencyContactLastName,
                        Suffix = string.IsNullOrWhiteSpace(updateData.EmergencyContactSuffix) ? null : updateData.EmergencyContactSuffix,
                        Relationship = string.IsNullOrWhiteSpace(updateData.EmergencyContactRelationship) ? null : updateData.EmergencyContactRelationship,
                        ContactNumber = string.IsNullOrWhiteSpace(updateData.EmergencyContactNumber) ? null : updateData.EmergencyContactNumber,
                        Address = string.IsNullOrWhiteSpace(updateData.EmergencyContactAddress) ? null : updateData.EmergencyContactAddress
                    };
                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger?.LogInformation("Employee {UserId} information updated successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error updating employee {UserId} information: {Message}", userId, ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating employee information: {Message}", ex.Message);
            return false;
        }
    }
}

// DTO for employee registration
public class EmployeeRegistrationData
{
    // Personal Information
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
    public string SystemId { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;

    // Address Information
    public string HouseNo { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    // Emergency Contact Information
    public string EmergencyContactFirstName { get; set; } = string.Empty;
    public string EmergencyContactMiddleName { get; set; } = string.Empty;
    public string EmergencyContactLastName { get; set; } = string.Empty;
    public string EmergencyContactSuffix { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public string EmergencyContactAddress { get; set; } = string.Empty;

    // Salary Information
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
}

// Result of duplicate employee check
public class EmployeeDuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public int? ExistingEmployeeId { get; set; }
    public string? ExistingSystemId { get; set; }
    public string? ExistingName { get; set; }
    public string? ExistingFullName { get; set; }
    public string? ExistingEmail { get; set; }
    public string? ExistingContactNumber { get; set; }
    public DateTime? ExistingBirthDate { get; set; }
    public string? ExistingAddress { get; set; }
}

// DTO for displaying employees in lists
public class EmployeeDisplayDto
{
    public int UserId { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

// DTO for updating employee information
public class EmployeeUpdateData
{
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
    public string HouseNo { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string EmergencyContactFirstName { get; set; } = string.Empty;
    public string EmergencyContactMiddleName { get; set; } = string.Empty;
    public string EmergencyContactLastName { get; set; } = string.Empty;
    public string EmergencyContactSuffix { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public string EmergencyContactAddress { get; set; } = string.Empty;
}

