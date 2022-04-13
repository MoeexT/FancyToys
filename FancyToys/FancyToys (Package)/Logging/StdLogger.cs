using System;
using System.Collections.Generic;

using FancyToys.Views;


namespace FancyToys.Logging {

    public static class StdLogger {

        private static Queue<StdStruct> _cache;

        public static StdType Level { private get; set; }

        static StdLogger() {
            _cache = new Queue<StdStruct>();

            Dispatch(new StdStruct() {
                Content = "StdLogger initialized",
                Level = StdType.Error,
                Sender = 0,
            });
        }

        private static void Dispatch(StdStruct ss) {
            if (ServerView.CurrentInstance != null) {
                ServerView.CurrentInstance.PrintStd(ss);
            } else {
                _cache.Enqueue(ss);
            }
        }

        public static void Flush() {
            while (_cache.Count > 0) {
                Dispatch(_cache.Dequeue());
            }
        }

        public static void StdOutput(int pid, string msg) {
            throw new NotImplementedException();
        }

        public static void StdError(int pid, string msg) {
            throw new NotImplementedException();
        }
    }

}
