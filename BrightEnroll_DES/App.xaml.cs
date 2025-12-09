using Microsoft.Maui.Handlers;
using System.Threading.Tasks;

namespace BrightEnroll_DES
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "BrightEnroll_DES" };
            
#if WINDOWS
            // Configure window size for Windows platform
            window.HandlerChanged += async (s, e) =>
            {
                try
                {
                    // Add a small delay to ensure the window is fully initialized
                    await Task.Delay(100);
                    
                    if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
                    {
                        // Check if AppWindow is available
                        if (winUIWindow.AppWindow == null)
                        {
                            System.Diagnostics.Debug.WriteLine("Warning: AppWindow is null, skipping window configuration.");
                            return;
                        }

                        // Set default window size (1300x910 pixels)
                        // This ensures the window always opens at this size
                        const int windowWidth = 1300;
                        const int windowHeight = 910;
                        
                        try
                        {
                            // Use dispatcher to ensure we're on the UI thread
                            var resizeSuccess = winUIWindow.DispatcherQueue.TryEnqueue(() =>
                            {
                                try
                                {
                                    if (winUIWindow.AppWindow != null)
                                    {
                                        winUIWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
                                    }
                                }
                                catch (Exception resizeEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error resizing window: {resizeEx.Message}");
                                }
                            });
                            
                            if (!resizeSuccess)
                            {
                                System.Diagnostics.Debug.WriteLine("Warning: Could not enqueue window resize operation.");
                            }
                        }
                        catch (Exception resizeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error resizing window: {resizeEx.Message}");
                            // Continue with centering even if resize fails
                        }
                        
                        // Center the window on the screen
                        try
                        {
                            var centerSuccess = winUIWindow.DispatcherQueue.TryEnqueue(() =>
                            {
                                try
                                {
                                    if (winUIWindow.AppWindow?.Id != null)
                                    {
                                        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                                            winUIWindow.AppWindow.Id, 
                                            Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                                        
                                        if (displayArea != null && winUIWindow.AppWindow != null)
                                        {
                                            var centerX = (displayArea.WorkArea.Width - windowWidth) / 2;
                                            var centerY = (displayArea.WorkArea.Height - windowHeight) / 2;
                                            winUIWindow.AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                                        }
                                    }
                                }
                                catch (Exception centerEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error centering window: {centerEx.Message}");
                                }
                            });
                            
                            if (!centerSuccess)
                            {
                                System.Diagnostics.Debug.WriteLine("Warning: Could not enqueue window centering operation.");
                            }
                        }
                        catch (Exception centerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error centering window: {centerEx.Message}");
                            // Window will still open, just not centered
                        }
                        
                        // Optional: Set minimum window size
                        // Uncomment the lines below if you want to prevent resizing below 1300x910
                        // var presenter = winUIWindow.AppWindow.Presenter as Microsoft.UI.Windowing.AppWindowPresenter;
                        // if (presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
                        // {
                        //     overlappedPresenter.IsResizable = true;
                        // }
                    }
                }
                catch (System.Runtime.InteropServices.SEHException sehEx)
                {
                    // Handle SEHException specifically
                    System.Diagnostics.Debug.WriteLine($"SEHException in window handler: {sehEx.Message}");
                    // Don't rethrow - allow the application to continue
                }
                catch (Exception ex)
                {
                    // Handle any other exceptions
                    System.Diagnostics.Debug.WriteLine($"Error in window handler: {ex.Message}");
                    // Don't rethrow - allow the application to continue
                }
            };
#endif
            
            return window;
        }
    }
}
