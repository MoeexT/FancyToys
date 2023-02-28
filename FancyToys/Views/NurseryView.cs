using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using FancyToys.Logging;
using FancyToys.Service.Nursery;

using MemoryPack;


namespace FancyToys.Views {

    public partial class NurseryView {

        public void UpdateProcessInformation() {
            DispatcherQueue.TryEnqueue(() => {
                ProcessInfoList.Clear();
            });
        }

        public void UpdateProcessInformation(Dictionary<int, ProcessStatistic> alivePs) {
            DispatcherQueue.TryEnqueue(() => {
                var deleteList = new List<ProcessStatistic>();

                // update
                foreach (ProcessStatistic pi in ProcessInfoList) {
                    if (alivePs.TryGetValue(pi.GetNurseryId(), out ProcessStatistic ps)) {
                        pi.SetCPU(ps.cpu);
                        pi.SetMemory(ps.memory);
                        alivePs.Remove(pi.GetNurseryId());
                    } else {
                        deleteList.Add(pi);
                    }
                }

                // delete
                foreach (ProcessStatistic rp in deleteList) {
                    ProcessInfoList.Remove(rp);
                }

                // insert
                foreach (ProcessStatistic si in alivePs.Values) {
                    ProcessInfoList.Add(si);
                }
            });
        }

        public void OnClosing() {
            List<NurseryItemStruct> list = NurseryList.Select(item => item.GetStruct()).ToList();
            _settingContainer.Values["NurseryItemsBytes"] = MemoryPackSerializer.Serialize(list);
            Dogger.Info($"Saved {list.Count} nursery items.");
        }

        private async void OnLoaded() {
            byte[] nurseryAsh = (byte[])_settingContainer.Values["NurseryItemsBytes"];

            if (nurseryAsh is null || nurseryAsh.Length == 0) {
                Dogger.Warn("Load nursery items failed: ash is null or empty.");
                return;
            }

            List<NurseryItemStruct> items;

            try {
                items = await MemoryPackSerializer.DeserializeAsync<List<NurseryItemStruct>>(new MemoryStream(nurseryAsh));
            } catch (Exception e) {
                Dogger.Fatal($"Error loading nursery items: {e.Message}");
                items = null;
            }

            if (items is null) {
                Dogger.Error("Load nursery items failed: deserialize failed");
                return;
            }

            items.ForEach(nis => {
                if (!nis.IsAlive || !AddProcess(nis.NurseryId, nis.FilePath, nis.Arguments)) {
                    AddFile(nis.FilePath, nis.Arguments);
                }
            });

            Dogger.Info($"Load {items.Count} items.");
        }

        /// <summary>
        /// Initialize a new bored process, and create a ToggleSwitch represents this process showing at frontend.
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="arg">arguments while executing</param>
        private void AddFile(string pathName, string arg = "") {
            if (!File.Exists(pathName)) {
                Dogger.Error($"File not exist: {pathName}");
                return;
            }
            NurseryItem item = new(pathName, arg);

            NurseryList.Add(item);
            item.OnProcessLaunched += (_) => _informationManager.run();
            item.OnItemDeleted += (ni) => NurseryList.Remove(ni);
            Dogger.Info($"File path {pathName} add {NurseryList.Count}");
        }

        private bool AddProcess(int pid, string filePath = "", string arguments = "") {
            try {
                Process process = Process.GetProcessById(pid);
                NurseryItem item = new(process);
                NurseryList.Add(item);
                _informationManager.run();
                item.OnItemDeleted += (ni) => NurseryList.Remove(ni);

                if (File.Exists(filePath)) {
                    item.FilePath = filePath;
                    item.Arguments = arguments;
                    item.OnProcessLaunched += (_) => _informationManager.run();
                }

                Dogger.Info($"Alive process \"{item}\" add.");

                return true;
            } catch (Exception e) {
                Dogger.Error($"The process specified by PID({pid}) doesn't exist: {e.Message}");
                return false;
            }
        }

        private void SortData(Comparison<ProcessStatistic> comparison) {
            var sortableList = new List<ProcessStatistic>(ProcessInfoList);
            sortableList.Sort(comparison);
            ProcessInfoList.Clear();

            foreach (ProcessStatistic pi in sortableList) {
                ProcessInfoList.Add(pi);
            }
        }

        /// <summary>
        /// 动态改变switchToggle的样式
        /// From https://blog.csdn.net/lindexi_gd/article/details/104992276
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private Style SetStyle(DependencyProperty property, object value) {
            Style style = new() {
                TargetType = typeof(ListBoxItem)
            };
            style.Setters.Add(new Setter(property, value));
            style.Setters.Add(new Setter(Control.PaddingProperty, "10,0,0,0"));
            NurseryListView.ItemContainerStyle = style;

            return style;
        }
    }

}
