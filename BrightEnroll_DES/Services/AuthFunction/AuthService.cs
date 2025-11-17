using BrightEnroll_DES.Models;

namespace BrightEnroll_DES.Services.AuthFunction
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
                // Validate credentials against database
                var user = await _loginService.ValidateUserCredentialsAsync(username, password);

                if (user != null)
                {
                    _currentUser = user;
                    _isAuthenticated = true;
                    return true;
                }

                _isAuthenticated = false;
                _currentUser = null;
                return false;
            }
            catch (Exception)
            {
                // Log error in production
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
