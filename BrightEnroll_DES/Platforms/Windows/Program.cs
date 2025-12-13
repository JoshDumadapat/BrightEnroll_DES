using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace BrightEnroll_DES.WinUI;

/// <summary>
/// Program entry point for WinUI 3 Desktop application.
/// This ensures proper DispatcherQueue initialization before Application.Start.
/// Follows the recommended WinUI 3 Desktop app startup pattern.
/// </summary>
public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            // Initialize COM wrappers for WinRT interop
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            
            // Start the WinUI 3 application
            // Application.Start automatically creates DispatcherQueue on the UI thread
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                // Set up SynchronizationContext in the Application.Start callback
                // This ensures async operations are properly marshalled to the UI thread
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException(
                        "DispatcherQueue is null in Application.Start callback. This should not happen."));
                SynchronizationContext.SetSynchronizationContext(context);

                // Create and initialize the App instance
                // InitializeComponent will now have access to a properly initialized DispatcherQueue
                new App();
            });
        }
        catch (Exception ex)
        {
            // Log startup errors for debugging
            System.Diagnostics.Debug.WriteLine($"[Program.Main] Application startup failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Program.Main] Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"[Program.Main] StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[Program.Main] InnerException: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"[Program.Main] InnerException StackTrace: {ex.InnerException.StackTrace}");
            }

            // Write to file for debugging (if possible)
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BrightEnroll_DES",
                    "startup_error.log");
                var logDir = System.IO.Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir) && !System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }
                
                if (!string.IsNullOrEmpty(logDir))
                {
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Startup Error in Program.Main\n" +
                        $"Exception Type: {ex.GetType().FullName}\n" +
                        $"Message: {ex.Message}\n" +
                        $"StackTrace: {ex.StackTrace}\n" +
                        (ex.InnerException != null 
                            ? $"InnerException: {ex.InnerException.Message}\n" +
                              $"InnerException StackTrace: {ex.InnerException.StackTrace}\n"
                            : "") +
                        $"\n");
                }
            }
            catch 
            { 
                // Ignore file logging errors to prevent masking the original exception
            }

            // Re-throw to show error dialog
            throw;
        }
    }
}
