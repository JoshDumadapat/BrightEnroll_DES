using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Infrastructure;

namespace BrightEnroll_DES.Services.Sync;

public interface ISyncService
{
    Task<bool> SyncAsync();
    Task<bool> IsOnlineAsync();
}

public class SyncService : ISyncService
{
    private readonly IDbContextFactory<LocalDbContext> _localContextFactory;
    private readonly IDbContextFactory<CloudDbContext> _cloudContextFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly ILogger<SyncService>? _logger;
    private const int BatchSize = 100; // Process entities in batches

    public SyncService(
        IDbContextFactory<LocalDbContext> localContextFactory,
        IDbContextFactory<CloudDbContext> cloudContextFactory,
        IConnectivityService connectivityService,
        ILogger<SyncService>? logger = null)
    {
        _localContextFactory = localContextFactory ?? throw new ArgumentNullException(nameof(localContextFactory));
        _cloudContextFactory = cloudContextFactory ?? throw new ArgumentNullException(nameof(cloudContextFactory));
        _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
        _logger = logger;
    }

    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            return networkAccess == NetworkAccess.Internet;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SyncAsync()
    {
        try
        {
            // Check if online
            if (!await IsOnlineAsync())
            {
                _logger?.LogInformation("Sync skipped: No internet connection");
                return false;
            }

            _logger?.LogInformation("Starting sync process...");

            // Test cloud database connection before attempting sync
            try
            {
                await using var testContext = await _cloudContextFactory.CreateDbContextAsync();
                _logger?.LogInformation("Testing cloud database connection...");
                
                var canConnect = await testContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger?.LogError("Cannot connect to cloud database. The server may be unreachable, firewall is blocking, or the connection string is incorrect.");
                    _logger?.LogError("Please verify: 1) Server name and port are correct, 2) Firewall allows port 1433, 3) Connection string in appsettings.json is valid");
                    return false;
                }
                _logger?.LogInformation("Cloud database connection verified successfully");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                _logger?.LogError(sqlEx, "SQL Server connection error. Error Number: {Number}, State: {State}, Class: {Class}. Message: {Message}", 
                    sqlEx.Number, sqlEx.State, sqlEx.Class, sqlEx.Message);
                _logger?.LogError("This usually means: 1) Server is not reachable, 2) Wrong server name/port, 3) Firewall blocking, 4) SQL Server not configured for remote connections");
                return false;
            }
            catch (Exception connEx)
            {
                _logger?.LogError(connEx, "Cloud database connection test failed. Error: {Message}. Inner Exception: {InnerException}. Sync aborted.", 
                    connEx.Message, connEx.InnerException?.Message ?? "None");
                return false;
            }

            int totalSynced = 0;

            // Sync in dependency order
            totalSynced += await SyncGuardiansAsync();
            totalSynced += await SyncUsersAsync();
            totalSynced += await SyncStudentsAsync();
            totalSynced += await SyncStudentRequirementsAsync();
            totalSynced += await SyncEmployeeAddressesAsync();
            totalSynced += await SyncEmployeeEmergencyContactsAsync();
            totalSynced += await SyncSalaryInfosAsync();
            totalSynced += await SyncGradeLevelsAsync();
            totalSynced += await SyncFeesAsync();
            totalSynced += await SyncFeeBreakdownsAsync();
            totalSynced += await SyncExpensesAsync();
            totalSynced += await SyncExpenseAttachmentsAsync();
            totalSynced += await SyncUserStatusLogsAsync();
            totalSynced += await SyncBuildingsAsync();
            totalSynced += await SyncClassroomsAsync();
            totalSynced += await SyncSectionsAsync();
            totalSynced += await SyncSubjectsAsync();
            totalSynced += await SyncSubjectSectionsAsync();
            totalSynced += await SyncSubjectSchedulesAsync();
            totalSynced += await SyncTeacherSectionAssignmentsAsync();
            totalSynced += await SyncClassSchedulesAsync();
            totalSynced += await SyncRolesAsync();
            totalSynced += await SyncDeductionsAsync();

            _logger?.LogInformation($"Sync completed. Total entities synced: {totalSynced}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during sync process");
            return false;
        }
    }

    private async Task<int> SyncGuardiansAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Guardians
            .Where(g => !g.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        int synced = 0;

        try
        {
            await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
            
            // Test cloud connection first
            try
            {
                await cloudContext.Database.CanConnectAsync();
            }
            catch (Exception connEx)
            {
                _logger?.LogError(connEx, "Cannot connect to cloud database. Sync aborted.");
                return 0;
            }

            foreach (var guardian in unsynced)
            {
                try
                {
                    // Check for existing guardian by name and contact combination
                    var existingGuardian = await cloudContext.Guardians
                        .FirstOrDefaultAsync(g => g.FirstName == guardian.FirstName 
                            && g.LastName == guardian.LastName 
                            && g.ContactNum == guardian.ContactNum);

                    if (existingGuardian == null)
                    {
                        // Create new guardian without setting GuardianId - let SQL Server auto-generate it
                        var cloudGuardian = new Guardian
                        {
                            FirstName = guardian.FirstName,
                            MiddleName = guardian.MiddleName,
                            LastName = guardian.LastName,
                            Suffix = guardian.Suffix,
                            ContactNum = guardian.ContactNum,
                            Relationship = guardian.Relationship
                        };

                        await cloudContext.Guardians.AddAsync(cloudGuardian);
                        await cloudContext.SaveChangesAsync();
                    }

                    guardian.IsSynced = true;
                    synced++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error syncing Guardian '{guardian.FirstName} {guardian.LastName}' (Contact: {guardian.ContactNum}): {ex.Message}");
                    // Continue with other entities even if one fails
                }
            }

            if (synced > 0)
            {
                await localContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Fatal error in SyncGuardiansAsync: {ex.Message}");
        }

        return synced;
    }

    private async Task<int> SyncUsersAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Users
            .Where(u => !u.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var user in unsynced)
        {
            try
            {
                // Check for existing user by unique field (SystemId or Email) instead of UserId
                var existingUser = await cloudContext.Users
                    .FirstOrDefaultAsync(u => u.SystemId == user.SystemId || u.Email == user.Email);

                if (existingUser == null)
                {
                    // Create new user without setting UserId - let SQL Server auto-generate it
                    var cloudUser = new UserEntity
                    {
                        SystemId = user.SystemId,
                        FirstName = user.FirstName,
                        MidName = user.MidName,
                        LastName = user.LastName,
                        Suffix = user.Suffix,
                        Birthdate = user.Birthdate,
                        Age = user.Age,
                        Gender = user.Gender,
                        ContactNum = user.ContactNum,
                        UserRole = user.UserRole,
                        Email = user.Email,
                        Password = user.Password,
                        DateHired = user.DateHired,
                        Status = user.Status
                    };

                    await cloudContext.Users.AddAsync(cloudUser);
                    await cloudContext.SaveChangesAsync();
                }

                user.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing User '{user.SystemId}' (Email: {user.Email}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncStudentsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Students
            .Where(s => !s.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var student in unsynced)
        {
            try
            {
                var exists = await cloudContext.Students
                    .AnyAsync(s => s.StudentId == student.StudentId);

                if (!exists)
                {
                    var cloudStudent = new Student
                    {
                        StudentId = student.StudentId,
                        FirstName = student.FirstName,
                        MiddleName = student.MiddleName,
                        LastName = student.LastName,
                        Suffix = student.Suffix,
                        Birthdate = student.Birthdate,
                        Age = student.Age,
                        PlaceOfBirth = student.PlaceOfBirth,
                        Sex = student.Sex,
                        MotherTongue = student.MotherTongue,
                        IpComm = student.IpComm,
                        IpSpecify = student.IpSpecify,
                        FourPs = student.FourPs,
                        FourPsHseId = student.FourPsHseId,
                        HseNo = student.HseNo,
                        Street = student.Street,
                        Brngy = student.Brngy,
                        Province = student.Province,
                        City = student.City,
                        Country = student.Country,
                        ZipCode = student.ZipCode,
                        PhseNo = student.PhseNo,
                        Pstreet = student.Pstreet,
                        Pbrngy = student.Pbrngy,
                        Pprovince = student.Pprovince,
                        Pcity = student.Pcity,
                        Pcountry = student.Pcountry,
                        PzipCode = student.PzipCode,
                        StudentType = student.StudentType,
                        Lrn = student.Lrn,
                        SchoolYr = student.SchoolYr,
                        GradeLevel = student.GradeLevel,
                        GuardianId = student.GuardianId,
                        DateRegistered = student.DateRegistered,
                        Status = student.Status
                    };

                    cloudContext.Students.Add(cloudStudent);
                    await cloudContext.SaveChangesAsync();
                }

                student.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Student {student.StudentId}: {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncStudentRequirementsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.StudentRequirements
            .Where(r => !r.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var req in unsynced)
        {
            try
            {
                // Check for existing requirement by StudentId + RequirementName + RequirementType
                var existingReq = await cloudContext.StudentRequirements
                    .FirstOrDefaultAsync(r => r.StudentId == req.StudentId 
                        && r.RequirementName == req.RequirementName 
                        && r.RequirementType == req.RequirementType);

                if (existingReq == null)
                {
                    // Create new requirement without setting RequirementId - let SQL Server auto-generate it
                    var cloudReq = new StudentRequirement
                    {
                        StudentId = req.StudentId,
                        RequirementName = req.RequirementName,
                        Status = req.Status,
                        RequirementType = req.RequirementType
                    };

                    await cloudContext.StudentRequirements.AddAsync(cloudReq);
                    await cloudContext.SaveChangesAsync();
                }

                req.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing StudentRequirement for StudentId {req.StudentId} ({req.RequirementName}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncEmployeeAddressesAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.EmployeeAddresses
            .Where(a => !a.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var addr in unsynced)
        {
            try
            {
                // Check for existing address by UserId (one address per user)
                var existingAddress = await cloudContext.EmployeeAddresses
                    .FirstOrDefaultAsync(a => a.UserId == addr.UserId);

                if (existingAddress == null)
                {
                    // Create new address without setting AddressId - let SQL Server auto-generate it
                    var cloudAddr = new EmployeeAddress
                    {
                        UserId = addr.UserId,
                        HouseNo = addr.HouseNo,
                        StreetName = addr.StreetName,
                        Province = addr.Province,
                        City = addr.City,
                        Barangay = addr.Barangay,
                        Country = addr.Country,
                        ZipCode = addr.ZipCode
                    };

                    await cloudContext.EmployeeAddresses.AddAsync(cloudAddr);
                    await cloudContext.SaveChangesAsync();
                }

                addr.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing EmployeeAddress for UserId {addr.UserId}: {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncEmployeeEmergencyContactsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.EmployeeEmergencyContacts
            .Where(e => !e.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var contact in unsynced)
        {
            try
            {
                // Check for existing emergency contact by UserId (one contact per user)
                var existingContact = await cloudContext.EmployeeEmergencyContacts
                    .FirstOrDefaultAsync(e => e.UserId == contact.UserId);

                if (existingContact == null)
                {
                    // Create new contact without setting EmergencyId - let SQL Server auto-generate it
                    var cloudContact = new EmployeeEmergencyContact
                    {
                        UserId = contact.UserId,
                        FirstName = contact.FirstName,
                        MiddleName = contact.MiddleName,
                        LastName = contact.LastName,
                        Suffix = contact.Suffix,
                        Relationship = contact.Relationship,
                        ContactNumber = contact.ContactNumber,
                        Address = contact.Address
                    };

                    await cloudContext.EmployeeEmergencyContacts.AddAsync(cloudContact);
                    await cloudContext.SaveChangesAsync();
                }

                contact.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing EmployeeEmergencyContact for UserId {contact.UserId}: {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncSalaryInfosAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.SalaryInfos
            .Where(s => !s.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var salary in unsynced)
        {
            try
            {
                // Check for existing salary by UserId and DateEffective combination
                // (a user can have multiple salary records with different effective dates)
                var existingSalary = await cloudContext.SalaryInfos
                    .FirstOrDefaultAsync(s => s.UserId == salary.UserId && s.DateEffective == salary.DateEffective);

                if (existingSalary == null)
                {
                    // Create new salary without setting SalaryId - let SQL Server auto-generate it
                    var cloudSalary = new SalaryInfo
                    {
                        UserId = salary.UserId,
                        BaseSalary = salary.BaseSalary,
                        Allowance = salary.Allowance,
                        DateEffective = salary.DateEffective,
                        IsActive = salary.IsActive
                    };

                    await cloudContext.SalaryInfos.AddAsync(cloudSalary);
                    await cloudContext.SaveChangesAsync();
                }

                salary.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing SalaryInfo for UserId {salary.UserId} (DateEffective: {salary.DateEffective:yyyy-MM-dd}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncGradeLevelsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.GradeLevels
            .Where(g => !g.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var gradeLevel in unsynced)
        {
            try
            {
                // Check for existing grade level by GradeLevelName (unique field)
                var existingGradeLevel = await cloudContext.GradeLevels
                    .FirstOrDefaultAsync(g => g.GradeLevelName == gradeLevel.GradeLevelName);

                if (existingGradeLevel == null)
                {
                    // Create new grade level without setting GradeLevelId - let SQL Server auto-generate it
                    var cloudGradeLevel = new GradeLevel
                    {
                        GradeLevelName = gradeLevel.GradeLevelName,
                        IsActive = gradeLevel.IsActive
                    };

                    await cloudContext.GradeLevels.AddAsync(cloudGradeLevel);
                    await cloudContext.SaveChangesAsync();
                }

                gradeLevel.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing GradeLevel '{gradeLevel.GradeLevelName}': {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncFeesAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Fees
            .Where(f => !f.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var fee in unsynced)
        {
            try
            {
                // Check for existing fee by GradeLevelId (one fee per grade level)
                var existingFee = await cloudContext.Fees
                    .FirstOrDefaultAsync(f => f.GradeLevelId == fee.GradeLevelId);

                if (existingFee == null)
                {
                    // Create new fee without setting FeeId - let SQL Server auto-generate it
                    var cloudFee = new Fee
                    {
                        GradeLevelId = fee.GradeLevelId,
                        TuitionFee = fee.TuitionFee,
                        MiscFee = fee.MiscFee,
                        OtherFee = fee.OtherFee,
                        CreatedDate = fee.CreatedDate,
                        UpdatedDate = fee.UpdatedDate,
                        CreatedBy = fee.CreatedBy,
                        UpdatedBy = fee.UpdatedBy,
                        IsActive = fee.IsActive
                    };

                    await cloudContext.Fees.AddAsync(cloudFee);
                    await cloudContext.SaveChangesAsync();
                }

                fee.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Fee for GradeLevelId {fee.GradeLevelId}: {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncFeeBreakdownsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.FeeBreakdowns
            .Where(b => !b.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var breakdown in unsynced)
        {
            try
            {
                // Check for existing breakdown by FeeId + BreakdownType + ItemName
                var existingBreakdown = await cloudContext.FeeBreakdowns
                    .FirstOrDefaultAsync(b => b.FeeId == breakdown.FeeId 
                        && b.BreakdownType == breakdown.BreakdownType 
                        && b.ItemName == breakdown.ItemName);

                if (existingBreakdown == null)
                {
                    // Create new breakdown without setting BreakdownId - let SQL Server auto-generate it
                    var cloudBreakdown = new FeeBreakdown
                    {
                        FeeId = breakdown.FeeId,
                        BreakdownType = breakdown.BreakdownType,
                        ItemName = breakdown.ItemName,
                        Amount = breakdown.Amount,
                        DisplayOrder = breakdown.DisplayOrder,
                        CreatedDate = breakdown.CreatedDate,
                        UpdatedDate = breakdown.UpdatedDate
                    };

                    await cloudContext.FeeBreakdowns.AddAsync(cloudBreakdown);
                    await cloudContext.SaveChangesAsync();
                }

                breakdown.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing FeeBreakdown for FeeId {breakdown.FeeId} ({breakdown.ItemName}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncExpensesAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Expenses
            .Where(e => !e.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var expense in unsynced)
        {
            try
            {
                // Check for existing expense by ExpenseCode (unique field)
                var existingExpense = await cloudContext.Expenses
                    .FirstOrDefaultAsync(e => e.ExpenseCode == expense.ExpenseCode);

                if (existingExpense == null)
                {
                    // Create new expense without setting ExpenseId - let SQL Server auto-generate it
                    var cloudExpense = new Expense
                    {
                        ExpenseCode = expense.ExpenseCode,
                        Category = expense.Category,
                        Description = expense.Description,
                        Amount = expense.Amount,
                        ExpenseDate = expense.ExpenseDate,
                        Payee = expense.Payee,
                        OrNumber = expense.OrNumber,
                        PaymentMethod = expense.PaymentMethod,
                        Status = expense.Status,
                        RecordedBy = expense.RecordedBy,
                        ApprovedBy = expense.ApprovedBy,
                        CreatedAt = expense.CreatedAt,
                        UpdatedAt = expense.UpdatedAt
                    };

                    await cloudContext.Expenses.AddAsync(cloudExpense);
                    await cloudContext.SaveChangesAsync();
                }

                expense.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Expense '{expense.ExpenseCode}': {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncExpenseAttachmentsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.ExpenseAttachments
            .Where(a => !a.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var attachment in unsynced)
        {
            try
            {
                // Check for existing attachment by ExpenseId + FileName combination
                var existingAttachment = await cloudContext.ExpenseAttachments
                    .FirstOrDefaultAsync(a => a.ExpenseId == attachment.ExpenseId && a.FileName == attachment.FileName);

                if (existingAttachment == null)
                {
                    // Create new attachment without setting AttachmentId - let SQL Server auto-generate it
                    var cloudAttachment = new ExpenseAttachment
                    {
                        ExpenseId = attachment.ExpenseId,
                        FileName = attachment.FileName,
                        FilePath = attachment.FilePath,
                        UploadedAt = attachment.UploadedAt
                    };

                    await cloudContext.ExpenseAttachments.AddAsync(cloudAttachment);
                    await cloudContext.SaveChangesAsync();
                }

                attachment.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing ExpenseAttachment for ExpenseId {attachment.ExpenseId} (FileName: {attachment.FileName}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncUserStatusLogsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.UserStatusLogs
            .Where(l => !l.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var log in unsynced)
        {
            try
            {
                // Check for existing log by UserId + CreatedAt combination (unique timestamp per user status change)
                var existingLog = await cloudContext.UserStatusLogs
                    .FirstOrDefaultAsync(l => l.UserId == log.UserId && l.CreatedAt == log.CreatedAt);

                if (existingLog == null)
                {
                    // Create new log without setting LogId - let SQL Server auto-generate it
                    var cloudLog = new UserStatusLog
                    {
                        UserId = log.UserId,
                        ChangedBy = log.ChangedBy,
                        OldStatus = log.OldStatus,
                        NewStatus = log.NewStatus,
                        Reason = log.Reason,
                        CreatedAt = log.CreatedAt
                    };

                    await cloudContext.UserStatusLogs.AddAsync(cloudLog);
                    await cloudContext.SaveChangesAsync();
                }

                log.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing UserStatusLog for UserId {log.UserId} (CreatedAt: {log.CreatedAt:yyyy-MM-dd HH:mm:ss}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncBuildingsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Buildings
            .Where(b => !b.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var building in unsynced)
        {
            try
            {
                // Check for existing building by BuildingName (unique field)
                var existingBuilding = await cloudContext.Buildings
                    .FirstOrDefaultAsync(b => b.BuildingName == building.BuildingName);

                if (existingBuilding == null)
                {
                    // Create new building without setting BuildingId - let SQL Server auto-generate it
                    var cloudBuilding = new Building
                    {
                        BuildingName = building.BuildingName,
                        FloorCount = building.FloorCount,
                        Description = building.Description
                    };

                    await cloudContext.Buildings.AddAsync(cloudBuilding);
                    await cloudContext.SaveChangesAsync();
                }

                building.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Building '{building.BuildingName}': {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncClassroomsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Classrooms
            .Where(c => !c.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var classroom in unsynced)
        {
            try
            {
                // Check for existing classroom by RoomName (unique field)
                var existingClassroom = await cloudContext.Classrooms
                    .FirstOrDefaultAsync(c => c.RoomName == classroom.RoomName);

                if (existingClassroom == null)
                {
                    // Create new classroom without setting RoomId - let SQL Server auto-generate it
                    var cloudClassroom = new Classroom
                    {
                        RoomName = classroom.RoomName,
                        BuildingName = classroom.BuildingName,
                        FloorNumber = classroom.FloorNumber,
                        RoomType = classroom.RoomType,
                        Capacity = classroom.Capacity,
                        Status = classroom.Status,
                        Notes = classroom.Notes,
                        CreatedAt = classroom.CreatedAt,
                        UpdatedAt = classroom.UpdatedAt
                    };

                    await cloudContext.Classrooms.AddAsync(cloudClassroom);
                    await cloudContext.SaveChangesAsync();
                }

                classroom.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Classroom '{classroom.RoomName}': {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncSectionsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Sections
            .Where(s => !s.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var section in unsynced)
        {
            try
            {
                // Check for existing section by SectionName + GradeLevelId combination
                var existingSection = await cloudContext.Sections
                    .FirstOrDefaultAsync(s => s.SectionName == section.SectionName && s.GradeLevelId == section.GradeLevelId);

                if (existingSection == null)
                {
                    // Create new section without setting SectionId - let SQL Server auto-generate it
                    var cloudSection = new Section
                    {
                        SectionName = section.SectionName,
                        GradeLevelId = section.GradeLevelId,
                        ClassroomId = section.ClassroomId,
                        Capacity = section.Capacity,
                        Notes = section.Notes,
                        CreatedAt = section.CreatedAt,
                        UpdatedAt = section.UpdatedAt
                    };

                    await cloudContext.Sections.AddAsync(cloudSection);
                    await cloudContext.SaveChangesAsync();
                }

                section.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Section '{section.SectionName}' (GradeLevelId: {section.GradeLevelId}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncSubjectsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Subjects
            .Where(s => !s.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var subject in unsynced)
        {
            try
            {
                // Check for existing subject by SubjectName + GradeLevelId combination
                var existingSubject = await cloudContext.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectName == subject.SubjectName && s.GradeLevelId == subject.GradeLevelId);

                if (existingSubject == null)
                {
                    // Create new subject without setting SubjectId - let SQL Server auto-generate it
                    var cloudSubject = new Subject
                    {
                        GradeLevelId = subject.GradeLevelId,
                        SubjectName = subject.SubjectName,
                        Description = subject.Description,
                        CreatedAt = subject.CreatedAt,
                        UpdatedAt = subject.UpdatedAt
                    };

                    await cloudContext.Subjects.AddAsync(cloudSubject);
                    await cloudContext.SaveChangesAsync();
                }

                subject.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Subject '{subject.SubjectName}' (GradeLevelId: {subject.GradeLevelId}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncSubjectSectionsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.SubjectSections
            .Where(ss => !ss.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var subjectSection in unsynced)
        {
            try
            {
                // Check for existing subject-section by SectionId + SubjectId combination
                var existingSubjectSection = await cloudContext.SubjectSections
                    .FirstOrDefaultAsync(ss => ss.SectionId == subjectSection.SectionId && ss.SubjectId == subjectSection.SubjectId);

                if (existingSubjectSection == null)
                {
                    // Create new subject-section without setting Id - let SQL Server auto-generate it
                    var cloudSubjectSection = new SubjectSection
                    {
                        SectionId = subjectSection.SectionId,
                        SubjectId = subjectSection.SubjectId
                    };

                    await cloudContext.SubjectSections.AddAsync(cloudSubjectSection);
                    await cloudContext.SaveChangesAsync();
                }

                subjectSection.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing SubjectSection (SectionId: {subjectSection.SectionId}, SubjectId: {subjectSection.SubjectId}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncSubjectSchedulesAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.SubjectSchedules
            .Where(ss => !ss.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var schedule in unsynced)
        {
            try
            {
                // Check for existing schedule by SubjectId + GradeLevelId + DayOfWeek + StartTime combination
                var existingSchedule = await cloudContext.SubjectSchedules
                    .FirstOrDefaultAsync(ss => ss.SubjectId == schedule.SubjectId 
                        && ss.GradeLevelId == schedule.GradeLevelId 
                        && ss.DayOfWeek == schedule.DayOfWeek 
                        && ss.StartTime == schedule.StartTime);

                if (existingSchedule == null)
                {
                    // Create new schedule without setting ScheduleId - let SQL Server auto-generate it
                    var cloudSchedule = new SubjectSchedule
                    {
                        SubjectId = schedule.SubjectId,
                        GradeLevelId = schedule.GradeLevelId,
                        DayOfWeek = schedule.DayOfWeek,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        IsDefault = schedule.IsDefault,
                        CreatedAt = schedule.CreatedAt,
                        UpdatedAt = schedule.UpdatedAt
                    };

                    await cloudContext.SubjectSchedules.AddAsync(cloudSchedule);
                    await cloudContext.SaveChangesAsync();
                }

                schedule.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing SubjectSchedule (SubjectId: {schedule.SubjectId}, Day: {schedule.DayOfWeek}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncTeacherSectionAssignmentsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.TeacherSectionAssignments
            .Where(a => !a.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var assignment in unsynced)
        {
            try
            {
                // Check for existing assignment by TeacherId + SectionId + SubjectId + Role combination
                var existingAssignment = await cloudContext.TeacherSectionAssignments
                    .FirstOrDefaultAsync(a => a.TeacherId == assignment.TeacherId 
                        && a.SectionId == assignment.SectionId 
                        && a.SubjectId == assignment.SubjectId 
                        && a.Role == assignment.Role);

                if (existingAssignment == null)
                {
                    // Create new assignment without setting AssignmentId - let SQL Server auto-generate it
                    var cloudAssignment = new TeacherSectionAssignment
                    {
                        TeacherId = assignment.TeacherId,
                        SectionId = assignment.SectionId,
                        SubjectId = assignment.SubjectId,
                        Role = assignment.Role,
                        CreatedAt = assignment.CreatedAt,
                        UpdatedAt = assignment.UpdatedAt
                    };

                    await cloudContext.TeacherSectionAssignments.AddAsync(cloudAssignment);
                    await cloudContext.SaveChangesAsync();
                }

                assignment.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing TeacherSectionAssignment (TeacherId: {assignment.TeacherId}, SectionId: {assignment.SectionId}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncClassSchedulesAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.ClassSchedules
            .Where(s => !s.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var schedule in unsynced)
        {
            try
            {
                // Check for existing class schedule by AssignmentId + DayOfWeek + StartTime combination
                var existingSchedule = await cloudContext.ClassSchedules
                    .FirstOrDefaultAsync(s => s.AssignmentId == schedule.AssignmentId 
                        && s.DayOfWeek == schedule.DayOfWeek 
                        && s.StartTime == schedule.StartTime);

                if (existingSchedule == null)
                {
                    // Create new schedule without setting ScheduleId - let SQL Server auto-generate it
                    var cloudSchedule = new ClassSchedule
                    {
                        AssignmentId = schedule.AssignmentId,
                        DayOfWeek = schedule.DayOfWeek,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        RoomId = schedule.RoomId,
                        CreatedAt = schedule.CreatedAt,
                        UpdatedAt = schedule.UpdatedAt
                    };

                    await cloudContext.ClassSchedules.AddAsync(cloudSchedule);
                    await cloudContext.SaveChangesAsync();
                }

                schedule.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing ClassSchedule (AssignmentId: {schedule.AssignmentId}, Day: {schedule.DayOfWeek}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncRolesAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Roles
            .Where(r => !r.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var role in unsynced)
        {
            try
            {
                // Check for existing role by unique field (RoleName) instead of ID
                var existingRole = await cloudContext.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == role.RoleName);

                if (existingRole == null)
                {
                    // Create new role without setting RoleId - let SQL Server auto-generate it
                    var cloudRole = new Role
                    {
                        RoleName = role.RoleName,
                        BaseSalary = role.BaseSalary,
                        Allowance = role.Allowance,
                        IsActive = role.IsActive,
                        CreatedDate = role.CreatedDate,
                        UpdatedDate = role.UpdatedDate
                    };

                    await cloudContext.Roles.AddAsync(cloudRole);
                    await cloudContext.SaveChangesAsync();
                }

                role.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Role '{role.RoleName}' (ID: {role.RoleId}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }

    private async Task<int> SyncDeductionsAsync()
    {
        await using var localContext = await _localContextFactory.CreateDbContextAsync();
        var unsynced = await localContext.Deductions
            .Where(d => !d.IsSynced)
            .Take(BatchSize)
            .ToListAsync();

        if (unsynced.Count == 0) return 0;

        await using var cloudContext = await _cloudContextFactory.CreateDbContextAsync();
        int synced = 0;

        foreach (var deduction in unsynced)
        {
            try
            {
                // Check for existing deduction by unique field (DeductionType) instead of ID
                var existingDeduction = await cloudContext.Deductions
                    .FirstOrDefaultAsync(d => d.DeductionType == deduction.DeductionType);

                if (existingDeduction == null)
                {
                    // Create new deduction without setting DeductionId - let SQL Server auto-generate it
                    var cloudDeduction = new Deduction
                    {
                        DeductionType = deduction.DeductionType,
                        DeductionName = deduction.DeductionName,
                        RateOrValue = deduction.RateOrValue,
                        IsPercentage = deduction.IsPercentage,
                        MaxAmount = deduction.MaxAmount,
                        MinAmount = deduction.MinAmount,
                        Description = deduction.Description,
                        IsActive = deduction.IsActive,
                        CreatedDate = deduction.CreatedDate,
                        UpdatedDate = deduction.UpdatedDate
                    };

                    await cloudContext.Deductions.AddAsync(cloudDeduction);
                    await cloudContext.SaveChangesAsync();
                }

                deduction.IsSynced = true;
                synced++;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error syncing Deduction '{deduction.DeductionType}' (ID: {deduction.DeductionId}): {ex.Message}");
            }
        }

        if (synced > 0)
        {
            await localContext.SaveChangesAsync();
        }

        return synced;
    }
}
