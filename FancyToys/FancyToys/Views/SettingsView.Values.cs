using System;

using FancyToys.Logging;
using FancyToys.Utils;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using NAudio.CoreAudioApi;

namespace FancyToys.Views {

    public partial class SettingsView {

        public double OpacitySliderValue {
            get => (double)(LocalSettings.Values[nameof(OpacitySliderValue)] ?? 0.6);
            set {
                Notifier.Notify(Notifier.Keys.ServerPanelOpacity, value);
                LocalSettings.Values[nameof(OpacitySliderValue)] = value;
                OnSettingChanged?.Invoke(LocalSettings, nameof(OpacitySliderValue));
            }
        }

        public Brush MonitorFontColor {
            get => (Brush)LocalSettings.Values[nameof(MonitorFontColor)];
            set {
                LocalSettings.Values[nameof(MonitorFontColor)] = value;
                OnSettingChanged?.Invoke(LocalSettings, nameof(MonitorFontColor));
            }
        }

        public ElementTheme CurrentTheme {
            get => Enum.Parse<ElementTheme>(LocalSettings.Values[nameof(CurrentTheme)] as string ?? ElementTheme.Default.ToString());
            set {
                if (MainWindow.CurrentWindow.Content is FrameworkElement fe) {
                     fe.RequestedTheme = value;
                }
                LocalSettings.Values[nameof(CurrentTheme)] = value.ToString();
                OnSettingChanged?.Invoke(LocalSettings, nameof(CurrentTheme));
            }
        }

        public LogLevel LogLevel {
            get => Enum.Parse<LogLevel>(LocalSettings.Values[nameof(LogLevel)] as string ?? LogLevel.Trace.ToString());
            set {
                Dogger.LogLevel = value;
                LocalSettings.Values[nameof(LogLevel)] = value.ToString();
                OnSettingChanged?.Invoke(LocalSettings, nameof(LogLevel));
            }
        }

        public StdType StdLevel {
            get => Enum.Parse<StdType>(LocalSettings.Values[nameof(StdLevel)] as string ?? StdType.Output.ToString());
            set {
                Dogger.StdLevel = value;
                LocalSettings.Values[nameof(StdLevel)] = value.ToString();
                OnSettingChanged?.Invoke(LocalSettings, nameof(StdLevel));
            }
        }

    }

}
