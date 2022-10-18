using System;
using System.Collections.Generic;

using FancyToys.Logging;


namespace FancyToys.Utils {

    internal static class Notifier {
        public enum Keys {
            ServerPanelOpacity,
        }
        
        public delegate void NotifyHandler<in T>(T value);
        
        private static readonly Dictionary<Keys, object> container;

        private static readonly Dictionary<Keys, object> hotel; 

        static Notifier() {
            hotel = new Dictionary<Keys, object>();
            container = new Dictionary<Keys, object>();
        }

        /// <summary>
        /// value publisher register here
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(Keys key) where T: unmanaged {
            if (container.ContainsKey(key) && container[key] is UnmanagedJar<T>) {
                return;
            }
            container.Add(key, new UnmanagedJar<T>());
        }

        /// <summary>
        /// subscribe to value publisher
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static unsafe void Subscribe<T>(Keys key, T* value) where T: unmanaged {
            Dogger.Trace($"Notifier.Subscribe<{typeof(T).Name}>({key})");
            if (!container.TryGetValue(key, out object o) || o is not UnmanagedJar<T> jar) {
                jar = new UnmanagedJar<T>();
                // may throws ArgumentException
                container.Add(key, jar);
            }
            jar.AddSubscriber(value);
        }

        public static void Subscribe<T>(Keys key, NotifyHandler<T> handler) {
            if (!hotel.TryGetValue(key, out object o) || o is not List<NotifyHandler<T>> list) {
                list = new List<NotifyHandler<T>>();
                hotel.Add(key, list);
            }
            list.Add(handler);
        }

        /// <summary>
        /// publish value to subscribers
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public static void Notify<T>(Keys key, T value) where T: unmanaged {
            if (!container.TryGetValue(key, out object obj) || obj is not UnmanagedJar<T> jar) {
                Dogger.Warn("Notify return1");
                return;
            }
            jar.Notify(value);
        }
        
        
        private class ManagedJar<T> {
            private readonly List<NotifyHandler<object>> room;
        }


        private unsafe class UnmanagedJar<T> where T: unmanaged {
            private T*[] jar;
            private int size;

            public UnmanagedJar() {
                size = -1;
                jar = new T*[4];
            }

            public UnmanagedJar(int size) {
                this.size = -1;
                jar = new T*[size];
            }

            public void Notify(T value) {
                Dogger.Trace("Notify");
                for (int i = 0; i <= size; ++i) {
                    Dogger.Info($"Notify:{*(jar[i])} {*(jar[i])}");
                    *(jar[i]) = value;
                }
            }

            public int AddSubscriber(T* subscriber) {
                if (++size == jar.Length) {
                    T*[] biggerJar = new T*[size << 1];
                    Array.Copy(jar, biggerJar, size);
                    jar = biggerJar;
                }
                jar[size] = subscriber;
                return size;
            }
        }
    }
}
