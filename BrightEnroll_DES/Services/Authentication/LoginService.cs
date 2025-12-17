using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Authentication
{
    public interface ILoginService
    {
        Task<User?> ValidateUserCredentialsAsync(string emailOrSystemId, string password);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserBySystemIdAsync(string systemId);
        Task<bool> UserExistsAsync(string email);
        Task<User?> ValidateSuperAdminFromCloudAsync(string email, string password);
        Task<User?> ValidateSuperAdminFromSuperAdminDatabaseAsync(string email, string password);
        Task<Customer?> GetCustomerByAdminEmailAsync(string email);
    }

    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration? _configuration;
        private readonly SuperAdminDbContext? _superAdminContext;

        public LoginService(
            IUserRepository userRepository,
            IConfiguration? configuration = null,
            SuperAdminDbContext? superAdminContext = null)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _configuration = configuration;
            _superAdminContext = superAdminContext;
        }

        public async Task<User?> ValidateUserCredentialsAsync(string emailOrSystemId, string password)
        {
            try
            {
                // Get user by email or system ID (repository handles validation and parameterization)
                var user = await _userRepository.GetByEmailOrSystemIdAsync(emailOrSystemId);

                if (user == null)
                {
                    return null;
                }

                // Verify password using BCrypt
                if (BCrypt.Net.BCrypt.Verify(password, user.password))
                {
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error validating user credentials: {ex.Message}", ex);
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _userRepository.GetByEmailAsync(email);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting user by email: {ex.Message}", ex);
            }
        }

        public async Task<User?> GetUserBySystemIdAsync(string systemId)
        {
            try
            {
                return await _userRepository.GetBySystemIdAsync(systemId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting user by system ID: {ex.Message}", ex);
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                return await _userRepository.ExistsByEmailAsync(email);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if user exists: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates SuperAdmin credentials from cloud database (for security - SuperAdmin should NOT be in local DB)
        /// </summary>
        public async Task<User?> ValidateSuperAdminFromCloudAsync(string email, string password)
        {
            try
            {
                if (_configuration == null) return null;
                
                var cloudConnectionString = _configuration.GetConnectionString("CloudConnection");
                if (string.IsNullOrWhiteSpace(cloudConnectionString))
                    return null;

                using var connection = new SqlConnection(cloudConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT user_ID, system_ID, first_name, last_name, email, 
                           password, user_role, status
                    FROM tbl_Users 
                    WHERE email = @Email 
                    AND user_role IN ('SuperAdmin', 'Super Admin')";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var hashedPassword = reader["password"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(hashedPassword) && 
                        BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                    {
                        return new User
                        {
                            user_ID = (int)reader["user_ID"],
                            system_ID = reader["system_ID"]?.ToString() ?? "",
                            first_name = reader["first_name"]?.ToString() ?? "",
                            last_name = reader["last_name"]?.ToString() ?? "",
                            email = reader["email"]?.ToString() ?? "",
                            user_role = reader["user_role"]?.ToString() ?? "",
                            password = hashedPassword
                        };
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating SuperAdmin from cloud: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets customer information by admin email (to retrieve school-specific connection strings)
        /// </summary>
        public async Task<Customer?> GetCustomerByAdminEmailAsync(string email)
        {
            try
            {
                if (_superAdminContext == null) return null;
                
                if (string.IsNullOrWhiteSpace(email)) return null;
                
                // Case-insensitive email comparison (SQL Server default collation is case-insensitive, but using ToLower for safety)
                var emailLower = email.Trim().ToLowerInvariant();
                return await _superAdminContext.Customers
                    .FirstOrDefaultAsync(c => c.AdminUsername != null && 
                                             c.AdminUsername.Trim().ToLower() == emailLower);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting customer by admin email {email}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates SuperAdmin credentials from SuperAdmin database (for local development/testing)
        /// </summary>
        public async Task<User?> ValidateSuperAdminFromSuperAdminDatabaseAsync(string email, string password)
        {
            try
            {
                if (_superAdminContext == null) return null;

                var user = await _superAdminContext.Users
                    .FirstOrDefaultAsync(u => u.Email == email && 
                        (u.UserRole == "SuperAdmin" || u.UserRole == "Super Admin"));

                if (user == null)
                {
                    return null;
                }

                // Verify password using BCrypt
                if (BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    return new User
                    {
                        user_ID = user.UserId,
                        system_ID = user.SystemId,
                        first_name = user.FirstName,
                        last_name = user.LastName,
                        email = user.Email,
                        user_role = user.UserRole,
                        password = user.Password
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating SuperAdmin from SuperAdmin database: {ex.Message}");
                return null;
            }
        }
    }
}

