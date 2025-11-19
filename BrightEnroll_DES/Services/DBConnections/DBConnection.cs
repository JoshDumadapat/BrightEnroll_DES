using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BrightEnroll_DES.Services.DBConnections
{
    public class DBConnection
    {
        private readonly string _connectionString;

        public DBConnection()
        {
            // Try to get connection string from configuration or environment
            _connectionString = GetConnectionString();
        }

        public DBConnection(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Gets connection string from: 1) environment variable, 2) appsettings.json, 3) default LocalDB
        private string GetConnectionString()
        {
            var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(envConnectionString))
            {
                return envConnectionString;
            }

            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                var configuration = builder.Build();
                var configConnectionString = configuration.GetConnectionString("DefaultConnection");
                
                if (!string.IsNullOrWhiteSpace(configConnectionString))
                {
                    return configConnectionString;
                }
            }
            catch
            {
                // appsettings.json not found, use default
            }
            var server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "(localdb)\\MSSQLLocalDB";
            var database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "DB_BrightEnroll_DES";
            var integratedSecurity = Environment.GetEnvironmentVariable("DB_INTEGRATED_SECURITY") ?? "True";

            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = integratedSecurity.Equals("True", StringComparison.OrdinalIgnoreCase),
                TrustServerCertificate = true,
                Encrypt = true,
                PersistSecurityInfo = false,
                Pooling = false,
                MultipleActiveResultSets = false
            };

            return connectionStringBuilder.ConnectionString;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, params SqlParameter[] parameters)
        {
            var dataTable = new DataTable();
            
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                
                using var command = new SqlCommand(query, connection);
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }
                
                using var adapter = new SqlDataAdapter(command);
                adapter.Fill(dataTable);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing query: {ex.Message}", ex);
            }
            
            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                
                using var command = new SqlCommand(query, connection);
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }
                
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing non-query: {ex.Message}", ex);
            }
        }

        public async Task<object?> ExecuteScalarAsync(string query, params SqlParameter[] parameters)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                
                using var command = new SqlCommand(query, connection);
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }
                
                return await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing scalar: {ex.Message}", ex);
            }
        }
    }
}

