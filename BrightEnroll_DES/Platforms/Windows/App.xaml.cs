using Microsoft.UI.Xaml;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BrightEnroll_DES.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // InitializeComponent should be called first and is now safe because
            // Program.cs ensures DispatcherQueue is properly initialized
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp()
        {
            try
            {
                return MauiProgram.CreateMauiApp();
            }
            catch (System.Runtime.InteropServices.SEHException sehEx)
            {
                // Handle SEHException during MauiApp creation
                // SEHException often indicates missing WinUI runtime or native dependencies
                System.Diagnostics.Debug.WriteLine($"SEHException during CreateMauiApp: {sehEx.Message}");
                System.Diagnostics.Debug.WriteLine($"SEHException StackTrace: {sehEx.StackTrace}");
                if (sehEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SEHException InnerException: {sehEx.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"SEHException InnerException StackTrace: {sehEx.InnerException.StackTrace}");
                }
                
                // Log to file for debugging (if possible)
                try
                {
                    var logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "BrightEnroll_DES",
                        "error.log");
                    var logDir = System.IO.Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(logDir) && !System.IO.Directory.Exists(logDir))
                    {
                        System.IO.Directory.CreateDirectory(logDir);
                    }
                    System.IO.File.AppendAllText(logPath, 
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SEHException in CreateMauiApp: {sehEx.Message}\n" +
                        $"StackTrace: {sehEx.StackTrace}\n\n");
                }
                catch { /* Ignore file logging errors */ }
                
                // Re-throw to prevent app from starting in a broken state
                // This will show the error dialog, but at least we've logged it
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception during CreateMauiApp: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }

}
