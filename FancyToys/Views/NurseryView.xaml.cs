using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

using WinRT.Interop;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using CommunityToolkit.WinUI.UI.Controls;

using FancyToys.Controls.Dialogs;
using FancyToys.Service.Nursery;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板


namespace FancyToys.Views {

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NurseryView: Page {

        // 进程控件信息
        public ObservableCollection<NurseryItem> NurseryList { get; }

        // 进程资源信息
        private ObservableCollection<ProcessStatistic> ProcessInfoList { get; }

        private readonly InformationManager _informationManager;
        private readonly ApplicationDataContainer _settingContainer;

        public static NurseryView Instance { get; private set; }

        public NurseryView() {
            InitializeComponent();
            NurseryList = new ObservableCollection<NurseryItem>();
            ProcessInfoList = new ObservableCollection<ProcessStatistic>();
            _informationManager = new InformationManager(this);
            _settingContainer = ApplicationData.Current.LocalSettings.CreateContainer(nameof(TeleportView), ApplicationDataCreateDisposition.Always);
            Instance = this;
            OnLoaded();
        }

        private async void DropAreaDrop(object sender, DragEventArgs e) {
            DragOperationDeferral defer = e.GetDeferral();

            try {
                DataPackageView dpv = e.DataView;

                if (!dpv.Contains(StandardDataFormats.StorageItems)) return;

                IReadOnlyList<IStorageItem> files = await dpv.GetStorageItemsAsync();

                foreach (IStorageItem item in files) {
                    if (item.Name.EndsWith(".exe")) {
                        AddFile(item.Path);
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
            IntPtr hwnd = WindowNative.GetWindowHandle(MainWindow.CurrentWindow);

            FileOpenPicker picker = new() {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.HomeGroup
            };

            InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".exe");
            StorageFile file = await picker.PickSingleFileAsync();

            // TODO: 可能选择多个文件
            if (file != null) {
                DispatcherQueue.TryEnqueue(() => {
                    AddFile(file.Path);
                });
            }
        }

        private void StopAllFlyoutItemClick(object sender, RoutedEventArgs e) {
            foreach (NurseryItem nurseryItem in NurseryList) {
                nurseryItem.Stop();
            }
        }

        private async void RemoveAllFlyoutItemClick(object sender, RoutedEventArgs e) {
            NurseryList.ToList().ForEach(async (nurseryItem) => await nurseryItem.Delete());
        }

        private async void SeizeProcessFlyoutItemClick(object sender, RoutedEventArgs e) {
            InputDialog inputDialog = new("Nursery", "输入要捕获的进程id") {
                XamlRoot = XamlRoot,
            };
            await inputDialog.ShowAsync();

            if (!inputDialog.isSaved) {
                return;
            }

            string content = inputDialog.inputContent.Trim();

            MessageDialog dialog;

            if (!int.TryParse(content, out int pid)) {
                dialog = new MessageDialog("Nursery", "Invalid PID", "Ok", MessageDialog.MessageLevel.Notice) {
                    XamlRoot = XamlRoot,
                };
                await dialog.ShowAsync();
                return;
            }

            if (AddProcess(pid)) return;

            dialog = new MessageDialog("Nursery", "Invalid PID, the process doesn't exist.", "Ok", MessageDialog.MessageLevel.Notice) {
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
        }

        private void HelpFlyoutItemClick(object sender, RoutedEventArgs e) { DropFileTeachingTip.IsOpen = true; }

        private async void AboutFlyoutItemClick(object sender, RoutedEventArgs e) {
            // TODO set XamlRoot
            await MessageDialog.Info("Nursery v0.1.3",
                "Nursery is a light daemon powered by .net and it keeps your applications online.");
        }

        private void ListBoxSizeClick(object sender, RoutedEventArgs e) {
            NurseryListView.ItemContainerStyle = SetStyle(FrameworkElement.HeightProperty, ((MenuFlyoutItem)sender).Tag);
        }

        private void DataGridSizeClick(object sender, RoutedEventArgs e) { }

        private void FlushSpeedClick(object sender, RoutedEventArgs e) {
            if (sender is not MenuFlyoutItem item) {
                return;
            }
            _informationManager.UpdateSpan = (int)item.Tag;
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
