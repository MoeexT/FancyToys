﻿using System;

using FancyToys.Logging;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;


namespace FancyToys.Views {

    public partial class SettingsView {
        public double OpacitySliderValue {
            get => (double)(LocalSettings.Values[nameof(OpacitySliderValue)] ?? 0.6);
            set {
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
                //if (Window.Current.Content is not FrameworkElement framework) return;
                //if (SystemThemeButton.XamlRoot.Content is not FrameworkElement framework) return;
                //NavView.RequestedTheme = value;
                LocalSettings.Values[nameof(CurrentTheme)] = value.ToString();
                OnSettingChanged?.Invoke(LocalSettings, nameof(CurrentTheme));
            }
        }

        public LogLevel LogLevel {
            get => Enum.Parse<LogLevel>(LocalSettings.Values[nameof(LogLevel)] as string ?? LogLevel.Info.ToString());
            set {
                Dogger.Level = value;
                LocalSettings.Values[nameof(LogLevel)] = value.ToString();

                throw new NotImplementedException();
                OnSettingChanged?.Invoke(LocalSettings, nameof(LogLevel));
            }
        }

        public StdType StdLevel {
            get => Enum.Parse<StdType>(LocalSettings.Values[nameof(StdLevel)] as string ?? StdType.Output.ToString());
            set {
                LocalSettings.Values[nameof(StdLevel)] = value.ToString();
                throw new NotImplementedException();
                
                OnSettingChanged?.Invoke(LocalSettings, nameof(StdLevel));
            }
        }

    }

}
