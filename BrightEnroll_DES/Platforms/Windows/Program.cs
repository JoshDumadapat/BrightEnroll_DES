using System;

namespace BrightEnroll_DES.WinUI
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                global::WinRT.ComWrappersSupport.InitializeComWrappers();

                var app = new Microsoft.UI.Xaml.Application();

                app.Run();
            }
            catch (System.Runtime.InteropServices.SEHException sehEx)
            {
                LogException("seh_exception.log", sehEx);
                throw;
            }
            catch (Exception ex)
            {
                LogException("startup_error.log", ex);
                throw;
            }
        }

        private static void LogException(string fileName, Exception ex)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BrightEnroll_DES",
                    fileName);

                var logDir = System.IO.Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir) && !System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);

                var logContent =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().FullName} - {ex.Message}\n" +
                    $"StackTrace: {ex.StackTrace}\n" +
                    (ex.InnerException != null
                        ? $"InnerException: {ex.InnerException.GetType().FullName} - {ex.InnerException.Message}\n" +
                          $"StackTrace: {ex.InnerException.StackTrace}\n"
                        : "") +
                    "\n";

                System.IO.File.AppendAllText(logPath, logContent);
            }
            catch
            {
                // Never mask the original exception
            }
        }
    }
}


//using Microsoft.UI.Dispatching;
//using Microsoft.UI.Xaml;
//using System;
//using System.Threading;

//namespace BrightEnroll_DES.WinUI;

//public class Program
//{
//    [STAThread]
//    static void Main(string[] args)
//    {
//        try
//        {
//            // Initialize COM wrappers for WinRT interop
//            global::WinRT.ComWrappersSupport.InitializeComWrappers();

//            // Start the WinUI 3 application
//            // Application.Start automatically creates DispatcherQueue on the UI thread
//            Microsoft.UI.Xaml.Application.Start((p) =>
//            {
//                try
//                {
//                    // Set up SynchronizationContext in the Application.Start callback
//                    // This ensures async operations are properly marshalled to the UI thread
//                    var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
//                    if (dispatcherQueue == null)
//                    {
//                        throw new InvalidOperationException(
//                            "DispatcherQueue is null in Application.Start callback. This should not happen.");
//                    }

//                    var context = new DispatcherQueueSynchronizationContext(dispatcherQueue);
//                    SynchronizationContext.SetSynchronizationContext(context);

//                    // Create and initialize the App instance
//                    // InitializeComponent will now have access to a properly initialized DispatcherQueue
//                    // Wrap in try-catch to handle SEHException gracefully
//                    try
//                    {
//                        new App();
//                    }
//                    catch (System.Runtime.InteropServices.SEHException sehEx)
//                    {
//                        // SEHException often indicates missing WinUI runtime or native dependencies
//                        System.Diagnostics.Debug.WriteLine($"[Program.Main] SEHException creating App: {sehEx.Message}");
//                        System.Diagnostics.Debug.WriteLine($"[Program.Main] SEHException ErrorCode: 0x{sehEx.ErrorCode:X8}");

//                        // Log to file
//                        try
//                        {
//                            var logPath = System.IO.Path.Combine(
//                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//                                "BrightEnroll_DES",
//                                "seh_exception.log");
//                            var logDir = System.IO.Path.GetDirectoryName(logPath);
//                            if (!string.IsNullOrEmpty(logDir) && !System.IO.Directory.Exists(logDir))
//                            {
//                                System.IO.Directory.CreateDirectory(logDir);
//                            }

//                            if (!string.IsNullOrEmpty(logDir))
//                            {
//                                System.IO.File.AppendAllText(logPath,
//                                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SEHException in App() constructor\n" +
//                                    $"ErrorCode: 0x{sehEx.ErrorCode:X8}\n" +
//                                    $"Message: {sehEx.Message}\n" +
//                                    $"StackTrace: {sehEx.StackTrace}\n\n");
//                            }
//                        }
//                        catch { /* Ignore file logging errors */ }

//                        // Re-throw to show error dialog
//                        throw;
//                    }
//                }
//                catch (Exception innerEx)
//                {
//                    // Log any other exceptions in the Application.Start callback
//                    System.Diagnostics.Debug.WriteLine($"[Program.Main] Exception in Application.Start callback: {innerEx.Message}");
//                    System.Diagnostics.Debug.WriteLine($"[Program.Main] Exception Type: {innerEx.GetType().FullName}");
//                    System.Diagnostics.Debug.WriteLine($"[Program.Main] StackTrace: {innerEx.StackTrace}");
//                    throw;
//                }
//            });
//        }
//        catch (Exception ex)
//        {
//            // Log startup errors for debugging
//            System.Diagnostics.Debug.WriteLine($"[Program.Main] Application startup failed: {ex.Message}");
//            System.Diagnostics.Debug.WriteLine($"[Program.Main] Exception Type: {ex.GetType().FullName}");
//            System.Diagnostics.Debug.WriteLine($"[Program.Main] StackTrace: {ex.StackTrace}");

//            if (ex.InnerException != null)
//            {
//                System.Diagnostics.Debug.WriteLine($"[Program.Main] InnerException: {ex.InnerException.Message}");
//                System.Diagnostics.Debug.WriteLine($"[Program.Main] InnerException StackTrace: {ex.InnerException.StackTrace}");
//            }

//            // Write to file for debugging (if possible)
//            try
//            {
//                var logPath = System.IO.Path.Combine(
//                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//                    "BrightEnroll_DES",
//                    "startup_error.log");
//                var logDir = System.IO.Path.GetDirectoryName(logPath);
//                if (!string.IsNullOrEmpty(logDir) && !System.IO.Directory.Exists(logDir))
//                {
//                    System.IO.Directory.CreateDirectory(logDir);
//                }

//                if (!string.IsNullOrEmpty(logDir))
//                {
//                    System.IO.File.AppendAllText(logPath,
//                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Startup Error in Program.Main\n" +
//                        $"Exception Type: {ex.GetType().FullName}\n" +
//                        $"Message: {ex.Message}\n" +
//                        $"StackTrace: {ex.StackTrace}\n" +
//                        (ex.InnerException != null 
//                            ? $"InnerException: {ex.InnerException.Message}\n" +
//                              $"InnerException StackTrace: {ex.InnerException.StackTrace}\n"
//                            : "") +
//                        $"\n");
//                }
//            }
//            catch 
//            { 
//                // Ignore file logging errors to prevent masking the original exception
//            }

//            // Re-throw to show error dialog
//            throw;
//        }
//    }
//}
