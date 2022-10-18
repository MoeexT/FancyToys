using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FancyToys.Logging;
using FancyToys.Views;

using NLog;


namespace FancyToys.Nursery {

    public class InformationManager {

        private int updateSpan = 1000;
        private const int minSpan = 20;
        private const int maxSpan = 5000;
        private readonly NurseryView _nurseryView;
        private static readonly object _lock = new();
        private bool _working;

        public int UpdateSpan {
            get => updateSpan;
            set => updateSpan = value < minSpan ? minSpan : value > maxSpan ? maxSpan : value;
        }

        public InformationManager(NurseryView nurseryView) {
            _nurseryView = nurseryView;
        }

        public void run() {
            Task.Run(() => {
                CancellationTokenSource source = new();

                lock (_lock) {
                    if (_working) {
                        source.Cancel();
                        return;
                    }
                }

                Flush(source.Token);
            });
        }

        private async void Flush(CancellationToken token) {
            lock (_lock) {
                _working = true;
            }
            bool goon = false;

            do {
                try {
                    IEnumerable<KeyValuePair<int, NurseryItem>> linq = _nurseryView.NurseryProcesses
                        .Where(item => item.Value.IsRunning)
                        .Select(item => item);

                    Dictionary<int, ProcessStatistic> alivePs = linq.ToDictionary(
                        item => item.Key,
                        item => new ProcessStatistic(
                            item.Key,
                            item.Value.Ps.Id,
                            item.Value.Ps.ProcessName,
                            item.Value.CpuCounter.NextValue(),
                            item.Value.MemCounter.NextValue()
                        )
                    );

                    goon = alivePs.Count > 0;

                    if (goon) {
                        _nurseryView.UpdateProcessInformation(alivePs);
                    }
                } catch (Exception e) {
                    Dogger.Error(e.ToString());
                }

                await Task.Delay(updateSpan, token);
            } while (goon);

            lock (_lock) {
                _working = false;
            }
            // clean the last one process info.
            _nurseryView.UpdateProcessInformation(new Dictionary<int, ProcessStatistic>());
        }
    }

}
