using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using FancyToys.Views;

using NLog;

using LogLevel = FancyToys.Logging.LogLevel;


namespace FancyToys.Logging {

    public static class Dogger {

        public static LogLevel LogLevel { get; set; } = LogLevel.Trace;
        public static StdType StdLevel { get; set; }

        // TODO 放到一个队列里
        private static readonly Queue<LogStruct> _logCache;
        private static readonly Queue<StdStruct> _stdCache;

        private static readonly NLog.Logger NLogger;

        static Dogger() {
            _logCache = new Queue<LogStruct>();
            _stdCache = new Queue<StdStruct>();
            NLogger = LogManager.GetCurrentClassLogger();
        }

        public static void Trace(string msg, int depth = 1) => Show(msg, LogLevel.Trace, depth + 1);

        public static void Debug(string msg, int depth = 1) => Show(msg, LogLevel.Debug, depth + 1);

        public static void Info(string msg, int depth = 1) => Show(msg, LogLevel.Info, depth + 1);

        public static void Warn(string msg, int depth = 1) => Show(msg, LogLevel.Warn, depth + 1);

        public static void Error(string msg, int depth = 1) => Show(msg, LogLevel.Error, depth + 1);

        public static void Fatal(string msg, int depth = 1) => Show(msg, LogLevel.Fatal, depth + 1);

        private static void Show(string s, LogLevel level, int depth) {
            if (level < LogLevel) return;

            Dispatch(new LogStruct {
                Level = level,
                Source = $"[{CallerName(depth + 1)}]",
                Content = s,
            });
        }

        public static void StdOutput(int pid, string msg) {
            Dispatch(
                new StdStruct {
                    Level = StdType.Output,
                    Sender = pid,
                    Content = msg
                }
            );
        }

        public static void StdError(int pid, string msg) {
            Dispatch(
                new StdStruct {
                    Level = StdType.Error,
                    Sender = pid,
                    Content = msg
                }
            );
        }

        public static void Flush() {
            while (_logCache.Count > 0) {
                Dispatch(_logCache.Dequeue());
            }

            while (_stdCache.Count > 0) {
                Dispatch(_stdCache.Dequeue());
            }
        }

        private static void Dispatch(LogStruct log) {
            NLogger.Info(log.ToString());
            if (ServerView.CurrentInstance != null) {
                ServerView.CurrentInstance.PrintLog(log);
            } else {
                _logCache.Enqueue(log);
            }
        }

        private static void Dispatch(StdStruct ss) {
            if (ServerView.CurrentInstance != null) {
                ServerView.CurrentInstance.PrintStd(ss);
            } else {
                _stdCache.Enqueue(ss);
            }
        }

        private static string CallerName(int depth) {
            MethodBase method = new StackTrace().GetFrame(depth)?.GetMethod();
            return $"{method?.ReflectedType?.Name}.{method?.Name}";
        }
    }

}
