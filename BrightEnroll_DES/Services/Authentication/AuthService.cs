using BrightEnroll_DES.Models;

namespace BrightEnroll_DES.Services.Authentication
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        bool IsAuthenticated { get; }
        User? CurrentUser { get; }
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private bool _isAuthenticated = false;
        private User? _currentUser = null;
        private readonly ILoginService _loginService;

        public AuthService(ILoginService loginService)
        {
            _loginService = loginService;
        }

        public bool IsAuthenticated => _isAuthenticated;
        public User? CurrentUser => _currentUser;

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] LoginAsync called for: {username}");
                Console.WriteLine($"[AuthService] LoginAsync called for: {username}");
                
                // Validate credentials against database
                System.Diagnostics.Debug.WriteLine($"[AuthService] Calling ValidateUserCredentialsAsync...");
                Console.WriteLine($"[AuthService] Calling ValidateUserCredentialsAsync...");
                var user = await _loginService.ValidateUserCredentialsAsync(username, password);
                System.Diagnostics.Debug.WriteLine($"[AuthService] ValidateUserCredentialsAsync returned: {user != null}");
                Console.WriteLine($"[AuthService] ValidateUserCredentialsAsync returned: {user != null}");

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AuthService] Login successful for: {username}, Role: {user.user_role}, SystemId: {user.system_ID}");
                    _currentUser = user;
                    _isAuthenticated = true;
                    System.Diagnostics.Debug.WriteLine($"[AuthService] Auth state set - IsAuthenticated: {_isAuthenticated}, CurrentUser: {_currentUser != null}");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"[AuthService] Login failed for: {username} - User not found or invalid password");
                _isAuthenticated = false;
                _currentUser = null;
                return false;
            }
            catch (Exception ex)
            {
                // Log error for debugging
                System.Diagnostics.Debug.WriteLine($"[AuthService] Login exception for {username}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AuthService] Inner exception: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[AuthService] Stack trace: {ex.StackTrace}");
                _isAuthenticated = false;
                _currentUser = null;
                return false;
            }
        }

        public void Logout()
        {
            _isAuthenticated = false;
            _currentUser = null;
        }
    }
}

