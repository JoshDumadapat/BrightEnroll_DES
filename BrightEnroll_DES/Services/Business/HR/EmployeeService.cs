using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.HR;

public class EmployeeService
{
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly SchoolYearService _schoolYearService;
    private readonly NotificationService? _notificationService;
    private readonly ILogger<EmployeeService>? _logger;

    public EmployeeService(AppDbContext context, IUserRepository userRepository, SchoolYearService schoolYearService, ILogger<EmployeeService>? logger = null, NotificationService? notificationService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _schoolYearService = schoolYearService ?? throw new ArgumentNullException(nameof(schoolYearService));
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<int> RegisterEmployeeAsync(EmployeeRegistrationData employeeData)
    {
        try
        {
            _logger?.LogInformation("=== STARTING EMPLOYEE REGISTRATION ===");
            _logger?.LogInformation("Employee Data: FirstName={FirstName}, LastName={LastName}, Email={Email}, SystemId={SystemId}", 
                employeeData.FirstName, employeeData.LastName, employeeData.Email, employeeData.SystemId);
            
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

            _logger?.LogInformation("Creating user account with SystemId={SystemId}, Email={Email}, Role={Role}", 
                user.system_ID, user.email, user.user_role);
            
            try
            {
                await _userRepository.InsertAsync(user);
                _logger?.LogInformation("User account created successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "FAILED TO CREATE USER ACCOUNT. SystemId={SystemId}, Email={Email}", 
                    user.system_ID, user.email);
                LogDetailedException(ex, "UserRepository.InsertAsync");
                throw new Exception($"Failed to create user account: {ex.Message}", ex);
            }

            _logger?.LogInformation("Retrieving created user with SystemId={SystemId}", user.system_ID);
            var insertedUser = await _userRepository.GetBySystemIdAsync(user.system_ID);
            if (insertedUser == null)
            {
                _logger?.LogError("CRITICAL: User was created but cannot be retrieved. SystemId={SystemId}", user.system_ID);
                throw new Exception($"Failed to retrieve created user with SystemId: {user.system_ID}");
            }
            int userId = insertedUser.user_ID;
            _logger?.LogInformation("User retrieved successfully. UserId={UserId}", userId);

            _logger?.LogInformation("Starting EF Core transaction for employee records (UserId={UserId})", userId);
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger?.LogInformation("Creating employee address record");
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
                _logger?.LogInformation("Address data: HouseNo={HouseNo}, Street={Street}, City={City}, Province={Province}", 
                    address.HouseNo, address.StreetName, address.City, address.Province);

                _context.EmployeeAddresses.Add(address);

                _logger?.LogInformation("Creating emergency contact record");
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
                _logger?.LogInformation("Emergency contact data: Name={FirstName} {LastName}, Relationship={Relationship}, Contact={ContactNumber}", 
                    emergencyContact.FirstName, emergencyContact.LastName, emergencyContact.Relationship, emergencyContact.ContactNumber);

                _context.EmployeeEmergencyContacts.Add(emergencyContact);

                _logger?.LogInformation("Creating salary info record");
                var currentSchoolYear = _schoolYearService.GetCurrentSchoolYear();
                var salaryInfo = new SalaryInfo
                {
                    UserId = userId,
                    BaseSalary = employeeData.BaseSalary,
                    Allowance = employeeData.Allowance,
                    DateEffective = DateTime.Today,
                    IsActive = employeeData.IsSalaryActive, // Use flag from employeeData
                    SchoolYear = currentSchoolYear
                };
                _logger?.LogInformation("Salary data: BaseSalary={BaseSalary}, Allowance={Allowance}, Total={Total}, SchoolYear={SchoolYear}", 
                    salaryInfo.BaseSalary, salaryInfo.Allowance, salaryInfo.BaseSalary + salaryInfo.Allowance, salaryInfo.SchoolYear);

                _context.SalaryInfos.Add(salaryInfo);

                _logger?.LogInformation("Entities to be saved: Address={AddressState}, EmergencyContact={EmergencyContactState}, SalaryInfo={SalaryInfoState}", 
                    _context.Entry(address).State,
                    _context.Entry(emergencyContact).State,
                    _context.Entry(salaryInfo).State);

                _logger?.LogInformation("Calling SaveChangesAsync()...");
                try
                {
                    await _context.SaveChangesAsync();
                    _logger?.LogInformation("SaveChangesAsync() completed successfully");
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    _logger?.LogError(dbEx, "=== DATABASE UPDATE EXCEPTION ===");
                    LogDetailedException(dbEx, "SaveChangesAsync");
                    
                    // Log all entries that failed
                    if (dbEx.Entries != null && dbEx.Entries.Any())
                    {
                        _logger?.LogError("Failed entries count: {Count}", dbEx.Entries.Count);
                        foreach (var entry in dbEx.Entries)
                        {
                            _logger?.LogError("Failed Entry: EntityType={EntityType}, State={State}", 
                                entry.Entity.GetType().Name, entry.State);
                            
                            foreach (var prop in entry.Properties)
                            {
                                _logger?.LogError("  Property: {PropertyName}={PropertyValue} (IsModified={IsModified}, CurrentValue={CurrentValue})", 
                                    prop.Metadata.Name, prop.CurrentValue, prop.IsModified, prop.CurrentValue);
                            }
                        }
                    }
                    
                    throw;
                }

                _logger?.LogInformation("Committing transaction...");
                await transaction.CommitAsync();
                _logger?.LogInformation("Transaction committed successfully");

                _logger?.LogInformation("=== EMPLOYEE REGISTRATION SUCCESS ===");
                _logger?.LogInformation("Employee registered successfully: {SystemId} (User ID: {UserId})", user.system_ID, userId);
                return userId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "=== ROLLING BACK TRANSACTION ===");
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error creating employee records for user {UserId}: {Message}", userId, ex.Message);
                LogDetailedException(ex, "Employee Records Creation");
                
                // Delete the user that was created before the transaction
                try
                {
                    _logger?.LogWarning("Attempting to delete user {UserId} due to transaction failure", userId);
                    await _userRepository.DeleteAsync(userId);
                    _logger?.LogInformation("User {UserId} deleted successfully after transaction failure", userId);
                }
                catch (Exception deleteEx)
                {
                    _logger?.LogError(deleteEx, "CRITICAL: Failed to delete user {UserId} after transaction failure. Manual cleanup may be required.", userId);
                }
                
                throw new Exception($"Failed to create employee records: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "=== EMPLOYEE REGISTRATION FAILED ===");
            _logger?.LogError(ex, "Error registering employee: {Message}", ex.Message);
            LogDetailedException(ex, "RegisterEmployeeAsync");
            throw new Exception($"Failed to register employee: {ex.Message}", ex);
        }
    }

    private void LogDetailedException(Exception ex, string context)
    {
        _logger?.LogError("=== DETAILED EXCEPTION LOG - {Context} ===", context);
        _logger?.LogError("Exception Type: {ExceptionType}", ex.GetType().FullName);
        _logger?.LogError("Exception Message: {Message}", ex.Message);
        _logger?.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
        
        var sqlEx = ex as Microsoft.Data.SqlClient.SqlException;
        if (sqlEx == null && ex.InnerException is Microsoft.Data.SqlClient.SqlException innerSqlEx)
        {
            sqlEx = innerSqlEx;
        }
        
        if (sqlEx != null)
        {
            _logger?.LogError("=== SQL EXCEPTION DETAILS ===");
            _logger?.LogError("SQL Error Number: {Number}", sqlEx.Number);
            _logger?.LogError("SQL Severity: {Severity}", sqlEx.Class);
            _logger?.LogError("SQL State: {State}", sqlEx.State);
            _logger?.LogError("SQL Server: {Server}", sqlEx.Server ?? "N/A");
            _logger?.LogError("SQL Procedure: {Procedure}", sqlEx.Procedure ?? "N/A");
            _logger?.LogError("SQL Line Number: {LineNumber}", sqlEx.LineNumber);
            _logger?.LogError("SQL Error Message: {Message}", sqlEx.Message);
            
            if (sqlEx.Errors != null && sqlEx.Errors.Count > 0)
            {
                _logger?.LogError("SQL Errors Count: {Count}", sqlEx.Errors.Count);
                for (int i = 0; i < sqlEx.Errors.Count; i++)
                {
                    var error = sqlEx.Errors[i];
                    _logger?.LogError("  SQL Error #{Index}: Number={Number}, Message={Message}, Class={Class}, State={State}", 
                        i + 1, error.Number, error.Message, error.Class, error.State);
                }
            }
        }
        
        var dbUpdateEx = ex as Microsoft.EntityFrameworkCore.DbUpdateException;
        if (dbUpdateEx == null && ex.InnerException is Microsoft.EntityFrameworkCore.DbUpdateException innerDbUpdateEx)
        {
            dbUpdateEx = innerDbUpdateEx;
        }
        
        if (dbUpdateEx != null)
        {
            _logger?.LogError("=== DB UPDATE EXCEPTION DETAILS ===");
            _logger?.LogError("Entries Count: {Count}", dbUpdateEx.Entries?.Count ?? 0);
            
            if (dbUpdateEx.Entries != null && dbUpdateEx.Entries.Any())
            {
                foreach (var entry in dbUpdateEx.Entries)
                {
                    _logger?.LogError("Failed Entity: Type={EntityType}, State={State}", 
                        entry.Entity.GetType().Name, entry.State);
                    
                    foreach (var prop in entry.Properties)
                    {
                        var currentValue = prop.CurrentValue;
                        var originalValue = prop.OriginalValue;
                        var maxLength = prop.Metadata.GetMaxLength();
                        
                        _logger?.LogError("  Property: {PropertyName}", prop.Metadata.Name);
                        _logger?.LogError("    Current Value: {CurrentValue} (Length: {CurrentLength})", 
                            currentValue, currentValue?.ToString()?.Length ?? 0);
                        _logger?.LogError("    Original Value: {OriginalValue}", originalValue);
                        _logger?.LogError("    Is Modified: {IsModified}", prop.IsModified);
                        if (maxLength.HasValue)
                        {
                            _logger?.LogError("    Max Length: {MaxLength}", maxLength.Value);
                            if (currentValue != null && currentValue.ToString()?.Length > maxLength.Value)
                            {
                                _logger?.LogError("    *** VALUE EXCEEDS MAX LENGTH! ***");
                            }
                        }
                    }
                }
            }
        }
        
        Exception? innerEx = ex.InnerException;
        int depth = 1;
        while (innerEx != null && depth <= 5) // Limit depth to prevent infinite loops
        {
            _logger?.LogError("--- Inner Exception #{Depth} ---", depth);
            _logger?.LogError("Type: {Type}", innerEx.GetType().FullName);
            _logger?.LogError("Message: {Message}", innerEx.Message);
            innerEx = innerEx.InnerException;
            depth++;
        }
        
        _logger?.LogError("=== END DETAILED EXCEPTION LOG ===");
    }

    public async Task<EmployeeDuplicateCheckResult?> CheckForDuplicateEmployeeAsync(EmployeeRegistrationData employeeData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(employeeData.FirstName) || string.IsNullOrWhiteSpace(employeeData.LastName))
            {
                return null;
            }

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

            var duplicateEmployee = matchingEmployees.First();
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

    public async Task<List<EmployeeDisplayDto>> GetAllEmployeesAsync()
    {
        try
        {
            // Exclude inactive employees - they should only appear in the Archive page
            // Note: The database view normalizes status to proper case (first letter uppercase)
            // and defaults null/empty status to 'Active', so we just need to exclude 'Inactive'
            var employeeViews = await _context.EmployeeDataViews
                .Where(e => e.Status == null || e.Status != "Inactive")
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

            user.status = normalizedNewStatus;
            await _userRepository.UpdateAsync(user);

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

    public async Task<bool> UpdateEmployeeInfoAsync(int userId, EmployeeUpdateData updateData, int? requestedByUserId = null)
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

                // Handle salary update ONLY if salary values were actually provided AND changed
                // Check if salary fields were explicitly provided (not just default values)
                bool salaryWasEdited = updateData.BaseSalary.HasValue || updateData.Allowance.HasValue;
                
                if (salaryWasEdited)
                {
                    var currentSalary = await _context.SalaryInfos
                        .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

                    if (currentSalary != null)
                    {
                        var newBaseSalary = updateData.BaseSalary ?? currentSalary.BaseSalary;
                        var newAllowance = updateData.Allowance ?? currentSalary.Allowance;

                        // IMPORTANT: Only process salary update if values actually changed
                        // This prevents creating log entries when salary wasn't edited
                        bool salaryActuallyChanged = (newBaseSalary != currentSalary.BaseSalary) || 
                                                    (newAllowance != currentSalary.Allowance);
                        
                        if (!salaryActuallyChanged)
                        {
                            // Salary values are the same - skip salary update entirely
                            // Only update other employee info fields
                            _logger?.LogInformation("Salary values unchanged for user {UserId}, skipping salary update", userId);
                        }
                        else
                        {
                            // Salary values changed - proceed with salary update logic
                            // Check if salary change requires approval (threshold check - cumulative approach)
                            // Threshold is dynamically loaded from Payroll module (tbl_roles.threshold_percentage)
                            // CUMULATIVE APPROACH: Always compares new salary to role base * (1 + threshold%)
                            // If threshold is 0%, no approval needed (no limit on salary increase)
                            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == updateData.Role);
                            decimal defaultBaseSalary = role?.BaseSalary ?? currentSalary.BaseSalary;
                            decimal defaultAllowance = role?.Allowance ?? currentSalary.Allowance;
                            decimal thresholdPercentage = role?.ThresholdPercentage ?? 0.00m;
                            
                            bool requiresApproval = false;
                            
                            // If threshold is 0%, no approval needed - allow any salary increase
                            if (thresholdPercentage == 0.00m)
                            {
                                _logger?.LogInformation(
                                    "Salary threshold check for {UserId} ({Role}): " +
                                    "Threshold is 0% - No approval required. Allowing direct update.",
                                    userId, updateData.Role);
                                requiresApproval = false;
                            }
                            else
                            {
                                // Threshold > 0% - check if exceeds threshold using CUMULATIVE approach
                                // Compare new salary to role base * (1 + threshold%)
                                decimal salaryThresholdPercentage = thresholdPercentage / 100m;

                                // Calculate threshold amounts (CUMULATIVE: always based on role base, not current salary)
                                var thresholdBaseSalary = defaultBaseSalary * (1 + salaryThresholdPercentage);
                                var thresholdAllowance = defaultAllowance * (1 + salaryThresholdPercentage);

                                // CUMULATIVE CHECK: Compare new salary to role base + threshold
                                // This ensures threshold doesn't restart - if previous increase used 7% of 10%,
                                // remaining threshold is still 3% (new salary must be <= role base * 1.10)
                                bool baseSalaryExceeds = newBaseSalary > thresholdBaseSalary;
                                bool allowanceExceeds = newAllowance > thresholdAllowance;
                                
                                if (baseSalaryExceeds || allowanceExceeds)
                                {
                                    requiresApproval = true;
                                    _logger?.LogInformation(
                                        "Salary threshold check for {UserId} ({Role}): " +
                                        "Base Salary: {NewBase:N2} > {ThresholdBase:N2} (role base: {RoleBase:N2} + {Threshold}% threshold) = {BaseExceeds}, " +
                                        "Allowance: {NewAllowance:N2} > {ThresholdAllowance:N2} (role base: {RoleAllowance:N2} + {Threshold}% threshold) = {AllowanceExceeds}",
                                        userId, updateData.Role, newBaseSalary, thresholdBaseSalary, defaultBaseSalary, thresholdPercentage, baseSalaryExceeds,
                                        newAllowance, thresholdAllowance, defaultAllowance, thresholdPercentage, allowanceExceeds);
                                }
                                else
                                {
                                    _logger?.LogInformation(
                                        "Salary threshold check for {UserId} ({Role}): " +
                                        "Within threshold ({Threshold}%) - No approval required.",
                                        userId, updateData.Role, thresholdPercentage);
                                }
                            }

                            if (requiresApproval)
                            {
                                // IMPORTANT: Create a NEW salary change request - each change creates a separate log entry
                                // This ensures a complete audit trail - never updates existing records
                                if (requestedByUserId.HasValue)
                                {
                                    var currentSchoolYear = _schoolYearService.GetCurrentSchoolYear();
                                    // Create a NEW salary change request record for this change
                                    var salaryRequest = new SalaryChangeRequest
                                    {
                                        UserId = userId,
                                        CurrentBaseSalary = currentSalary.BaseSalary,
                                        CurrentAllowance = currentSalary.Allowance,
                                        RequestedBaseSalary = newBaseSalary,
                                        RequestedAllowance = newAllowance,
                                        Reason = "Salary update from employee profile edit",
                                        Status = "Pending",
                                        RequestedBy = requestedByUserId.Value, // Current logged-in HR user ID
                                        RequestedAt = DateTime.Now,
                                        SchoolYear = currentSchoolYear,
                                        IsInitialRegistration = false
                                    };
                                    // Add new record to log - this creates a separate entry for this salary change
                                    _context.SalaryChangeRequests.Add(salaryRequest);
                                    _logger?.LogInformation("Salary change request created for user {UserId} due to threshold", userId);
                                }
                                else
                                {
                                    _logger?.LogWarning("Salary change requires approval but no requestedByUserId provided. Skipping salary update.");
                                }
                            }
                            else
                            {
                                // Direct update if within threshold
                                var oldBaseSalary = currentSalary.BaseSalary;
                                var oldAllowance = currentSalary.Allowance;
                                
                                currentSalary.BaseSalary = newBaseSalary;
                                currentSalary.Allowance = newAllowance;
                                _context.SalaryInfos.Update(currentSalary);
                                
                                // IMPORTANT: Record the salary change in the salary change log (auto-approved since below threshold)
                                // Each salary change creates a NEW log entry - never updates existing records
                                // This ensures a complete audit trail of all salary changes over time
                                // Note: We already checked salaryActuallyChanged above, so this will always be true here
                                if (requestedByUserId.HasValue)
                                {
                                    var currentSchoolYear = _schoolYearService.GetCurrentSchoolYear();
                                    // Create a NEW salary change request record - this is a log entry, not an update
                                    var salaryChangeRequest = new SalaryChangeRequest
                                    {
                                        UserId = userId,
                                        CurrentBaseSalary = oldBaseSalary,
                                        CurrentAllowance = oldAllowance,
                                        RequestedBaseSalary = newBaseSalary,
                                        RequestedAllowance = newAllowance,
                                        Reason = "Salary update from employee profile edit (below threshold - auto-approved)",
                                        Status = "Approved", // Auto-approved since below threshold
                                        RequestedBy = requestedByUserId.Value,
                                        ApprovedBy = requestedByUserId.Value, // Same user since auto-approved
                                        RequestedAt = DateTime.Now,
                                        ApprovedAt = DateTime.Now,
                                        EffectiveDate = DateTime.Today,
                                        SchoolYear = currentSchoolYear,
                                        IsInitialRegistration = false
                                    };
                                    // Add new record to log - this creates a separate entry for this salary change
                                    _context.SalaryChangeRequests.Add(salaryChangeRequest);
                                    await _context.SaveChangesAsync(); // Save to get the RequestId
                                    
                                    // Get employee info for notification
                                    var employee = await _context.Users.FindAsync(userId);
                                    var employeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "Unknown";
                                    
                                    // Create notification for salary change log
                                    if (_notificationService != null)
                                    {
                                        await _notificationService.CreateNotificationAsync(
                                            notificationType: "SalaryChange",
                                            title: "Salary Updated (Auto-Approved)",
                                            message: $"Salary for {employeeName} has been updated directly (below threshold). New salary: ₱{newBaseSalary:N2} + ₱{newAllowance:N2}",
                                            referenceType: "SalaryChangeRequest",
                                            referenceId: salaryChangeRequest.RequestId,
                                            actionUrl: "/human-resource?tab=SalaryChangeLog",
                                            priority: "Normal",
                                            createdBy: requestedByUserId.Value
                                        );
                                    }
                                    
                                    _logger?.LogInformation("Salary change recorded in log for user {UserId} (auto-approved, below threshold)", userId);
                                }
                                
                                _logger?.LogInformation("Salary updated directly for user {UserId} (within threshold)", userId);
                            }
                        }
                    }
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

public class EmployeeRegistrationData
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

    public string EmergencyContactFirstName { get; set; } = string.Empty;
    public string EmergencyContactMiddleName { get; set; } = string.Empty;
    public string EmergencyContactLastName { get; set; } = string.Empty;
    public string EmergencyContactSuffix { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public string EmergencyContactAddress { get; set; } = string.Empty;

    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public bool IsSalaryActive { get; set; } = true; // Default to active, set to false if approval needed
}

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
    public decimal? BaseSalary { get; set; }
    public decimal? Allowance { get; set; }
}

