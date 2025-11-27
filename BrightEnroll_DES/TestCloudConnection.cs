using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BrightEnroll_DES.Data;

namespace BrightEnroll_DES;

/// <summary>
/// Standalone program to test cloud database connection
/// Run this from command line: dotnet run --project BrightEnroll_DES.csproj -- TestCloudConnection
/// Or compile and run: dotnet build && dotnet run
/// </summary>
public class CloudConnectionTestProgram
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("Cloud Database Connection Test");
        Console.WriteLine("==========================================\n");

        try
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Get cloud connection string
            var cloudConnectionString = configuration.GetConnectionString("CloudConnection");
            
            if (string.IsNullOrWhiteSpace(cloudConnectionString))
            {
                cloudConnectionString = "Server=db33580.public.databaseasp.net; Database=db33580; User Id=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
                Console.WriteLine("⚠ Warning: Using fallback connection string from code\n");
            }
            else
            {
                Console.WriteLine("✓ Using connection string from appsettings.json\n");
            }

            // Display connection info (mask password)
            var displayString = cloudConnectionString;
            if (displayString.Contains("Password="))
            {
                var passwordIndex = displayString.IndexOf("Password=") + 9;
                var passwordEnd = displayString.IndexOf(";", passwordIndex);
                if (passwordEnd == -1) passwordEnd = displayString.Length;
                displayString = displayString.Substring(0, passwordIndex) + "***" + displayString.Substring(passwordEnd);
            }
            Console.WriteLine($"Connection String: {displayString}\n");

            // Test connection
            Console.WriteLine("Testing connection...");
            var startTime = DateTime.Now;

            // Create DbContextOptions and CloudDbContext
            var optionsBuilder = new DbContextOptionsBuilder<CloudDbContext>();
            optionsBuilder.UseSqlServer(cloudConnectionString);
            await using var context = new CloudDbContext(optionsBuilder.Options);

            var canConnect = await context.Database.CanConnectAsync();
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            if (canConnect)
            {
                Console.WriteLine($"\n✅ SUCCESS: Cloud database connection successful!");
                Console.WriteLine($"   Connection time: {elapsed:F0}ms\n");

                // Try a simple query
                Console.WriteLine("Testing query execution...");
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                Console.WriteLine("✅ Query execution successful!\n");

                // Try to get database name
                try
                {
                    var dbName = context.Database.GetDbConnection().Database;
                    Console.WriteLine($"Database Name: {dbName}");
                }
                catch { }

                // Try to get server version
                try
                {
                    var serverVersion = context.Database.GetDbConnection().ServerVersion;
                    Console.WriteLine($"Server Version: {serverVersion}");
                }
                catch { }

                Console.WriteLine("\n✅ All tests passed! Your system can connect to the cloud database.");
            }
            else
            {
                Console.WriteLine($"\n❌ FAILED: Cannot connect to cloud database");
                Console.WriteLine($"   Connection attempt took: {elapsed:F0}ms\n");
                Console.WriteLine("Please check:");
                Console.WriteLine("  • Internet connection");
                Console.WriteLine("  • Firewall settings (port 1433)");
                Console.WriteLine("  • Connection string in appsettings.json");
                Console.WriteLine("  • Server name and credentials");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            Console.WriteLine($"\n❌ SQL SERVER ERROR:");
            Console.WriteLine($"   Error Number: {sqlEx.Number}");
            Console.WriteLine($"   Error Message: {sqlEx.Message}\n");

            if (sqlEx.Number == 53 || sqlEx.Number == -1)
            {
                Console.WriteLine("🔍 DIAGNOSIS: Network path not found");
                Console.WriteLine("\nPossible causes:");
                Console.WriteLine("  • Server name is incorrect");
                Console.WriteLine("  • Firewall is blocking port 1433");
                Console.WriteLine("  • SQL Server is not configured for remote connections");
                Console.WriteLine("  • DNS resolution failed");
            }
            else if (sqlEx.Number == 18456)
            {
                Console.WriteLine("🔍 DIAGNOSIS: Login failed");
                Console.WriteLine("\nPossible causes:");
                Console.WriteLine("  • Incorrect username or password");
                Console.WriteLine("  • SQL Server authentication failed");
            }
            else if (sqlEx.Number == 2)
            {
                Console.WriteLine("🔍 DIAGNOSIS: Server not found");
                Console.WriteLine("\nPossible causes:");
                Console.WriteLine("  • SQL Server is not running");
                Console.WriteLine("  • Server is unreachable");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
        }

        Console.WriteLine("\n==========================================");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

