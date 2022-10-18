using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;

using FancyToys.Controls.Dialogs;
using FancyToys.Logging;
using FancyToys.Nursery;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace FancyToys.Views {

    public partial class NurseryView {

        /// <summary>
        /// Initialize a new bored process, and create a ToggleSwitch represents this process showing at frontend.
        /// </summary>
        /// <param name="pathName"></param>
        private void Add(string pathName) {
            if (!File.Exists(pathName)) {
                Dogger.Error($"No such file: ${pathName}");
                return;
            }

            NurseryItem item = new(pathName, Path.GetFileName(pathName));
            NurseryProcesses[item.NurseryId] = item;

            ToggleSwitch ts = NewSwitch(item.NurseryId, pathName);
            item.Switch = ts;
            ProcessSwitchList.Items.Add(ts);

            item.OnProcessExited += (i) => {
                DispatcherQueue.TryEnqueue(() => {
                        ts.IsOn = false;
                    }
                );
            };
            Dogger.Trace($"add {item.NurseryId} {pathName}");
        }

        private async void Launch(NurseryItem item) {
            // if (!await Task.Run(item.Launch)) {
            //     return;
            // }
            //
            // _informationManager.run();
            await Task.Run(() => {
                    item.Launch();
                    _informationManager.run();
                }
            );
        }

        private void Stop(NurseryItem item) {
            if (item.IsRunning) {
                Task.Run(item.Stop);
            }
        }

        private async void Remove(int pid) {
            if (!NurseryProcesses.TryGetValue(pid, out NurseryItem item)) return;
            bool confirm = true;

            if (item.Switch.IsOn) {
                confirm &= await MessageDialog.Warn("进程未退出", "继续操作可能丢失工作内容", "仍然退出");
            }
            if (!confirm) return;

            ProcessSwitchList.Items!.Remove(item.Switch);
            NurseryProcesses.Remove(pid);

            // dispose the process instance
            item.Dispose();
        }

        /// <summary>
        /// generate a ToggleSwitch for showing at frontend.
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="pathName"></param>
        /// <returns></returns>
        private ToggleSwitch NewSwitch(int pid, string pathName) {
            string pn = Path.GetFileName(pathName);

            ToggleSwitch twitch = new() {
                IsOn = false,
                Tag = pid,
                FontSize = 14,
                ContextFlyout = NewMenu(pid, pathName),
                OnContent = pn + " is running",
                OffContent = pn + " stopped"
            };
            ToolTipService.SetToolTip(twitch, pathName);

            twitch.Toggled += (sender, e) => {
                if (sender is not ToggleSwitch ts) {
                    Dogger.Debug($"Sender not ToggleSwitch{sender}");
                    return;
                }
                Dogger.Info($"Toggle: {ts.IsOn}");
                if (!NurseryProcesses.TryGetValue(pid, out NurseryItem item)) return;

                if (ts.IsOn) {
                    Launch(item);
                } else {
                    Stop(item);
                }
            };
            // NurseryProcesses[pid].Switch = twitch;

            return twitch;
        }

        private MenuFlyout NewMenu(int pid, string pathName) {
            MenuFlyout menu = new();

            MenuFlyoutItem ai = new() {
                Icon = new FontIcon { Glyph = "\uE723" },
                Tag = pid,
                Text = "参数",
            };
            ai.Click += ArgsButtonClick;

            MenuFlyoutItem ci = new() {
                Icon = new FontIcon { Glyph = "\uE8C8" },
                Tag = pid,
                Text = "复制文件名",
            };

            ci.Click += (s, e) => {
                DataPackage dataPackage = new();
                dataPackage.SetText(pathName);
                Clipboard.SetContent(dataPackage);
            };
            ToolTipService.SetToolTip(ci, pathName);

            MenuFlyoutItem ri = new() {
                Icon = new FontIcon { Glyph = "\uE74D" },
                Tag = pid,
                Text = "删除",
            };

            ri.Click += (s, e) => {
                Remove(pid);
            };

            menu.Items.Add(ai);
            menu.Items.Add(ci);
            menu.Items.Add(ri);

            return menu;
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
            ProcessSwitchList.ItemContainerStyle = style;

            return style;
        }
    }

}
