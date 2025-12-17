using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class SchoolDatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchoolDatabaseService>? _logger;

    public SchoolDatabaseService(IConfiguration configuration, ILogger<SchoolDatabaseService>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Generates a unique database name based on school information
    public string GenerateDatabaseName(string schoolName, string customerCode)
    {
        // Clean school name: remove special characters, spaces, make lowercase
        var cleanName = System.Text.RegularExpressions.Regex.Replace(schoolName, @"[^a-zA-Z0-9]", "");
        cleanName = cleanName.ToLower();
        
        // Limit to 50 characters
        if (cleanName.Length > 50)
        {
            cleanName = cleanName.Substring(0, 50);
        }

        // Use customer code as suffix for uniqueness
        var dbName = $"DB_{cleanName}_{customerCode}";
        
        // Ensure it's valid SQL identifier (max 128 chars for SQL Server)
        if (dbName.Length > 128)
        {
            dbName = dbName.Substring(0, 128);
        }

        return dbName;
    }

    // Creates a new database for the school on the cloud server
    public async Task<bool> CreateSchoolDatabaseAsync(string databaseName, string masterConnectionString)
    {
        try
        {
            _logger?.LogInformation($"Creating database '{databaseName}' on cloud server...");

            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Check if database already exists
            var checkDbQuery = $@"
                SELECT COUNT(*) 
                FROM sys.databases 
                WHERE name = @DatabaseName";

            using (var checkCmd = new SqlCommand(checkDbQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@DatabaseName", databaseName);
                var result = await checkCmd.ExecuteScalarAsync();
                var exists = result != null ? (int)result : 0;
                
                if (exists > 0)
                {
                    _logger?.LogWarning($"Database '{databaseName}' already exists.");
                    return true; // Database exists, consider it successful
                }
            }

            // Create the database
            var createDbQuery = $@"
                CREATE DATABASE [{databaseName}]
                COLLATE SQL_Latin1_General_CP1_CI_AS";

            using (var createCmd = new SqlCommand(createDbQuery, connection))
            {
                await createCmd.ExecuteNonQueryAsync();
                _logger?.LogInformation($"Database '{databaseName}' created successfully.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error creating database '{databaseName}': {ex.Message}");
            return false;
        }
    }

    // Generates a connection string for the school's database
    public string GenerateConnectionString(string databaseName, string baseConnectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            };

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error generating connection string for database '{databaseName}': {ex.Message}");
            throw;
        }
    }

    // Gets the master database connection string from configuration
    public string GetMasterConnectionString()
    {
        // Get cloud connection string from appsettings
        var cloudConnection = _configuration.GetConnectionString("CloudConnection");
        
        if (string.IsNullOrWhiteSpace(cloudConnection))
        {
            throw new Exception("CloudConnection string is not configured in appsettings.json");
        }

        // Parse the connection string and change database to 'master'
        var builder = new SqlConnectionStringBuilder(cloudConnection)
        {
            InitialCatalog = "master"
        };

        return builder.ConnectionString;
    }

    // Provisions a complete database setup for a school
    public async Task<SchoolDatabaseInfo> ProvisionSchoolDatabaseAsync(string schoolName, string customerCode)
    {
        try
        {
            _logger?.LogInformation($"Provisioning database for school: {schoolName} (Code: {customerCode})");

            // Generate database name
            var databaseName = GenerateDatabaseName(schoolName, customerCode);
            _logger?.LogInformation($"Generated database name: {databaseName}");

            // Get master connection string
            var masterConnectionString = GetMasterConnectionString();

            // Create the database
            var dbCreated = await CreateSchoolDatabaseAsync(databaseName, masterConnectionString);
            
            if (!dbCreated)
            {
                throw new Exception($"Failed to create database '{databaseName}'");
            }

            // Generate connection string for the new database
            var baseConnectionString = _configuration.GetConnectionString("CloudConnection");
            if (string.IsNullOrWhiteSpace(baseConnectionString))
            {
                throw new Exception("CloudConnection string is not configured in appsettings.json");
            }
            var connectionString = GenerateConnectionString(databaseName, baseConnectionString);

            _logger?.LogInformation($"Database provisioning completed for '{databaseName}'");

            return new SchoolDatabaseInfo
            {
                DatabaseName = databaseName,
                ConnectionString = connectionString,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error provisioning database for school '{schoolName}': {ex.Message}");
            return new SchoolDatabaseInfo
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

public class SchoolDatabaseInfo
{
    public string? DatabaseName { get; set; }
    public string? ConnectionString { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

