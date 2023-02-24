using System;
using System.Collections.Generic;

using Windows.Storage;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;

using NAudio.CoreAudioApi;

using FancyToys.Logging;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Views {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsView: Page {
        public delegate void SettingChangedEventHandler(ApplicationDataContainer settings, string key);

        public event SettingChangedEventHandler OnSettingChanged;
        private ApplicationDataContainer LocalSettings { get; }
        private List<ComboBoxItem> LogComboItemList { get; }
        private List<ComboBoxItem> StdComboItemList { get; }

        public SettingsView() {
            InitializeComponent();
            LocalSettings = ApplicationData.Current.LocalSettings;
            LogComboItemList = new List<ComboBoxItem>();
            StdComboItemList = new List<ComboBoxItem>();

            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel))) {
                LogComboItemList.Add(new ComboBoxItem {
                    Content = level,
                    Foreground = new SolidColorBrush(Consts.LogForegroundColors[level]),
                });
            }

            foreach (StdType type in Enum.GetValues(typeof(StdType))) {
                StdComboItemList.Add(new ComboBoxItem {
                    Content = type,
                    Foreground = new SolidColorBrush(Consts.StdForegroundColors[type]),
                });
            }
            InitializeDefaultSettings();
        }

        private void InitializeDefaultSettings() {
            InitializeValues();

            switch (CurrentTheme) {
                case ElementTheme.Dark:
                    DarkThemeButton.IsChecked = true;
                    break;
                case ElementTheme.Light:
                    LightThemeButton.IsChecked = true;
                    break;
                case ElementTheme.Default:
                default:
                    SystemThemeButton.IsChecked = true;
                    break;
            }
        }

        private void ChangeTheme(object sender, RoutedEventArgs e) {
            if (sender is null) return;

            CurrentTheme = (sender as RadioButton)!.Content switch {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                "System" => ElementTheme.Default,
                _ => ElementTheme.Default
            };
        }

        private void LogLevelChanged(object sender, SelectionChangedEventArgs e) {
            if (sender != LogLevelComboBox) return;

            ComboBoxItem item = (ComboBoxItem)LogLevelComboBox.SelectedItem;
            TextBlock header = (TextBlock)LogLevelComboBox.Header;
            Brush originHeaderForeground = header!.Foreground;
            LogLevelComboBox.Foreground = item!.Foreground;
            header.Foreground = originHeaderForeground;
            LogLevel = (LogLevel)(item.Content ?? LogLevel);
        }

        private void StdLevelChanged(object sender, SelectionChangedEventArgs e) {
            if (!ReferenceEquals(sender, StdLevelComboBox)) return;
            ComboBoxItem item = (ComboBoxItem)StdLevelComboBox.SelectedItem;
            TextBlock header = (TextBlock)StdLevelComboBox.Header;
            Brush originHeaderForeground = header!.Foreground;
            StdLevelComboBox.Foreground = item!.Foreground;
            header.Foreground = originHeaderForeground;
            Dogger.StdLevel = item.Content is null ? StdLevel : (StdType)item.Content;
        }

        private int IndexOfLogLevels() {
            foreach (ComboBoxItem item in LogComboItemList) {
                if (item.Content is LogLevel level && level == LogLevel) {
                    return LogComboItemList.IndexOf(item);
                }
            }
            Dogger.Warn($"LogLevel {LogLevel} is not in {nameof(LogComboItemList)}.");
            return 0;
        }

        private int IndexOfStdLevels() {
            foreach (ComboBoxItem item in StdComboItemList) {
                if (item.Content is StdType level && level == StdLevel) {
                    return StdComboItemList.IndexOf(item);
                }
            }
            Dogger.Warn($"StdLevel {StdLevel} is not in {nameof(StdComboItemList)}.");
            return 0;
        }

        private void Opacity_OnTapped(object sender, TappedRoutedEventArgs e) {
            if (sender is not TextBlock opacityPreview) return;

            if (opacityPreview.Tag.ToString().Equals("White")) {
                MonitorFontColor = new SolidColorBrush(Colors.Black);
                opacityPreview.Tag = "Black";
            } else {
                MonitorFontColor = new SolidColorBrush(Colors.White);
                opacityPreview.Tag = "White";
            }
        }
    }

}
