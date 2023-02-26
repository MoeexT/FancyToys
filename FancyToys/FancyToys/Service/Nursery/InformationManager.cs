using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FancyToys.Logging;
using FancyToys.Views;


namespace FancyToys.Service.Nursery {

    public class InformationManager {

        private int updateSpan = 1000;
        private const int minSpan = 20;
        private const int maxSpan = 5000;
        private readonly NurseryView _nurseryView;
        private static readonly object _lock = new();
        private State state;

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
                    switch (state) {
                        case State.Sleeping:
                            source.Cancel();
                            return;
                        case State.Working:
                            return;
                        case State.Resting:
                        default:
                            Flush(source.Token);
                            break;
                    }
                }
            });
        }

        private async void Flush(CancellationToken token) {
            lock (_lock) { state = State.Working; }

            bool goon;
            Dogger.Debug("InformationManager started.");

            do {
                IEnumerable<KeyValuePair<int, NurseryItem>> linq = _nurseryView.NurseryList
                    .Where(item => item.IsAlive)
                    .Select(item => new KeyValuePair<int, NurseryItem>(item.NurseryId, item));

                Dictionary<int, ProcessStatistic> aliveProcesses = linq.ToDictionary(
                    item => item.Key,
                    item => item.Value.Statistic());

                goon = aliveProcesses.Count > 0;

                if (goon) {
                    _nurseryView.UpdateProcessInformation(aliveProcesses);
                }

                lock (_lock) { state = State.Sleeping; }
                await Task.Delay(updateSpan, token);
                lock (_lock) { state = State.Working; }
            } while (goon);

            lock (_lock) { state = State.Resting; }

            // clean the last one process info.
            _nurseryView.UpdateProcessInformation();
            Dogger.Debug("InformationManager stopped.");
        }

        private enum State {
            Resting = 0,
            Sleeping = 1,
            Working = 2,
        }
    }

}
