using System;
using System.Collections.ObjectModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

using WinRT.Interop;

using NLog.Config;
using NLog.Targets;

using FancyToys.Views;
using FancyToys.Controls;
using FancyToys.Logging;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow: Window {

        private readonly ObservableCollection<CategoryBase> Categories;
        private const string _logFileName = "fancy_toys.log";

        public static MainWindow CurrentWindow { get; private set; }

        public MainWindow() {
            Categories = new ObservableCollection<CategoryBase>() {
                new Category() {
                    Content = "Nursery",
                    Name = "NurseryView",
                    Glyph = "\uE9F5",
                    Tooltip = "Process manager",
                    Foreground = new SolidColorBrush(Colors.Silver),
                },
                new Category() {
                    Content = "Teleport",
                    Name = "TeleportView",
                    Glyph = "\uE95A",
                    Tooltip = "Clipboard manager",
                    Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0, 0x7b, 0xfe))
                },
                new Category() {
                    Content = "TinyTools",
                    Name = "TinyToolsView",
                    Glyph = "\uE74C",
                    Tooltip = "Tiny tool set",
                    Foreground = new SolidColorBrush(Colors.LightSteelBlue),
                },
                new Category() {
                    Content = "Server",
                    Name = "ServerView",
                    Glyph = "\uEB51",
                    Tooltip = "Debugging",
                    Foreground = new SolidColorBrush(Colors.LightGreen),
                },
            };
            InitializeComponent();
            NavView.SelectedItem = Categories.First();

            LoggingConfiguration config = new();

            FileTarget logFile = new("logfile") {
                FileName = "${specialfolder:folder=LocalApplicationData}/" + _logFileName,
                Layout = "${longdate} ${level} ${message} ${exception}"
            };
            DebuggerTarget logDebugger = new("logdebugger") {
                Layout = "${longdate} ${level} ${message} ${exception}"
            };
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logFile);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logDebugger);
            NLog.LogManager.Configuration = config;
            Dogger.Info("FancyToys started.");

            CurrentWindow = this;

            // no UIElement is set for titlebar, fallback titlebar is created
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar); // this line is optional as by it is null by default

            _ = new ServerView();
            _ = new TinyToolsView();
            _ = new SettingsView();

            AppWindow appWindow = GetAppWindowForCurrentWindow();
            appWindow.Closing += OnAppClosing;
        }

        private void NavViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) {
            if (args.IsSettingsSelected) {
                ContentFrame.Navigate(typeof(SettingsView));
            } else {
                Category selectedItem = (Category)args.SelectedItem;
                Type pageType = Type.GetType($"FancyToys.Views.{selectedItem.Name}");
                ContentFrame.Navigate(pageType);
            }
        }

        private async void OnAppClosing(AppWindow window, AppWindowClosingEventArgs e) {
            e.Cancel = true;
            if (NurseryView.Instance is not null) {
                NurseryView.Instance.OnClosing();
            }

            if (TeleportView.Instance is not null) {
                await TeleportView.Instance.OnClosing();
            }
            
            Close();
        }

        private AppWindow GetAppWindowForCurrentWindow() {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

    }

}
