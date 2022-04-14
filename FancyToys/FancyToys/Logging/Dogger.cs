using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using FancyToys.Views;

using NLog;

using LogLevel = FancyToys.Logging.LogLevel;


namespace FancyToys.Logging {

    public static class Dogger {

        private static readonly Queue<LogStruct> _logCache;
        private static readonly NLog.Logger NLogger;
        public static LogLevel Level { private get; set; }

        static Dogger() {
            _logCache = new Queue<LogStruct>();
            NLogger = LogManager.GetCurrentClassLogger();
        }

        public static void Trace(string msg, int depth = 1) => Show(msg, LogLevel.Trace, depth + 1);

        public static void Debug(string msg, int depth = 1) => Show(msg, LogLevel.Debug, depth + 1);

        public static void Info(string msg, int depth = 1) => Show(msg, LogLevel.Info, depth + 1);

        public static void Warn(string msg, int depth = 1) => Show(msg, LogLevel.Warn, depth + 1);

        public static void Error(string msg, int depth = 1) => Show(msg, LogLevel.Error, depth + 1);

        public static void Fatal(string msg, int depth = 1) => Show(msg, LogLevel.Fatal, depth + 1);

        private static void Show(string s, LogLevel level, int depth) {
            if (level <= Level) return;

            LogStruct log = new() {
                Level = level,
                Source = $"[{CallerName(depth + 1)}]",
                Content = s,
            };
            Dispatch(log);
        }

        private static void Dispatch(LogStruct log) {
            if (ServerView.CurrentInstance != null) {
                ServerView.CurrentInstance.PrintLog(log);
                NLogger.Info(log.ToString());
            } else {
                _logCache.Enqueue(log);
            }
        }

        public static void Flush() {
            while (_logCache.Count > 0) {
                Dispatch(_logCache.Dequeue());
            }
        }

        private static string CallerName(int depth) {
            MethodBase method = new StackTrace().GetFrame(depth).GetMethod();
            return $"{method?.ReflectedType?.Name}.{method?.Name}";
        }
    }

}
