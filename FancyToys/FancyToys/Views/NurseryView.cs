using System;
using System.Collections.Generic;
using System.Diagnostics;

using FancyToys.Logging;
using FancyToys.Service.Nursery;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace FancyToys.Views {

    public partial class NurseryView {

        /// <summary>
        /// Initialize a new bored process, and create a ToggleSwitch represents this process showing at frontend.
        /// </summary>
        /// <param name="pathName"></param>
        private void AddFile(string pathName) {
            if (!NurseryItem.WithPath(pathName, out NurseryItem item)) {
                return;
            }

            item.OnProcessLaunched += (_) => {
                _informationManager.run();
            };
            NurseryList.Add(item);
            Dogger.Info($"File path {pathName} add {NurseryList.Count}");
        }

        private void AddProcess(Process process) {
            if (!NurseryItem.WithProcess(process, out NurseryItem item)) {
                return;
            }
            _informationManager.run();
            NurseryList.Add(item);
            Dogger.Info($"Alive process \"{process.ProcessName}\" add.");
        }

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
