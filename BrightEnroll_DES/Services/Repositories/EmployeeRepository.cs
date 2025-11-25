using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    /// <summary>
    /// Repository for Employee entity operations
    /// Implements ORM-like patterns with SQL injection protection
    /// </summary>
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(int employeeId);
        Task<Employee?> GetByEmailAsync(string email);
        Task<Employee?> GetBySystemIdAsync(string systemId);
        Task<IEnumerable<Employee>> GetAllAsync();
        Task<IEnumerable<Employee>> GetByStatusAsync(string status);
        Task<IEnumerable<Employee>> GetByRoleAsync(string role);
        Task<IEnumerable<Employee>> SearchAsync(string searchTerm);
        Task<int> InsertAsync(Employee employee);
        Task<int> UpdateAsync(Employee employee);
        Task<int> DeleteAsync(int employeeId);
        Task<int> UpdateStatusAsync(int employeeId, string status, string? inactiveReason = null);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsBySystemIdAsync(string systemId);
        Task<bool> ExistsByIdAsync(int employeeId);
    }

    public class EmployeeRepository : BaseRepository, IEmployeeRepository
    {
        public EmployeeRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        /// <summary>
        /// Retrieves an employee by their primary key (user_ID)
        /// Uses parameterized query to prevent SQL injection
        /// </summary>
        public async Task<Employee?> GetByIdAsync(int employeeId)
        {
            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [email], [user_role], [date_hired], [password]
                FROM [dbo].[tbl_Users] 
                WHERE [user_ID] = @EmployeeId";

            var parameters = new[]
            {
                CreateParameter("@EmployeeId", employeeId, SqlDbType.Int)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToEmployee(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves an employee by email address
        /// Email is validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<Employee?> GetByEmailAsync(string email)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }

            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [email], [user_role], [date_hired], [password]
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

            return MapDataRowToEmployee(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves an employee by system ID
        /// System ID is sanitized and parameterized to prevent SQL injection
        /// </summary>
        public async Task<Employee?> GetBySystemIdAsync(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("System ID cannot be null or empty", nameof(systemId));
            }

            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [email], [user_role], [date_hired], [password]
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

            return MapDataRowToEmployee(dataTable.Rows[0]);
        }

        /// <summary>
        /// Retrieves all employees from the database
        /// </summary>
        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            try
            {
                const string query = @"
                    SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                           [birthdate], [age], [gender], [contact_num], [email], [user_role], [date_hired], [password]
                    FROM [dbo].[tbl_Users] 
                    ORDER BY [last_name], [first_name]";

                var dataTable = await ExecuteQueryAsync(query);

                var employees = new List<Employee>();
                foreach (DataRow row in dataTable.Rows)
                {
                    employees.Add(MapDataRowToEmployee(row));
                }

                System.Diagnostics.Debug.WriteLine($"GetAllAsync: Found {employees.Count} employees in database");
                return employees;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves employees by status
        /// Note: tbl_Users doesn't have status field, so this returns all employees
        /// </summary>
        public async Task<IEnumerable<Employee>> GetByStatusAsync(string status)
        {
            // Since tbl_Users doesn't have a status field, we return all employees
            // You might want to filter by role or another field instead
            return await GetAllAsync();
        }

        /// <summary>
        /// Retrieves employees by role
        /// </summary>
        public async Task<IEnumerable<Employee>> GetByRoleAsync(string role)
        {
            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [email], [user_role], [date_hired], [password]
                FROM [dbo].[tbl_Users] 
                WHERE [user_role] = @Role
                ORDER BY [last_name], [first_name]";

            var parameters = new[]
            {
                CreateParameter("@Role", SanitizeString(role, 50), SqlDbType.VarChar)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            var employees = new List<Employee>();
            foreach (DataRow row in dataTable.Rows)
            {
                employees.Add(MapDataRowToEmployee(row));
            }

            return employees;
        }

        /// <summary>
        /// Searches employees by name, email, or system ID
        /// </summary>
        public async Task<IEnumerable<Employee>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync();
            }

            var sanitizedSearch = SanitizeString(searchTerm, 150);
            var searchPattern = $"%{sanitizedSearch}%";

            const string query = @"
                SELECT [user_ID], [system_ID], [first_name], [mid_name], [last_name], [suffix], 
                       [birthdate], [age], [gender], [contact_num], [email], [user_role], [date_hired], [password]
                FROM [dbo].[tbl_Users] 
                WHERE [first_name] LIKE @SearchTerm 
                   OR [last_name] LIKE @SearchTerm 
                   OR [email] LIKE @SearchTerm 
                   OR [system_ID] LIKE @SearchTerm
                ORDER BY [last_name], [first_name]";

            var parameters = new[]
            {
                CreateParameter("@SearchTerm", searchPattern, SqlDbType.VarChar)
            };

            var dataTable = await ExecuteQueryAsync(query, parameters);

            var employees = new List<Employee>();
            foreach (DataRow row in dataTable.Rows)
            {
                employees.Add(MapDataRowToEmployee(row));
            }

            return employees;
        }

        /// <summary>
        /// Inserts a new employee into the database
        /// All fields are validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<int> InsertAsync(Employee employee)
        {
            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }

            ValidateEmployee(employee);

            try
            {
                const string query = @"
                    INSERT INTO [dbo].[tbl_Users] 
                        ([system_ID], [first_name], [mid_name], [last_name], [suffix], 
                         [birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired])
                    VALUES 
                        (@SystemId, @FirstName, @MidName, @LastName, @Suffix, 
                         @Birthdate, @Age, @Gender, @ContactNum, @UserRole, @Email, @Password, @DateHired)";

                // Note: tbl_Users requires a password field. Hash the password using BCrypt.
                var plainPassword = string.IsNullOrWhiteSpace(employee.password) ? "DefaultPassword123!" : employee.password;
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                
                var parameters = new[]
                {
                    CreateParameter("@SystemId", SanitizeString(employee.system_ID, 50), SqlDbType.VarChar),
                    CreateParameter("@FirstName", SanitizeString(employee.first_name, 50), SqlDbType.VarChar),
                    CreateParameter("@MidName", string.IsNullOrWhiteSpace(employee.mid_name) ? DBNull.Value : SanitizeString(employee.mid_name, 50), SqlDbType.VarChar),
                    CreateParameter("@LastName", SanitizeString(employee.last_name, 50), SqlDbType.VarChar),
                    CreateParameter("@Suffix", string.IsNullOrWhiteSpace(employee.suffix) ? DBNull.Value : SanitizeString(employee.suffix, 10), SqlDbType.VarChar),
                    CreateParameter("@Birthdate", employee.birthdate, SqlDbType.Date),
                    CreateParameter("@Age", employee.age, SqlDbType.TinyInt),
                    CreateParameter("@Gender", SanitizeString(employee.gender, 20), SqlDbType.VarChar),
                    CreateParameter("@ContactNum", SanitizeString(employee.contact_num, 20), SqlDbType.VarChar),
                    CreateParameter("@UserRole", SanitizeString(employee.role, 50), SqlDbType.VarChar),
                    CreateParameter("@Email", SanitizeString(employee.email, 150), SqlDbType.VarChar),
                    CreateParameter("@Password", hashedPassword, SqlDbType.VarChar),
                    CreateParameter("@DateHired", employee.date_hired, SqlDbType.DateTime)
                };

                var result = await ExecuteNonQueryAsync(query, parameters);
                System.Diagnostics.Debug.WriteLine($"InsertAsync: Inserted employee with result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InsertAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Updates an existing employee in the database
        /// All fields are validated and parameterized to prevent SQL injection
        /// </summary>
        public async Task<int> UpdateAsync(Employee employee)
        {
            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }

            ValidateEmployee(employee);

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
                    [date_hired] = @DateHired
                WHERE [user_ID] = @EmployeeId";

            var parameters = new[]
            {
                CreateParameter("@EmployeeId", employee.employee_ID, SqlDbType.Int),
                CreateParameter("@SystemId", SanitizeString(employee.system_ID, 50), SqlDbType.VarChar),
                CreateParameter("@FirstName", SanitizeString(employee.first_name, 50), SqlDbType.VarChar),
                CreateParameter("@MidName", string.IsNullOrWhiteSpace(employee.mid_name) ? DBNull.Value : SanitizeString(employee.mid_name, 50), SqlDbType.VarChar),
                CreateParameter("@LastName", SanitizeString(employee.last_name, 50), SqlDbType.VarChar),
                CreateParameter("@Suffix", string.IsNullOrWhiteSpace(employee.suffix) ? DBNull.Value : SanitizeString(employee.suffix, 10), SqlDbType.VarChar),
                CreateParameter("@Birthdate", employee.birthdate, SqlDbType.Date),
                CreateParameter("@Age", employee.age, SqlDbType.TinyInt),
                CreateParameter("@Gender", SanitizeString(employee.gender, 20), SqlDbType.VarChar),
                CreateParameter("@ContactNum", SanitizeString(employee.contact_num, 20), SqlDbType.VarChar),
                CreateParameter("@Email", SanitizeString(employee.email, 150), SqlDbType.VarChar),
                CreateParameter("@Role", SanitizeString(employee.role, 50), SqlDbType.VarChar),
                CreateParameter("@Status", SanitizeString(employee.status, 20), SqlDbType.VarChar),
                CreateParameter("@InactiveReason", string.IsNullOrWhiteSpace(employee.inactive_reason) ? DBNull.Value : employee.inactive_reason, SqlDbType.VarChar),
                CreateParameter("@DateHired", employee.date_hired, SqlDbType.DateTime),
                CreateParameter("@HouseNo", string.IsNullOrWhiteSpace(employee.house_no) ? DBNull.Value : SanitizeString(employee.house_no, 50), SqlDbType.VarChar),
                CreateParameter("@StreetName", string.IsNullOrWhiteSpace(employee.street_name) ? DBNull.Value : SanitizeString(employee.street_name, 100), SqlDbType.VarChar),
                CreateParameter("@Barangay", string.IsNullOrWhiteSpace(employee.barangay) ? DBNull.Value : SanitizeString(employee.barangay, 100), SqlDbType.VarChar),
                CreateParameter("@City", string.IsNullOrWhiteSpace(employee.city) ? DBNull.Value : SanitizeString(employee.city, 100), SqlDbType.VarChar),
                CreateParameter("@Province", string.IsNullOrWhiteSpace(employee.province) ? DBNull.Value : SanitizeString(employee.province, 100), SqlDbType.VarChar),
                CreateParameter("@Country", string.IsNullOrWhiteSpace(employee.country) ? DBNull.Value : SanitizeString(employee.country, 100), SqlDbType.VarChar),
                CreateParameter("@ZipCode", string.IsNullOrWhiteSpace(employee.zip_code) ? DBNull.Value : SanitizeString(employee.zip_code, 10), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactFirstName", string.IsNullOrWhiteSpace(employee.emergency_contact_first_name) ? DBNull.Value : SanitizeString(employee.emergency_contact_first_name, 50), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactMidName", string.IsNullOrWhiteSpace(employee.emergency_contact_mid_name) ? DBNull.Value : SanitizeString(employee.emergency_contact_mid_name, 50), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactLastName", string.IsNullOrWhiteSpace(employee.emergency_contact_last_name) ? DBNull.Value : SanitizeString(employee.emergency_contact_last_name, 50), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactSuffix", string.IsNullOrWhiteSpace(employee.emergency_contact_suffix) ? DBNull.Value : SanitizeString(employee.emergency_contact_suffix, 10), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactRelationship", string.IsNullOrWhiteSpace(employee.emergency_contact_relationship) ? DBNull.Value : SanitizeString(employee.emergency_contact_relationship, 50), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactNumber", string.IsNullOrWhiteSpace(employee.emergency_contact_number) ? DBNull.Value : SanitizeString(employee.emergency_contact_number, 20), SqlDbType.VarChar),
                CreateParameter("@EmergencyContactAddress", string.IsNullOrWhiteSpace(employee.emergency_contact_address) ? DBNull.Value : SanitizeString(employee.emergency_contact_address, 500), SqlDbType.VarChar),
                CreateDecimalParameter("@BaseSalary", employee.base_salary),
                CreateDecimalParameter("@Allowance", employee.allowance),
                CreateDecimalParameter("@Bonus", employee.bonus),
                CreateDecimalParameter("@Deductions", employee.deductions),
                CreateDecimalParameter("@TotalSalary", employee.total_salary)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }

        /// <summary>
        /// Updates employee status and inactive reason
        /// Note: tbl_Users doesn't have status field, so this method is kept for compatibility but returns 0
        /// </summary>
        public async Task<int> UpdateStatusAsync(int employeeId, string status, string? inactiveReason = null)
        {
            // Since tbl_Users doesn't have status field, we can't update it
            // This method is kept for compatibility but returns 0
            // In the future, you might want to add a status field to tbl_Users or use a different approach
            await Task.CompletedTask; // Suppress async warning
            return 0;
        }

        /// <summary>
        /// Deletes an employee by their primary key
        /// Uses parameterized query to prevent SQL injection
        /// </summary>
        public async Task<int> DeleteAsync(int employeeId)
        {
            const string query = @"
                DELETE FROM [dbo].[tbl_Users] 
                WHERE [user_ID] = @EmployeeId";

            var parameters = new[]
            {
                CreateParameter("@EmployeeId", employeeId, SqlDbType.Int)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }

        /// <summary>
        /// Checks if an employee exists by email
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
        /// Checks if an employee exists by system ID
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
        /// Checks if an employee exists by primary key
        /// Uses parameterized query to prevent SQL injection
        /// </summary>
        public async Task<bool> ExistsByIdAsync(int employeeId)
        {
            const string query = "SELECT COUNT(*) FROM [dbo].[tbl_Users] WHERE [user_ID] = @EmployeeId";
            var parameters = new[]
            {
                CreateParameter("@EmployeeId", employeeId, SqlDbType.Int)
            };

            var result = await ExecuteScalarAsync(query, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Creates a decimal parameter with proper precision and scale
        /// </summary>
        private SqlParameter CreateDecimalParameter(string parameterName, decimal? value)
        {
            var parameter = new SqlParameter(parameterName, value.HasValue ? (object)value.Value : DBNull.Value)
            {
                SqlDbType = SqlDbType.Decimal,
                Precision = 18,
                Scale = 2
            };
            return parameter;
        }

        /// <summary>
        /// Validates employee entity before database operations
        /// </summary>
        private void ValidateEmployee(Employee employee)
        {
            if (string.IsNullOrWhiteSpace(employee.first_name))
                throw new ArgumentException("First name is required", nameof(employee));

            if (string.IsNullOrWhiteSpace(employee.last_name))
                throw new ArgumentException("Last name is required", nameof(employee));

            if (string.IsNullOrWhiteSpace(employee.email))
                throw new ArgumentException("Email is required", nameof(employee));

            if (!IsValidEmail(employee.email))
                throw new ArgumentException("Invalid email format", nameof(employee));

            if (string.IsNullOrWhiteSpace(employee.system_ID))
                throw new ArgumentException("System ID is required", nameof(employee));

            if (string.IsNullOrWhiteSpace(employee.role))
                throw new ArgumentException("Role is required", nameof(employee));
        }

        /// <summary>
        /// Maps a DataRow to an Employee entity
        /// Maps user_ID to employee_ID and user_role to role
        /// </summary>
        private Employee MapDataRowToEmployee(DataRow row)
        {
            return new Employee
            {
                employee_ID = Convert.ToInt32(row["user_ID"]), // Map user_ID to employee_ID
                system_ID = row["system_ID"].ToString() ?? string.Empty,
                first_name = row["first_name"].ToString() ?? string.Empty,
                mid_name = row["mid_name"] == DBNull.Value ? null : row["mid_name"].ToString(),
                last_name = row["last_name"].ToString() ?? string.Empty,
                suffix = row["suffix"] == DBNull.Value ? null : row["suffix"].ToString(),
                birthdate = Convert.ToDateTime(row["birthdate"]),
                age = Convert.ToByte(row["age"]),
                gender = row["gender"].ToString() ?? string.Empty,
                contact_num = row["contact_num"].ToString() ?? string.Empty,
                email = row["email"].ToString() ?? string.Empty,
                role = row["user_role"].ToString() ?? string.Empty, // Map user_role to role
                password = row["password"] == DBNull.Value ? string.Empty : row["password"].ToString() ?? string.Empty,
                status = "Active", // Default since tbl_Users doesn't have status field
                inactive_reason = null, // Not available in tbl_Users
                date_hired = Convert.ToDateTime(row["date_hired"]),
                // Address fields - not in tbl_Users, set to null
                house_no = null,
                street_name = null,
                barangay = null,
                city = null,
                province = null,
                country = null,
                zip_code = null,
                // Emergency Contact fields - not in tbl_Users, set to null
                emergency_contact_first_name = null,
                emergency_contact_mid_name = null,
                emergency_contact_last_name = null,
                emergency_contact_suffix = null,
                emergency_contact_relationship = null,
                emergency_contact_number = null,
                emergency_contact_address = null,
                // Salary fields - not in tbl_Users, set to null
                base_salary = null,
                allowance = null,
                bonus = null,
                deductions = null,
                total_salary = null
            };
        }
    }
}

