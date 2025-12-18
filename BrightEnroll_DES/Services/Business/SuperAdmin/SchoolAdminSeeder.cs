using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class SchoolAdminSeeder
{
    private readonly ILogger<SchoolAdminSeeder>? _logger;

    public SchoolAdminSeeder(ILogger<SchoolAdminSeeder>? logger = null)
    {
        _logger = logger;
    }

    // Creates an admin user account in the school's database based on customer information
    public async Task<SchoolAdminInfo> SeedAdminAccountAsync(
        string connectionString,
        Customer customer,
        string defaultPassword = "Admin123456",
        string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string? suffix = null,
        string? role = null)
    {
        try
        {
            _logger?.LogInformation($"Seeding admin account for school: {customer.SchoolName}");

            // Use provided names or parse from ContactPerson
            string adminFirstName;
            string? adminMiddleName = null;
            string adminLastName;
            string? adminSuffix = null;
            
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                // Use provided separate name fields
                adminFirstName = firstName;
                adminMiddleName = middleName;
                adminLastName = lastName;
                adminSuffix = suffix;
            }
            else
            {
                // Fallback: Parse contact person name
                var (parsedFirstName, parsedLastName) = ParseContactPersonName(customer.ContactPerson);
                adminFirstName = parsedFirstName;
                adminLastName = parsedLastName;
            }
            
            // Use provided role or default to "Admin"
            string adminRole = !string.IsNullOrWhiteSpace(role) ? role : "Admin";
            
            // Generate system ID for the admin
            var systemId = await GenerateSystemIdAsync(connectionString, customer.CustomerCode);
            
            // Generate email if not provided
            var email = string.IsNullOrWhiteSpace(customer.ContactEmail) 
                ? GenerateEmail(customer.SchoolName, adminFirstName, adminLastName)
                : customer.ContactEmail;

            // Check if email already exists
            email = await EnsureUniqueEmailAsync(connectionString, email);

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

            // Calculate age from birthdate (default to 30 years old if not provided)
            var birthdate = new DateTime(DateTime.Now.Year - 30, 1, 1);
            var age = (byte)(DateTime.Today.Year - birthdate.Year);
            if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

            // Create admin user with full personal information
            var adminUser = new UserEntity
            {
                SystemId = systemId,
                FirstName = adminFirstName,
                MidName = adminMiddleName,
                LastName = adminLastName,
                Suffix = adminSuffix,
                Birthdate = birthdate,
                Age = age,
                Gender = "male", // Default, can be updated later
                ContactNum = customer.ContactPhone ?? "00000000000",
                UserRole = adminRole, // Use provided role
                Email = email,
                Password = hashedPassword,
                DateHired = DateTime.Now,
                Status = "active"
            };

            // Insert into school's database
            await InsertAdminUserAsync(connectionString, adminUser);

            _logger?.LogInformation($"Admin account created successfully. SystemID: {systemId}, Email: {email}");

            return new SchoolAdminInfo
            {
                SystemId = systemId,
                Email = email,
                Password = defaultPassword,
                FirstName = adminFirstName,
                LastName = adminLastName,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error seeding admin account for school '{customer.SchoolName}': {ex.Message}");
            return new SchoolAdminInfo
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    // Parses contact person name into first and last name
    private (string firstName, string lastName) ParseContactPersonName(string? contactPerson)
    {
        if (string.IsNullOrWhiteSpace(contactPerson))
        {
            return ("Admin", "User");
        }

        var parts = contactPerson.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 1)
        {
            return (parts[0], "Admin");
        }
        
        var firstName = parts[0];
        var lastName = string.Join(" ", parts.Skip(1));
        
        return (firstName, lastName);
    }

    // Generates a system ID for the admin user based on customer code
    private async Task<string> GenerateSystemIdAsync(string connectionString, string customerCode)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Extract numeric part from customer code (e.g., "CUST-001" -> "001")
            var codeNumber = "0001";
            if (!string.IsNullOrWhiteSpace(customerCode))
            {
                var parts = customerCode.Split('-');
                if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int num))
                {
                    codeNumber = num.ToString("D4");
                }
            }

            // Check existing system IDs in the school's database
            var checkQuery = @"
                SELECT TOP 1 system_ID 
                FROM tbl_Users 
                WHERE system_ID LIKE @Pattern 
                ORDER BY CAST(SUBSTRING(system_ID, 6, LEN(system_ID)) AS INT) DESC";

            var prefix = $"{customerCode}-";
            var pattern = $"{prefix}%";

            using var command = new SqlCommand(checkQuery, connection);
            command.Parameters.AddWithValue("@Pattern", pattern);

            var lastId = await command.ExecuteScalarAsync() as string;
            
            if (!string.IsNullOrEmpty(lastId))
            {
                // Extract number and increment
                var lastNumber = lastId.Replace(prefix, "");
                if (int.TryParse(lastNumber, out int lastNum))
                {
                    codeNumber = (lastNum + 1).ToString("D4");
                }
            }

            return $"{prefix}{codeNumber}";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, $"Error generating system ID, using default: {ex.Message}");
            return $"{customerCode}-0001";
        }
    }

    // Generates an email address if not provided
    private string GenerateEmail(string schoolName, string firstName, string lastName)
    {
        var cleanSchoolName = System.Text.RegularExpressions.Regex.Replace(schoolName, @"[^a-zA-Z0-9]", "").ToLower();
        var cleanFirstName = System.Text.RegularExpressions.Regex.Replace(firstName, @"[^a-zA-Z0-9]", "").ToLower();
        var cleanLastName = System.Text.RegularExpressions.Regex.Replace(lastName, @"[^a-zA-Z0-9]", "").ToLower();
        
        return $"{cleanFirstName}.{cleanLastName}@{cleanSchoolName}.edu.ph";
    }

    // Ensures email is unique by appending number if needed
    private async Task<string> EnsureUniqueEmailAsync(string connectionString, string email)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var baseEmail = email;
            var counter = 1;
            var uniqueEmail = email;

            while (true)
            {
                var checkQuery = "SELECT COUNT(*) FROM tbl_Users WHERE email = @Email";
                using var command = new SqlCommand(checkQuery, connection);
                command.Parameters.AddWithValue("@Email", uniqueEmail);
                
                var result = await command.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;
                
                if (count == 0)
                {
                    break; // Email is unique
                }

                // Email exists, try with number suffix
                var emailParts = baseEmail.Split('@');
                uniqueEmail = $"{emailParts[0]}{counter}@{emailParts[1]}";
                counter++;
                
                if (counter > 1000) // Safety limit
                {
                    throw new Exception("Unable to generate unique email after multiple attempts");
                }
            }

            return uniqueEmail;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, $"Error checking email uniqueness: {ex.Message}");
            return email; // Return original email if check fails
        }
    }

    // Inserts admin user into the school's database
    private async Task InsertAdminUserAsync(string connectionString, UserEntity adminUser)
    {
        try
        {
            // First, ensure the tbl_Users table exists in the school's database
            await EnsureUsersTableExistsAsync(connectionString);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var insertQuery = @"
                INSERT INTO tbl_Users (
                    system_ID, first_name, mid_name, last_name, suffix,
                    birthdate, age, gender, contact_num, user_role,
                    email, password, date_hired, status
                )
                VALUES (
                    @SystemId, @FirstName, @MidName, @LastName, @Suffix,
                    @Birthdate, @Age, @Gender, @ContactNum, @UserRole,
                    @Email, @Password, @DateHired, @Status
                )";

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@SystemId", adminUser.SystemId);
            command.Parameters.AddWithValue("@FirstName", adminUser.FirstName);
            command.Parameters.AddWithValue("@MidName", (object?)adminUser.MidName ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastName", adminUser.LastName);
            command.Parameters.AddWithValue("@Suffix", (object?)adminUser.Suffix ?? DBNull.Value);
            command.Parameters.AddWithValue("@Birthdate", adminUser.Birthdate);
            command.Parameters.AddWithValue("@Age", adminUser.Age);
            command.Parameters.AddWithValue("@Gender", adminUser.Gender);
            command.Parameters.AddWithValue("@ContactNum", adminUser.ContactNum);
            command.Parameters.AddWithValue("@UserRole", adminUser.UserRole);
            command.Parameters.AddWithValue("@Email", adminUser.Email);
            command.Parameters.AddWithValue("@Password", adminUser.Password);
            command.Parameters.AddWithValue("@DateHired", adminUser.DateHired);
            command.Parameters.AddWithValue("@Status", adminUser.Status);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error inserting admin user: {ex.Message}");
            throw;
        }
    }

    // Ensures the tbl_Users table exists in the school's database
    private async Task EnsureUsersTableExistsAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var createTableQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Users' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [dbo].[tbl_Users](
                        [user_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [system_ID] VARCHAR(50) NOT NULL UNIQUE,
                        [first_name] VARCHAR(50) NOT NULL,
                        [mid_name] VARCHAR(50) NULL,
                        [last_name] VARCHAR(50) NOT NULL,
                        [suffix] VARCHAR(10) NULL,
                        [birthdate] DATE NOT NULL,
                        [age] TINYINT NOT NULL,
                        [gender] VARCHAR(20) NOT NULL,
                        [contact_num] VARCHAR(20) NOT NULL,
                        [user_role] VARCHAR(50) NOT NULL,
                        [email] VARCHAR(150) NOT NULL UNIQUE,
                        [password] VARCHAR(255) NOT NULL,
                        [date_hired] DATETIME NOT NULL DEFAULT GETDATE(),
                        [status] VARCHAR(20) NOT NULL DEFAULT 'active'
                    );
                    CREATE UNIQUE INDEX IX_tbl_Users_system_ID ON tbl_Users(system_ID);
                    CREATE UNIQUE INDEX IX_tbl_Users_email ON tbl_Users(email);
                END";

            using var command = new SqlCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error ensuring tbl_Users table exists: {ex.Message}");
            throw;
        }
    }
}

public class SchoolAdminInfo
{
    public string? SystemId { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

