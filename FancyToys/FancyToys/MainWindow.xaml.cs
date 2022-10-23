﻿using FancyToys.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using NLog.Config;
using NLog.Targets;

using System;
using System.Collections.ObjectModel;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI;

using FancyToys.Controls;
using FancyToys.Logging;
using FancyToys.Utils;

using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;


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
                    Content = "Teleport",
                    Name = "TeleportView",
                    Glyph = "\uE95A",
                    Tooltip = "Copy & Notify",
                    Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0, 0x7b, 0xfe))
                },
                new Category() {
                    Content = "Nursery",
                    Name = "NurseryView",
                    Glyph = "\uE9F5",
                    Tooltip = "Process Manager",
                    Foreground = new SolidColorBrush(),
                },
                new Category() {
                    Content = "Server",
                    Name = "ServerView",
                    Glyph = "\uEA92",
                    Tooltip = "Debugging",
                    Foreground = new SolidColorBrush(Colors.Red),
                },
            };
            this.InitializeComponent();
            NavView.SelectedItem = Categories.First();

            LoggingConfiguration config = new();

            FileTarget logFile = new("logfile") {
                FileName = "${specialfolder:folder=LocalApplicationData}/" + _logFileName,
                Layout = "${longdate} ${level} ${message} ${exception}"
            };
            DebuggerTarget logDebugger = new("logdebugger");
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logDebugger);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logFile);
            NLog.LogManager.Configuration = config;
            Dogger.Info("FancyToys started.");

            CurrentWindow = this;
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

    }

}
