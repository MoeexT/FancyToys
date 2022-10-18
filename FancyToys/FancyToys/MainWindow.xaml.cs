using FancyToys.Views;

using Microsoft.UI.Xaml;

using muxc = Microsoft.UI.Xaml.Controls;

using NLog.Config;
using NLog.Targets;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using FancyToys.Logging;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow: Window {
        private readonly Dictionary<string, Type> views;
        private const string _logFileName = "fancy_toys.log";

        public static MainWindow CurrentWindow { get; private set; }
        
        public MainWindow() {
            this.InitializeComponent();

            views = new Dictionary<string, Type> {
                { "Nursery", typeof(NurseryView) },
                { "Teleport", typeof(TeleportView) },
                { "FancyServer", typeof(ServerView) },
            };

            LoggingConfiguration config = new();

            FileTarget logFile = new("logfile") {
                FileName = "${specialfolder:folder=LocalApplicationData}/" + _logFileName,
                Layout = "${longdate} ${level} ${message} ${exception}"
            };
            DebuggerTarget logDebugger = new("logdebugger");
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logDebugger);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logFile);
            NLog.LogManager.Configuration = config;
            Dogger.Info("FancyToys started.");
            CurrentWindow = this;
        }

        private void NavViewSelectionChanged(muxc.NavigationView sender, muxc.NavigationViewSelectionChangedEventArgs args) {
            if (args.IsSettingsSelected) {
                ContentFrame.Navigate(typeof(SettingsView));
            } else {
                var selectedItem = (muxc.NavigationViewItem)args.SelectedItem;
                ContentFrame.Navigate(views[selectedItem.Name]);
            }
        }

    }

}
