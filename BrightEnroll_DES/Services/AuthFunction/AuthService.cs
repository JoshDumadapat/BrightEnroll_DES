namespace BrightEnroll_DES.Services.AuthFunction
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        bool IsAuthenticated { get; }
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private bool _isAuthenticated = false;

        public bool IsAuthenticated => _isAuthenticated;

        public Task<bool> LoginAsync(string username, string password)
        {
            // Temporary if/else logic - no database connection yet
            if (username == "Admin1" && password == "pass123")
            {
                _isAuthenticated = true;
                return Task.FromResult(true);
            }

            _isAuthenticated = false;
            return Task.FromResult(false);
        }

        public void Logout()
        {
            _isAuthenticated = false;
        }
    }
}
