using System.Data;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    /// <summary>
    /// Base repository implementation providing common database operations
    /// with SQL injection protection through parameterized queries
    /// </summary>
    public abstract class BaseRepository
    {
        protected readonly DBConnection _dbConnection;

        protected BaseRepository(DBConnection dbConnection)
        {
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        }

        /// <summary>
        /// Executes a SELECT query and returns a DataTable
        /// All parameters are automatically sanitized to prevent SQL injection
        /// </summary>
        protected async Task<DataTable> ExecuteQueryAsync(string query, params SqlParameter[] parameters)
        {
            ValidateQuery(query);
            return await _dbConnection.ExecuteQueryAsync(query, parameters);
        }

        /// <summary>
        /// Executes INSERT, UPDATE, DELETE queries
        /// All parameters are automatically sanitized to prevent SQL injection
        /// </summary>
        protected async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
        {
            ValidateQuery(query);
            return await _dbConnection.ExecuteNonQueryAsync(query, parameters);
        }

        /// <summary>
        /// Executes a query that returns a single scalar value
        /// All parameters are automatically sanitized to prevent SQL injection
        /// </summary>
        protected async Task<object?> ExecuteScalarAsync(string query, params SqlParameter[] parameters)
        {
            ValidateQuery(query);
            return await _dbConnection.ExecuteScalarAsync(query, parameters);
        }

        /// <summary>
        /// Creates a SqlParameter with proper type and value handling
        /// This ensures all user input is properly parameterized
        /// </summary>
        protected SqlParameter CreateParameter(string parameterName, object? value, SqlDbType? dbType = null)
        {
            var parameter = new SqlParameter(parameterName, value ?? DBNull.Value);
            
            if (dbType.HasValue)
            {
                parameter.SqlDbType = dbType.Value;
            }
            
            return parameter;
        }

        /// <summary>
        /// Validates SQL query to prevent common injection patterns
        /// This is an additional security layer on top of parameterization
        /// </summary>
        private void ValidateQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be null or empty", nameof(query));
            }

            // Additional validation: Check for dangerous patterns
            var dangerousPatterns = new[]
            {
                "; DROP",
                "; DELETE",
                "; TRUNCATE",
                "; EXEC",
                "; EXECUTE",
                "UNION SELECT",
                "--",
                "/*",
                "xp_",
                "sp_"
            };

            var upperQuery = query.ToUpperInvariant();
            foreach (var pattern in dangerousPatterns)
            {
                if (upperQuery.Contains(pattern))
                {
                    throw new ArgumentException($"Query contains potentially dangerous pattern: {pattern}", nameof(query));
                }
            }
        }

        /// <summary>
        /// Sanitizes string input by trimming and limiting length
        /// </summary>
        protected string SanitizeString(string? input, int maxLength = 255)
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

        /// <summary>
        /// Validates email format
        /// </summary>
        protected bool IsValidEmail(string email)
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

