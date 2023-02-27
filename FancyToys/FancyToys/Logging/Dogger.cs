using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using FancyToys.Views;

using NLog;


namespace FancyToys.Logging {

    public static class Dogger {

        public static LogLevel LogLevel { get; set; }
        public static StdType StdLevel { get; set; }

        private static readonly Logger NLogger;
        private static readonly Queue<LogStruct> _logCache;
        private static readonly Queue<StdStruct> _stdCache;

        static Dogger() {
            _logCache = new Queue<LogStruct>();
            _stdCache = new Queue<StdStruct>();
            NLogger = LogManager.GetCurrentClassLogger();
        }

        public static void Trace(string msg, [CallerFilePath] string cp = null, [CallerMemberName] string cmn = null) {
            NLogger.Trace(msg);
            Show(msg, LogLevel.Trace, cp, cmn);
        }

        public static void Debug(string msg, [CallerFilePath] string cp = null, [CallerMemberName] string cmn = null) {
            NLogger.Debug(msg);
            Show(msg, LogLevel.Debug, cp, cmn);
        }

        public static void Info(string msg, [CallerFilePath] string cp = null, [CallerMemberName] string cmn = null) {
            NLogger.Info(msg);
            Show(msg, LogLevel.Info, cp, cmn);
        }

        public static void Warn(string msg, [CallerFilePath] string cp = null, [CallerMemberName] string cmn = null) {
            NLogger.Warn(msg);
            Show(msg, LogLevel.Warn, cp, cmn);
        }

        public static void Error(string msg, [CallerFilePath] string cp = null, [CallerMemberName] string cmn = null) {
            NLogger.Error(msg);
            Show(msg, LogLevel.Error, cp, cmn);
        }

        public static void Fatal(string msg, [CallerFilePath] string cp = null, [CallerMemberName] string cmn = null) {
            NLogger.Fatal(msg);
            Show(msg, LogLevel.Fatal, cp, cmn);
        }

        private static void Show(string s, LogLevel level, string callerFilePath, string callerMemberName) {
            if (level < LogLevel || string.IsNullOrEmpty(s)) {
                return;
            }

            Dispatch(new LogStruct {
                Level = level,
                Source = $"[{Path.GetFileNameWithoutExtension(callerFilePath)}.{callerMemberName}]",
                Content = s,
            });
        }

        public static void StdOutput(int pid, string msg) {
            if (StdLevel == StdType.Error) {
                return;
            }

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

        [Obsolete]
        private static string CallerName(int depth) {
            MethodBase method = new StackTrace().GetFrame(depth)?.GetMethod();
            return $"{method?.ReflectedType?.Name}.{method?.Name}";
        }
    }

}
