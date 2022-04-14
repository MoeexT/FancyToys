using System;
using System.Collections.Generic;

using FancyToys.Logging;


namespace FancyToys.Utils {

    internal static class Notifier {
        public enum Keys {
            ServerPanelOpacity,
        }
        
        private static readonly Dictionary<Keys, object> container;

        static Notifier() {
            container = new Dictionary<Keys, object>();
        }

        /// <summary>
        /// value publisher register here
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(Keys key) where T: unmanaged {
            if (container.ContainsKey(key) && container[key] is NotifierJar<T>) {
                return;
            }
            container.Add(key, new NotifierJar<T>());
        }

        /// <summary>
        /// subscribe to value publisher
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static unsafe void Subscribe<T>(Keys key, T* value) where T: unmanaged {
            if (!container.TryGetValue(key, out object obj) || obj is not NotifierJar<T> jar) {
                jar = new NotifierJar<T>();
                container.Add(key, jar);
            }
            jar.AddSubscriber(value);
        }

        /// <summary>
        /// publish value to subscribers
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static void Notify<T>(Keys key, T value) where T: unmanaged {
            if (!container.TryGetValue(key, out object obj)) {
                Dogger.Warn("Notify return1");
                return;
            }

            if (obj is not NotifierJar<T> jar) {
                Dogger.Warn("Notify return2");
                return;
            }
            jar.Notify(value);
        }

        private unsafe class NotifierJar<T> where T: unmanaged {
            private T*[] jar;
            private int size;

            public NotifierJar() {
                size = -1;
                jar = new T*[4];
            }

            public NotifierJar(int size) {
                this.size = -1;
                jar = new T*[size];
            }

            public void Notify(T value) {
                Dogger.Trace("Notify");
                if (size == -1) {
                    Dogger.Warn("Notify return1");
                    return;
                }

                for (int i = 0; i < size; ++i) {
                    Dogger.Info($"Notify:{*(jar[i])} {*(jar[i])}");
                    *(jar[i]) = value;
                }
            }

            public int AddSubscriber(T* value) {
                if (++size == jar.Length - 1) {
                    T*[] biggerJar = new T*[size << 1];
                    Array.Copy(jar, biggerJar, size);
                    jar = biggerJar;
                }
                jar[size] = value;
                return size;
            }
        }
    }
}
