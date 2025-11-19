using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BrightEnroll_DES.Services.Repositories
{
    // Repository interface for user operations - uses EF Core ORM for security
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetBySystemIdAsync(string systemId);
        Task<User?> GetByEmailOrSystemIdAsync(string emailOrSystemId);
        Task<IEnumerable<User>> GetAllAsync();
        Task<int> InsertAsync(User user);
        Task<int> UpdateAsync(User user);
        Task<int> DeleteAsync(int userId);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsBySystemIdAsync(string systemId);
        Task<bool> ExistsByIdAsync(int userId);
        Task<string> GetNextSystemIdAsync();
    }

    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserRepository>? _logger;

        public UserRepository(AppDbContext context, ILogger<UserRepository>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        // Gets user by ID using EF Core ORM
        public async Task<User?> GetByIdAsync(int userId)
        {
            try
            {
                var userEntity = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                return userEntity != null ? MapEntityToUser(userEntity) : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user by ID {UserId}: {Message}", userId, ex.Message);
                throw new Exception($"Failed to get user by ID: {ex.Message}", ex);
            }
        }

        // Gets user by email using EF Core ORM
        public async Task<User?> GetByEmailAsync(string email)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }

            try
            {
                var sanitizedEmail = SanitizeString(email, 150);
                var userEntity = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == sanitizedEmail);

                return userEntity != null ? MapEntityToUser(userEntity) : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user by email {Email}: {Message}", email, ex.Message);
                throw new Exception($"Failed to get user by email: {ex.Message}", ex);
            }
        }

        // Gets user by system ID using EF Core ORM
        public async Task<User?> GetBySystemIdAsync(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("System ID cannot be null or empty", nameof(systemId));
            }

            try
            {
                var sanitizedSystemId = SanitizeString(systemId, 50);
                var userEntity = await _context.Users
                    .FirstOrDefaultAsync(u => u.SystemId == sanitizedSystemId);

                return userEntity != null ? MapEntityToUser(userEntity) : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user by system ID {SystemId}: {Message}", systemId, ex.Message);
                throw new Exception($"Failed to get user by system ID: {ex.Message}", ex);
            }
        }

        // Gets user by email or system ID (for login) using EF Core ORM
        public async Task<User?> GetByEmailOrSystemIdAsync(string emailOrSystemId)
        {
            if (string.IsNullOrWhiteSpace(emailOrSystemId))
            {
                throw new ArgumentException("Email or System ID cannot be null or empty", nameof(emailOrSystemId));
            }

            try
            {
                var sanitizedInput = SanitizeString(emailOrSystemId, 150);
                var userEntity = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == sanitizedInput || u.SystemId == sanitizedInput);

                return userEntity != null ? MapEntityToUser(userEntity) : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user by email or system ID: {Message}", ex.Message);
                throw new Exception($"Failed to get user: {ex.Message}", ex);
            }
        }

        // Gets all users using EF Core ORM
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                var userEntities = await _context.Users
                    .OrderBy(u => u.UserId)
                    .ToListAsync();

                return userEntities.Select(MapEntityToUser).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all users: {Message}", ex.Message);
                throw new Exception($"Failed to get all users: {ex.Message}", ex);
            }
        }

        // Creates a new user using EF Core ORM
        public async Task<int> InsertAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ValidateUser(user);

            try
            {
                var userEntity = new UserEntity
                {
                    SystemId = SanitizeString(user.system_ID, 50),
                    FirstName = SanitizeString(user.first_name, 50),
                    MidName = string.IsNullOrWhiteSpace(user.mid_name) ? null : SanitizeString(user.mid_name, 50),
                    LastName = SanitizeString(user.last_name, 50),
                    Suffix = string.IsNullOrWhiteSpace(user.suffix) ? null : SanitizeString(user.suffix, 10),
                    Birthdate = user.birthdate,
                    Age = user.age,
                    Gender = SanitizeString(user.gender, 20),
                    ContactNum = SanitizeString(user.contact_num, 20),
                    UserRole = SanitizeString(user.user_role, 50),
                    Email = SanitizeString(user.email, 150),
                    Password = user.password, // Password is already hashed, no sanitization needed
                    DateHired = user.date_hired,
                    Status = string.IsNullOrWhiteSpace(user.status) ? "active" : SanitizeString(user.status, 20)
                };

                _context.Users.Add(userEntity);
                await _context.SaveChangesAsync();

                return userEntity.UserId; // Return the generated ID
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inserting user: {Message}", ex.Message);
                throw new Exception($"Failed to insert user: {ex.Message}", ex);
            }
        }

        // Updates an existing user using EF Core ORM
        public async Task<int> UpdateAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ValidateUser(user);

            try
            {
                var userEntity = await _context.Users.FindAsync(user.user_ID);
                if (userEntity == null)
                {
                    throw new Exception($"User with ID {user.user_ID} not found");
                }

                // Store original password to check if it changed
                var originalPassword = userEntity.Password;
                var newPassword = user.password; // Password is already hashed

                // Update properties with sanitization
                userEntity.SystemId = SanitizeString(user.system_ID, 50);
                userEntity.FirstName = SanitizeString(user.first_name, 50);
                userEntity.MidName = string.IsNullOrWhiteSpace(user.mid_name) ? null : SanitizeString(user.mid_name, 50);
                userEntity.LastName = SanitizeString(user.last_name, 50);
                userEntity.Suffix = string.IsNullOrWhiteSpace(user.suffix) ? null : SanitizeString(user.suffix, 10);
                userEntity.Birthdate = user.birthdate;
                userEntity.Age = user.age;
                userEntity.Gender = SanitizeString(user.gender, 20);
                userEntity.ContactNum = SanitizeString(user.contact_num, 20);
                userEntity.UserRole = SanitizeString(user.user_role, 50);
                userEntity.Email = SanitizeString(user.email, 150);
                userEntity.Password = newPassword; // Password is already hashed
                userEntity.DateHired = user.date_hired;
                userEntity.Status = string.IsNullOrWhiteSpace(user.status) ? "active" : SanitizeString(user.status, 20);

                // Explicitly mark Password as modified to ensure EF Core saves it
                // This is important because EF Core might not detect the change if the hash looks similar
                _context.Entry(userEntity).Property(u => u.Password).IsModified = true;

                var rowsAffected = await _context.SaveChangesAsync();
                
                _logger?.LogInformation("User {UserId} updated successfully. Password changed: {PasswordChanged}", 
                    user.user_ID, originalPassword != newPassword);
                
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user {UserId}: {Message}", user.user_ID, ex.Message);
                throw new Exception($"Failed to update user: {ex.Message}", ex);
            }
        }

        // Deletes a user by ID using EF Core ORM
        public async Task<int> DeleteAsync(int userId)
        {
            try
            {
                var userEntity = await _context.Users.FindAsync(userId);
                if (userEntity == null)
                {
                    return 0; // User not found, return 0 rows affected
                }

                _context.Users.Remove(userEntity);
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user {UserId}: {Message}", userId, ex.Message);
                throw new Exception($"Failed to delete user: {ex.Message}", ex);
            }
        }

        // Checks if user exists by email using EF Core ORM
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            if (!IsValidEmail(email))
            {
                return false;
            }

            try
            {
                var sanitizedEmail = SanitizeString(email, 150);
                return await _context.Users
                    .AnyAsync(u => u.Email == sanitizedEmail);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user exists by email: {Message}", ex.Message);
                return false;
            }
        }

        // Checks if user exists by system ID using EF Core ORM
        public async Task<bool> ExistsBySystemIdAsync(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                return false;
            }

            try
            {
                var sanitizedSystemId = SanitizeString(systemId, 50);
                return await _context.Users
                    .AnyAsync(u => u.SystemId == sanitizedSystemId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user exists by system ID: {Message}", ex.Message);
                return false;
            }
        }

        // Checks if user exists by ID using EF Core ORM
        public async Task<bool> ExistsByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user exists by ID: {Message}", ex.Message);
                return false;
            }
        }

        // Generates the next system ID in sequence (BDES-0001, BDES-0002, etc.) using EF Core ORM
        // Finds the highest existing number and adds 1
        // This method checks the database first to ensure no duplicate system IDs
        public async Task<string> GetNextSystemIdAsync()
        {
            try
            {
                // Get all system IDs that match BDES- pattern
                var bdesIds = await _context.Users
                    .Where(u => u.SystemId.StartsWith("BDES-") && u.SystemId.Length >= 10)
                    .Select(u => u.SystemId)
                    .ToListAsync();

                int maxNumber = 0;

                // Extract numbers from BDES- IDs
                foreach (var id in bdesIds)
                {
                    if (id.Length > 5)
                    {
                        var numberPart = id.Substring(5); // Get part after "BDES-"
                        if (int.TryParse(numberPart, out int number))
                        {
                            if (number > maxNumber)
                            {
                                maxNumber = number;
                            }
                        }
                    }
                }

                int nextNumber = maxNumber + 1;

                // Format with 4 digits: 1 becomes BDES-0001, 2 becomes BDES-0002
                string nextSystemId = $"BDES-{nextNumber:D4}";

                // Double-check to ensure the generated ID doesn't already exist (safety check)
                // This prevents race conditions where another process might have inserted the same ID
                bool exists = await ExistsBySystemIdAsync(nextSystemId);
                int retryCount = 0;
                const int maxRetries = 100; // Prevent infinite loop

                while (exists && retryCount < maxRetries)
                {
                    nextNumber++;
                    nextSystemId = $"BDES-{nextNumber:D4}";
                    exists = await ExistsBySystemIdAsync(nextSystemId);
                    retryCount++;
                }

                if (retryCount >= maxRetries)
                {
                    throw new Exception("Unable to generate a unique system ID after multiple attempts. Please check the database.");
                }

                return nextSystemId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating next system ID: {Message}", ex.Message);
                throw new Exception($"Failed to generate system ID: {ex.Message}", ex);
            }
        }

        // Validates user data before saving
        private void ValidateUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.first_name))
                throw new ArgumentException("First name is required", nameof(user));

            if (string.IsNullOrWhiteSpace(user.last_name))
                throw new ArgumentException("Last name is required", nameof(user));

            if (string.IsNullOrWhiteSpace(user.email))
                throw new ArgumentException("Email is required", nameof(user));

            if (!IsValidEmail(user.email))
                throw new ArgumentException("Invalid email format", nameof(user));

            if (string.IsNullOrWhiteSpace(user.system_ID))
                throw new ArgumentException("System ID is required", nameof(user));

            if (string.IsNullOrWhiteSpace(user.user_role))
                throw new ArgumentException("User role is required", nameof(user));

            if (string.IsNullOrWhiteSpace(user.password))
                throw new ArgumentException("Password is required", nameof(user));
        }

        // Converts UserEntity to User model
        private User MapEntityToUser(UserEntity entity)
        {
            return new User
            {
                user_ID = entity.UserId,
                system_ID = entity.SystemId,
                first_name = entity.FirstName,
                mid_name = entity.MidName,
                last_name = entity.LastName,
                suffix = entity.Suffix,
                birthdate = entity.Birthdate,
                age = entity.Age,
                gender = entity.Gender,
                contact_num = entity.ContactNum,
                user_role = entity.UserRole,
                email = entity.Email,
                password = entity.Password,
                date_hired = entity.DateHired,
                status = entity.Status
            };
        }

        // Trims and limits string length (security sanitization)
        private string SanitizeString(string? input, int maxLength = 255)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sanitized = input.Trim();

            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
            }

            return sanitized;
        }

        // Checks if email format is valid
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

