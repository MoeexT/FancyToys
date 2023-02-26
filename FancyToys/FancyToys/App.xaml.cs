using System;
using System.Runtime.InteropServices;

using WinRT;

using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Windows.AppLifecycle;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FancyToys {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App: Application {

        public static XamlRoot MainXamlRoot { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            this.InitializeComponent();
        }

        private void OnSuspending(object sender, SuspendingEventArgs e) {
            //This is never called in WInUi 3 in Desktop Preview 3
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// https://github.com/microsoft/WinUI-3-Demos/blob/420c48fe1613cb20b38000252369a0c556543eac/src/Build2020Demo/DemoBuildCs/DemoBuildCs/DemoBuildCs/App.xaml.cs#L41
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            m_window = new MainWindow();
            //Get the Window's HWND
            var windowNative = m_window.As<IWindowNative>();
            WindowHandle = windowNative.WindowHandle;
            m_window.Title = "FancyToys";
            m_window.Activate();
            MainXamlRoot = m_window.Content.XamlRoot;
            if (m_window.Content is FrameworkElement fe) {
                //fe.Loaded += (s, e) => Login();
            }

            // The Window object doesn't have Width and Height properties in WInUI 3 Desktop yet.
            // To set the Width and Height, you can use the Win32 API SetWindowPos.
            // Note, you should apply the DPI scale factor if you are thinking of dpi instead of pixels.
            SetWindowSize(WindowHandle, 900, 600);
        }

        protected async void Login() {
            var messageDialog = new ContentDialog {
                XamlRoot = m_window.Content.XamlRoot
            };
            await messageDialog.ShowAsync();
        }

        private static void SetWindowSize(IntPtr hwnd, int width, int height) {
            int dpi = PInvoke.User32.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            PInvoke.User32.SetWindowPos(hwnd, PInvoke.User32.SpecialWindowHandles.HWND_TOP,
                0, 0, width, height,
                PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE);
        }

        private Window m_window;
        public IntPtr WindowHandle { get; private set; }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative {
            IntPtr WindowHandle { get; }
        }
    }
}
