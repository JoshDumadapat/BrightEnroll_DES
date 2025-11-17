using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services.AuthFunction
{
    public class DatabaseSeeder
    {
        private readonly IUserRepository _userRepository;

        public DatabaseSeeder(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task SeedInitialAdminAsync()
        {
            try
            {
                // Check if admin user already exists using repository (handles parameterization)
                var exists = await _userRepository.ExistsBySystemIdAsync("BDES-0001");
                if (exists)
                {
                    // Admin already exists, skip seeding
                    return;
                }

                // Hash the password using BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123456");

                // Calculate age from birthdate (assuming birthdate is January 1, 2000)
                DateTime birthdate = new DateTime(2000, 1, 1);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                // Create admin user entity
                var adminUser = new User
                {
                    system_ID = "BDES-0001",
                    first_name = "Josh",
                    mid_name = null,
                    last_name = "Vanderson",
                    suffix = null,
                    birthdate = birthdate,
                    age = age,
                    gender = "male",
                    contact_num = "09366669571",
                    user_role = "Admin",
                    email = "joshvanderson01@gmail.com",
                    password = hashedPassword,
                    date_hired = DateTime.Now
                };

                // Insert using repository (handles validation and parameterization)
                await _userRepository.InsertAsync(adminUser);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding initial admin: {ex.Message}", ex);
            }
        }
    }
}

