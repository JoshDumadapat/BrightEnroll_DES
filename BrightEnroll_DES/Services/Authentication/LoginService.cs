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
            System.Diagnostics.Debug.WriteLine($"[LoginService] ===== ValidateUserCredentialsAsync START =====");
            Console.WriteLine($"[LoginService] ===== ValidateUserCredentialsAsync START =====");
            Console.WriteLine($"[LoginService] emailOrSystemId: {emailOrSystemId}");
            Console.WriteLine($"[LoginService] password length: {password?.Length ?? 0}");
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LoginService] Validating credentials for: {emailOrSystemId}");
                Console.WriteLine($"[LoginService] Validating credentials for: {emailOrSystemId}");
                
                // Get user by email or system ID (repository handles validation and parameterization)
                System.Diagnostics.Debug.WriteLine($"[LoginService] Calling GetByEmailOrSystemIdAsync...");
                Console.WriteLine($"[LoginService] Calling GetByEmailOrSystemIdAsync...");
                var user = await _userRepository.GetByEmailOrSystemIdAsync(emailOrSystemId);
                System.Diagnostics.Debug.WriteLine($"[LoginService] GetByEmailOrSystemIdAsync returned: {user != null}");
                Console.WriteLine($"[LoginService] GetByEmailOrSystemIdAsync returned: {user != null}");

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginService] User not found: {emailOrSystemId}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[LoginService] User found: {user.system_ID}, Role: {user.user_role}");
                Console.WriteLine($"[LoginService] User found: {user.system_ID}, Role: {user.user_role}");

                // Verify password using BCrypt
                System.Diagnostics.Debug.WriteLine($"[LoginService] Verifying password...");
                Console.WriteLine($"[LoginService] Verifying password...");
                Console.WriteLine($"[LoginService] Password hash length: {user.password?.Length ?? 0}");
                Console.WriteLine($"[LoginService] Password hash starts with: {user.password?.Substring(0, Math.Min(10, user.password?.Length ?? 0)) ?? "null"}");
                
                bool passwordValid = false;
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginService] About to start BCrypt verification in Task.Run...");
                    Console.WriteLine($"[LoginService] About to start BCrypt verification in Task.Run...");
                    
                    // Check if password hash is valid format BEFORE starting Task.Run
                    if (string.IsNullOrWhiteSpace(user.password))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoginService] Password hash is null or empty!");
                        Console.WriteLine($"[LoginService] Password hash is null or empty!");
                        return null;
                    }
                    
                    if (!user.password.StartsWith("$2"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoginService] Password hash doesn't start with $2 - invalid BCrypt format! Hash: {user.password.Substring(0, Math.Min(20, user.password.Length))}...");
                        Console.WriteLine($"[LoginService] Password hash doesn't start with $2 - invalid BCrypt format! Hash: {user.password.Substring(0, Math.Min(20, user.password.Length))}...");
                        return null;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[LoginService] Password hash format looks valid. Hash length: {user.password.Length}");
                    Console.WriteLine($"[LoginService] Password hash format looks valid. Hash length: {user.password.Length}");
                    Console.WriteLine($"[LoginService] Password hash preview: {user.password.Substring(0, Math.Min(30, user.password.Length))}...");
                    
                    // Use CancellationTokenSource with timeout to prevent hanging
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    
                    // Use Task.Run with ConfigureAwait(false) to avoid deadlocks
                    var verifyTask = Task.Run(() => 
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoginService] Inside Task.Run - Starting BCrypt.Verify...");
                        Console.WriteLine($"[LoginService] Inside Task.Run - Starting BCrypt.Verify...");
                        Console.WriteLine($"[LoginService] Password to verify length: {password?.Length ?? 0}");
                        
                        try
                        {
                            // Check cancellation token
                            cts.Token.ThrowIfCancellationRequested();
                            
                            System.Diagnostics.Debug.WriteLine($"[LoginService] Calling BCrypt.Verify...");
                            Console.WriteLine($"[LoginService] Calling BCrypt.Verify...");
                            
                            var result = BCrypt.Net.BCrypt.Verify(password, user.password);
                            
                            System.Diagnostics.Debug.WriteLine($"[LoginService] BCrypt.Verify completed: {result}");
                            Console.WriteLine($"[LoginService] BCrypt.Verify completed: {result}");
                            return result;
                        }
                        catch (OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoginService] BCrypt verification was cancelled (timeout)!");
                            Console.WriteLine($"[LoginService] BCrypt verification was cancelled (timeout)!");
                            throw;
                        }
                        catch (Exception bcryptEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoginService] BCrypt exception: {bcryptEx.Message}");
                            Console.WriteLine($"[LoginService] BCrypt exception: {bcryptEx.Message}");
                            Console.WriteLine($"[LoginService] BCrypt exception type: {bcryptEx.GetType().Name}");
                            Console.WriteLine($"[LoginService] BCrypt stack trace: {bcryptEx.StackTrace}");
                            if (bcryptEx.InnerException != null)
                            {
                                Console.WriteLine($"[LoginService] BCrypt inner exception: {bcryptEx.InnerException.Message}");
                            }
                            throw;
                        }
                    }, cts.Token);
                    
                    System.Diagnostics.Debug.WriteLine($"[LoginService] Waiting for BCrypt verification task (10s timeout)...");
                    Console.WriteLine($"[LoginService] Waiting for BCrypt verification task (10s timeout)...");
                    
                    passwordValid = await verifyTask.ConfigureAwait(false);
                    
                    System.Diagnostics.Debug.WriteLine($"[LoginService] Task completed, result: {passwordValid}");
                    Console.WriteLine($"[LoginService] Task completed, result: {passwordValid}");
                }
                catch (Exception verifyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginService] Password verification exception: {verifyEx.Message}");
                    Console.WriteLine($"[LoginService] Password verification exception: {verifyEx.Message}");
                    Console.WriteLine($"[LoginService] Exception type: {verifyEx.GetType().Name}");
                    if (verifyEx.InnerException != null)
                    {
                        Console.WriteLine($"[LoginService] Inner exception: {verifyEx.InnerException.Message}");
                    }
                    return null;
                }
                
                System.Diagnostics.Debug.WriteLine($"[LoginService] Password verification result: {passwordValid}");
                Console.WriteLine($"[LoginService] Password verification result: {passwordValid}");

                if (passwordValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginService] Login successful for: {emailOrSystemId}");
                    return user;
                }

                System.Diagnostics.Debug.WriteLine($"[LoginService] Invalid password for: {emailOrSystemId}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginService] ===== EXCEPTION CAUGHT =====");
                Console.WriteLine($"[LoginService] ===== EXCEPTION CAUGHT =====");
                System.Diagnostics.Debug.WriteLine($"[LoginService] Exception during validation: {ex.Message}");
                Console.WriteLine($"[LoginService] Exception during validation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginService] Exception type: {ex.GetType().FullName}");
                Console.WriteLine($"[LoginService] Exception type: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"[LoginService] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[LoginService] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[LoginService] Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"[LoginService] Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw new Exception($"Error validating user credentials: {ex.Message}", ex);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"[LoginService] ===== ValidateUserCredentialsAsync END =====");
                Console.WriteLine($"[LoginService] ===== ValidateUserCredentialsAsync END =====");
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

