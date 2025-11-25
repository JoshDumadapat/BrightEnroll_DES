using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;

namespace BrightEnroll_DES.Services.Authentication
{
    public interface ILoginService
    {
        Task<User?> ValidateUserCredentialsAsync(string emailOrSystemId, string password);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserBySystemIdAsync(string systemId);
        Task<bool> UserExistsAsync(string email);
    }

    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;

        public LoginService(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
    }
}

