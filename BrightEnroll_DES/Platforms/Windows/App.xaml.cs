using Microsoft.UI.Xaml;

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
            try
            {
                this.InitializeComponent();
            }
            catch (System.Runtime.InteropServices.SEHException sehEx)
            {
                // Handle SEHException during initialization
                System.Diagnostics.Debug.WriteLine($"SEHException during App initialization: {sehEx.Message}");
                // Try to continue - the app might still work
            }
            catch (Exception ex)
            {
                // Handle any other exceptions during initialization
                System.Diagnostics.Debug.WriteLine($"Exception during App initialization: {ex.Message}");
                throw; // Re-throw non-SEH exceptions as they might be critical
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
