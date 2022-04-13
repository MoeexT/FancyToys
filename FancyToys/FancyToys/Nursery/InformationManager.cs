using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ProcessManager _processManager;

        public int UpdateSpan {
            get => updateSpan;
            set =>
                updateSpan = value < minSpan ? minSpan : value > maxSpan ? maxSpan : value;
        }

        public InformationManager(NurseryView nurseryView, ProcessManager processManager) {
            _nurseryView = nurseryView;
            _processManager = processManager;
        }

        public void run() {
            Task.Run(
                async () => {
                    bool shouldClear = false;
                    List<NurseryInformationStruct> infoList = new();

                    while (true) {
                        try {
                            Fetch(infoList);

                            if (infoList.Count > 0) {
                                _nurseryView.UpdateProcessInformation(infoList);
                                infoList.Clear();
                                shouldClear = true;
                            } else {
                                if (shouldClear) {
                                    _nurseryView.UpdateProcessInformation(infoList);
                                    shouldClear = false;
                                }
                            }
                            await Task.Delay(updateSpan);
                        } catch (Exception e) {
                            Dogger.Error(e.ToString());
                            await Task.Delay(updateSpan);
                        }
                    }
                }
            );
        }

        public void Flush() {
            try {
                List<NurseryInformationStruct> infoList = new();
                Fetch(infoList);
                if (infoList.Count > 0) _nurseryView.UpdateProcessInformation(infoList);
            } catch (Exception e) {
                Dogger.Error(e.ToString());
            }
        }

        private void Fetch(List<NurseryInformationStruct> list) {
            if (list.Count > 0) {
                list.Clear();
            }

            list.AddRange(_processManager.GetAliveProcesses().
                                          Select(info => new NurseryInformationStruct {
                                              Id = info.Pcs.Id,
                                              ProcessName = info.Pcs.ProcessName,
                                              CPU = info.CpuCounter.NextValue(),
                                              Memory = (int)info.MemCounter.NextValue() >> 10,
                                          }));
        }
    }

}
