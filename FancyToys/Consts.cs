using System.Collections.Generic;
using System.Text;

using Windows.UI;

using FancyToys.Logging;

using Microsoft.UI;


namespace FancyToys {

    public static class Consts {
        public static readonly HashSet<LogLevel> HighlightedLogLevels = new HashSet<LogLevel> {
            LogLevel.Fatal
        };

        public static readonly Dictionary<LogLevel, Color> LogForegroundColors = new() {
            { 0, Colors.Gray }, // default value of enum
            { LogLevel.Trace, Colors.Gray },
            { LogLevel.Debug, Colors.Cyan },
            { LogLevel.Info, Colors.MediumSpringGreen },
            { LogLevel.Warn, Colors.Yellow },
            { LogLevel.Error, Colors.Red },
            { LogLevel.Fatal, Colors.Red },
        };

        public static readonly Dictionary<LogLevel, Color> LogBackgroundColors = new() {
            { LogLevel.Fatal, Colors.Red },
        };

        public static readonly HashSet<StdType> HighlightedStdLevels = new HashSet<StdType> {
            StdType.Error,
        };

        public static readonly Dictionary<StdType, Color> StdForegroundColors = new() {
            { StdType.Input, Colors.DodgerBlue },
            { StdType.Output, Colors.Aquamarine },
            { StdType.Error, Colors.Firebrick },
        };

        public static readonly Dictionary<LogLevel, Color> StdBackgroundColors = new() {
            { LogLevel.Fatal, Colors.Red },
        };
        
        public static Encoding Encoding { get; } = Encoding.UTF8;

        public enum SettingKeys {
            LogLevel,
            StdLevel,
            AppTheme,
            MonitorOpacity,
        }
    }

}
