using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    /// <summary>
    /// Repository for User entity operations
    /// Implements ORM-like patterns with SQL injection protection
    /// </summary>
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
    }

    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        /// <summary>
        /// Retrieves a user by their primary key (user_ID)
        /// Uses parameterized query to prevent SQL injection
        /// </summary>
        public async Task<User?> GetByIdAsync(int userId)
        {
            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired]
                FROM [dbo].[tbl_Users] 
                WHERE [user_ID] = @UserId";

            var parameters = new[]
            {
                CreateParameter("@UserId", userId, SqlDbType.Int)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToUser(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves a user by email address
        /// Email is validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }

            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired]
                FROM [dbo].[tbl_Users] 
                WHERE [email] = @Email";

            var parameters = new[]
            {
                CreateParameter("@Email", SanitizeString(email, 150), SqlDbType.VarChar)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToUser(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves a user by system ID
        /// System ID is sanitized and parameterized to prevent SQL injection
        /// </summary>
        public async Task<User?> GetBySystemIdAsync(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("System ID cannot be null or empty", nameof(systemId));
            }

            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired]
                FROM [dbo].[tbl_Users] 
                WHERE [system_ID] = @SystemId";

            var parameters = new[]
            {
                CreateParameter("@SystemId", SanitizeString(systemId, 50), SqlDbType.VarChar)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToUser(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves a user by email or system ID
        /// Both inputs are validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<User?> GetByEmailOrSystemIdAsync(string emailOrSystemId)
        {
            if (string.IsNullOrWhiteSpace(emailOrSystemId))
            {
                throw new ArgumentException("Email or System ID cannot be null or empty", nameof(emailOrSystemId));
            }

            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired]
                FROM [dbo].[tbl_Users] 
                WHERE [email] = @EmailOrSystemId OR [system_ID] = @EmailOrSystemId";

            var sanitizedInput = SanitizeString(emailOrSystemId, 150);
            var parameters = new[]
            {
                CreateParameter("@EmailOrSystemId", sanitizedInput, SqlDbType.VarChar)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToUser(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves all users from the database
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired]
                FROM [dbo].[tbl_Users] 
                ORDER BY [user_ID]";

            var dataTable = await ExecuteQueryAsync(query);

            var users = new List<User>();
            foreach (DataRow row in dataTable.Rows)
            {
                users.Add(MapDataRowToUser(row));
            }

            return users;
        }

        /// <summary>
        /// Inserts a new user into the database
        /// All fields are validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<int> InsertAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ValidateUser(user);

            const string query = @"
                INSERT INTO [dbo].[tbl_Users] 
                    ([system_ID], [first_name], [mid_name], [last_name], [suffix], 
                     [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired])
                VALUES 
                    (@SystemId, @FirstName, @MidName, @LastName, @Suffix, 
                     @Birthdate, @Age, @Gender, @ContactNum, @UserRole, @Email, @Password, @DateHired)";

            var parameters = new[]
            {
                CreateParameter("@SystemId", SanitizeString(user.system_ID, 50), SqlDbType.VarChar),
                CreateParameter("@FirstName", SanitizeString(user.first_name, 50), SqlDbType.VarChar),
                CreateParameter("@MidName", string.IsNullOrWhiteSpace(user.mid_name) ? DBNull.Value : SanitizeString(user.mid_name, 50), SqlDbType.VarChar),
                CreateParameter("@LastName", SanitizeString(user.last_name, 50), SqlDbType.VarChar),
                CreateParameter("@Suffix", string.IsNullOrWhiteSpace(user.suffix) ? DBNull.Value : SanitizeString(user.suffix, 10), SqlDbType.VarChar),
                CreateParameter("@Birthdate", user.birthdate, SqlDbType.Date),
                CreateParameter("@Age", user.age, SqlDbType.TinyInt),
                CreateParameter("@Gender", SanitizeString(user.gender, 20), SqlDbType.VarChar),
                CreateParameter("@ContactNum", SanitizeString(user.contact_num, 20), SqlDbType.VarChar),
                CreateParameter("@UserRole", SanitizeString(user.user_role, 50), SqlDbType.VarChar),
                CreateParameter("@Email", SanitizeString(user.email, 150), SqlDbType.VarChar),
                CreateParameter("@Password", user.password, SqlDbType.VarChar), // Already hashed, no sanitization needed
                CreateParameter("@DateHired", user.date_hired, SqlDbType.DateTime)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }

        /// <summary>
        /// Updates an existing user in the database
        /// All fields are validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<int> UpdateAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ValidateUser(user);

            const string query = @"
                UPDATE [dbo].[tbl_Users] 
                SET [system_ID] = @SystemId,
                    [first_name] = @FirstName,
                    [mid_name] = @MidName,
                    [last_name] = @LastName,
                    [suffix] = @Suffix,
                    [birthdate] = @Birthdate,
                    [age] = @Age,
                    [gender] = @Gender,
                    [contact_num] = @ContactNum,
                    [user_role] = @UserRole,
                    [email] = @Email,
                    [password] = @Password,
                    [date_hired] = @DateHired
                WHERE [user_ID] = @UserId";

            var parameters = new[]
            {
                CreateParameter("@UserId", user.user_ID, SqlDbType.Int),
                CreateParameter("@SystemId", SanitizeString(user.system_ID, 50), SqlDbType.VarChar),
                CreateParameter("@FirstName", SanitizeString(user.first_name, 50), SqlDbType.VarChar),
                CreateParameter("@MidName", string.IsNullOrWhiteSpace(user.mid_name) ? DBNull.Value : SanitizeString(user.mid_name, 50), SqlDbType.VarChar),
                CreateParameter("@LastName", SanitizeString(user.last_name, 50), SqlDbType.VarChar),
                CreateParameter("@Suffix", string.IsNullOrWhiteSpace(user.suffix) ? DBNull.Value : SanitizeString(user.suffix, 10), SqlDbType.VarChar),
                CreateParameter("@Birthdate", user.birthdate, SqlDbType.Date),
                CreateParameter("@Age", user.age, SqlDbType.TinyInt),
                CreateParameter("@Gender", SanitizeString(user.gender, 20), SqlDbType.VarChar),
                CreateParameter("@ContactNum", SanitizeString(user.contact_num, 20), SqlDbType.VarChar),
                CreateParameter("@UserRole", SanitizeString(user.user_role, 50), SqlDbType.VarChar),
                CreateParameter("@Email", SanitizeString(user.email, 150), SqlDbType.VarChar),
                CreateParameter("@Password", user.password, SqlDbType.VarChar),
                CreateParameter("@DateHired", user.date_hired, SqlDbType.DateTime)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }

        /// <summary>
        /// Deletes a user by their primary key
        /// Uses parameterized query to prevent SQL injection
        /// </summary>
        public async Task<int> DeleteAsync(int userId)
        {
            const string query = @"
                DELETE FROM [dbo].[tbl_Users] 
                WHERE [user_ID] = @UserId";

            var parameters = new[]
            {
                CreateParameter("@UserId", userId, SqlDbType.Int)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }

        /// <summary>
        /// Checks if a user exists by email
        /// Email is validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            if (!IsValidEmail(email))
            {
                return false;
            }

            const string query = "SELECT COUNT(*) FROM [dbo].[tbl_Users] WHERE [email] = @Email";
            var parameters = new[]
            {
                CreateParameter("@Email", SanitizeString(email, 150), SqlDbType.VarChar)
            };

            var result = await ExecuteScalarAsync(query, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Checks if a user exists by system ID
        /// System ID is sanitized and parameterized to prevent SQL injection
        /// </summary>
        public async Task<bool> ExistsBySystemIdAsync(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                return false;
            }

            const string query = "SELECT COUNT(*) FROM [dbo].[tbl_Users] WHERE [system_ID] = @SystemId";
            var parameters = new[]
            {
                CreateParameter("@SystemId", SanitizeString(systemId, 50), SqlDbType.VarChar)
            };

            var result = await ExecuteScalarAsync(query, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Checks if a user exists by primary key
        /// Uses parameterized query to prevent SQL injection
        /// </summary>
        public async Task<bool> ExistsByIdAsync(int userId)
        {
            const string query = "SELECT COUNT(*) FROM [dbo].[tbl_Users] WHERE [user_ID] = @UserId";
            var parameters = new[]
            {
                CreateParameter("@UserId", userId, SqlDbType.Int)
            };

            var result = await ExecuteScalarAsync(query, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Validates user entity before database operations
        /// </summary>
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

        /// <summary>
        /// Maps a DataRow to a User entity
        /// </summary>
        private User MapDataRowToUser(DataRow row)
        {
            return new User
            {
                user_ID = Convert.ToInt32(row["user_ID"]),
                system_ID = row["system_ID"].ToString() ?? string.Empty,
                first_name = row["first_name"].ToString() ?? string.Empty,
                mid_name = row["mid_name"] == DBNull.Value ? null : row["mid_name"].ToString(),
                last_name = row["last_name"].ToString() ?? string.Empty,
                suffix = row["suffix"] == DBNull.Value ? null : row["suffix"].ToString(),
                birthdate = Convert.ToDateTime(row["birthdate"]),
                age = Convert.ToByte(row["age"]),
                gender = row["gender"].ToString() ?? string.Empty,
                contact_num = row["contact_num"].ToString() ?? string.Empty,
                user_role = row["user_role"].ToString() ?? string.Empty,
                email = row["email"].ToString() ?? string.Empty,
                password = row["password"].ToString() ?? string.Empty,
                date_hired = Convert.ToDateTime(row["date_hired"])
            };
        }
    }
}

