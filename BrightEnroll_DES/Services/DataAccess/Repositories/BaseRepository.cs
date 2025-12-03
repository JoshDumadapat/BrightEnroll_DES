using System.Data;
using BrightEnroll_DES.Services.Database.Connections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.DataAccess.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly DBConnection _dbConnection;

        protected BaseRepository(DBConnection dbConnection)
        {
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        }

        protected async Task<DataTable> ExecuteQueryAsync(string query, params SqlParameter[] parameters)
        {
            ValidateQuery(query);
            return await _dbConnection.ExecuteQueryAsync(query, parameters);
        }

        protected async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
        {
            ValidateQuery(query);
            return await _dbConnection.ExecuteNonQueryAsync(query, parameters);
        }

        protected async Task<object?> ExecuteScalarAsync(string query, params SqlParameter[] parameters)
        {
            ValidateQuery(query);
            return await _dbConnection.ExecuteScalarAsync(query, parameters);
        }

        protected SqlParameter CreateParameter(string parameterName, object? value, SqlDbType? dbType = null)
        {
            var parameter = new SqlParameter(parameterName, value ?? DBNull.Value);
            
            if (dbType.HasValue)
            {
                parameter.SqlDbType = dbType.Value;
            }
            
            return parameter;
        }

        private void ValidateQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be null or empty", nameof(query));
            }

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

