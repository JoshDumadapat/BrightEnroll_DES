using Microsoft.Maui.Handlers;

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
            window.HandlerChanged += (s, e) =>
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
                {
                    // Set default window size (1300x910 pixels)
                    // This ensures the window always opens at this size
                    const int windowWidth = 1300;
                    const int windowHeight = 910;
                    winUIWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
                    
                    // Center the window on the screen
                    var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                        winUIWindow.AppWindow.Id, 
                        Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                    
                    if (displayArea != null)
                    {
                        var centerX = (displayArea.WorkArea.Width - windowWidth) / 2;
                        var centerY = (displayArea.WorkArea.Height - windowHeight) / 2;
                        winUIWindow.AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                    }
                    
                    // Optional: Set minimum window size
                    // Uncomment the lines below if you want to prevent resizing below 1300x910
                    // var presenter = winUIWindow.AppWindow.Presenter as Microsoft.UI.Windowing.AppWindowPresenter;
                    // if (presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
                    // {
                    //     overlappedPresenter.IsResizable = true;
                    // }
                }
            };
#endif
            
            return window;
        }
    }
}
