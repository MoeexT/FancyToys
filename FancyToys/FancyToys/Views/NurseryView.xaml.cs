using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

using FancyToys.Controls.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using CommunityToolkit.WinUI.UI.Controls;

using FancyToys.Logging;
using FancyToys.Nursery;
using WinRT.Interop;
using System.Diagnostics;



// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板


namespace FancyToys.Views {

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NurseryView: Page {
        // 进程资源信息
        private ObservableCollection<ProcessStatistic> ProcessInfoList { get; }
        // 进程控件信息
        public Dictionary<int, NurseryItem> NurseryProcesses { get; }
        
        private readonly InformationManager _informationManager;

        public NurseryView() {
            InitializeComponent();
            ProcessInfoList = new ObservableCollection<ProcessStatistic>();
            NurseryProcesses = new Dictionary<int, NurseryItem>();
            _informationManager = new InformationManager(this);
        }

        private async void DropAreaDrop(object sender, DragEventArgs e) {
            DragOperationDeferral defer = e.GetDeferral();

            try {
                DataPackageView dpv = e.DataView;

                if (!dpv.Contains(StandardDataFormats.StorageItems)) return;

                IReadOnlyList<IStorageItem> files = await dpv.GetStorageItemsAsync();

                foreach (IStorageItem item in files) {
                    if (item.Name.EndsWith(".exe")) {
                        Add(item.Path);
                    }
                }
            } finally {
                defer.Complete();
            }
        }

        private void DropAreaDragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride!.Caption = "拖放以添加";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            e.Handled = true;
        }

        private async void AddFileFlyoutItemClick(object sender, RoutedEventArgs e) {
            var hwnd = WindowNative.GetWindowHandle(MainWindow.CurrentWindow);
            FileOpenPicker picker = new() {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.HomeGroup
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".exe");
            //((IInitializeWithWindow)(object)picker).Initialize(Process.GetCurrentProcess().MainWindowHandle);
            StorageFile file = await picker.PickSingleFileAsync();

            // TODO: 可能选择多个文件
            if (file != null) {
                Add(file.Path);
            }
        }

        private void StopAllFlyoutItemClick(object sender, RoutedEventArgs e) {
            if (ProcessSwitchList.Items == null) return;

            foreach (ToggleSwitch ts in ProcessSwitchList.Items) {
                if (ts.IsOn) {
                    NurseryItem ni = NurseryProcesses[(int)ts.Tag];
                    ni.Stop();
                }
            }
        }

        private void RemoveAllFlyoutItemClick(object sender, RoutedEventArgs e) {
            if (ProcessSwitchList.Items == null) return;

            foreach (ToggleSwitch ts in ProcessSwitchList.Items) {
                Remove((int)ts.Tag);
            }
        }

        private void HelpFlyoutItemClick(object sender, RoutedEventArgs e) { DropFileTechingTip.IsOpen = true; }

        private void AboutFlyoutItemClick(object sender, RoutedEventArgs e) {
            _ = MessageDialog.Info("Nursery v0.1.3",
                "Nursery is a simple daemon process manager powered by FancyServer and it will keep your application online.");
        }

        private async void ArgsButtonClick(object sender, RoutedEventArgs e) {
            if (sender is not MenuFlyoutItem ai) {
                Dogger.Error("args-button is null");
                return;
            }

            int pid = (int)ai.Tag;
            NurseryItem ni = NurseryProcesses[pid];
            
            InputDialog inputDialog = new("Nursery", "输入参数", ni.Ps.StartInfo.Arguments) {
                XamlRoot = this.XamlRoot,
            };
            // Value does not fall within the expected range
            await inputDialog.ShowAsync();

            if (inputDialog.isSaved) {
                ni.Ps.StartInfo.Arguments = inputDialog.inputContent;
            }
        }

        private void ListBoxSizeClick(object sender, RoutedEventArgs e) {
            ProcessSwitchList.ItemContainerStyle = SetStyle(FrameworkElement.HeightProperty, ((MenuFlyoutItem)sender).Tag);
        }

        private void DataGridSizeClick(object sender, RoutedEventArgs e) { }

        private void FlushSpeedClick(object sender, RoutedEventArgs e) {
            // SettingsClerk.Clerk.STFlushTime = int.Parse((sender as MenuFlyoutItem).Tag as string);
            Dogger.Warn("this feature is not implemented yet");
        }

        private void ProcessGridSorting(object sender, DataGridColumnEventArgs e) {
            switch (e.Column.Header.ToString()) {
                case "Process":
                    if (e.Column.SortDirection is null or DataGridSortDirection.Descending) {
                        SortData((x, y) => string.Compare(x.Process, y.Process, StringComparison.Ordinal));
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    } else {
                        SortData((x, y) => -string.Compare(x.Process, y.Process, StringComparison.Ordinal));
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }
                    break;
                case "PID":
                    if (e.Column.SortDirection is null or DataGridSortDirection.Descending) {
                        SortData((x, y) => x.PID - y.PID);
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    } else {
                        SortData((x, y) => y.PID - x.PID);
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }
                    break;
                case "CPU":
                    if (e.Column.SortDirection is null or DataGridSortDirection.Descending) {
                        SortData((x, y) => (int)(x.cpu - y.cpu));
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    } else {
                        SortData((x, y) => (int)(y.cpu - x.cpu));
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }
                    break;
                case "Memory":
                    if (e.Column.SortDirection is null or DataGridSortDirection.Descending) {
                        SortData((x, y) => (int)(x.memory - y.memory));
                        e.Column.SortDirection = DataGridSortDirection.Ascending;
                    } else {
                        SortData((x, y) => (int)(y.memory - x.memory));
                        e.Column.SortDirection = DataGridSortDirection.Descending;
                    }
                    break;
            }

            foreach (DataGridColumn dc in ProcessGrid.Columns) {
                if (dc.Header.ToString() != e.Column.Header.ToString()) {
                    dc.SortDirection = null;
                }
            }
        }

    }

}
