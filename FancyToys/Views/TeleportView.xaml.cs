using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Timers;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

using CommunityToolkit.WinUI.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MemoryPack;

using FancyToys.Logging;
using FancyToys.Utils;
using FancyToys.Service.Teleport;


// ReSharper disable ConvertToConstant.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Views {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TeleportView {

        public static TeleportView Instance { get; private set; }

        private readonly Messenger _teleportServer;
        private readonly ApplicationDataContainer _dataContainer;
        private ObservableCollection<ClipItem> ClipList = new();

        private Timer _timer;
        private bool _allowSpanClip = true;

        public TeleportView() {
            /*
             * icon:
             * Radar, used for unconnected situation
             * wifi,
             * 
             */
            _dataContainer = ApplicationData.Current.LocalSettings.CreateContainer(nameof(TeleportView), ApplicationDataCreateDisposition.Always);
            InitializeComponent();
            TestConnectionStatusContainer.Child = _DisconnectedIcon;

            _teleportServer = new Messenger() {
                IP = TeleportServerIP,
                Port = TeleportServerPort,
            };

            _teleportServer.OnServerConnected += () => TestConnectionStatusContainer.Child = _ConnectedIcon;
            LoadClipboardHistory();

            _timer = new Timer {
                AutoReset = true,
                Interval = 500,
            };

            _timer.Elapsed += (_, _) => {
                Dogger.Trace("timer elapsed");
                _allowSpanClip = true;
                _timer.Stop();
            };

            Clipboard.ContentChanged += OnClipboardContentChanged;

            Instance = this;
        }

        private async void OnClipboardContentChanged(object o, object o1) {
            if (!AllowClip || !_allowSpanClip) return;
            _allowSpanClip = false;

            ClipItem former = ClipList.Count > 0 ? ClipList[0] : null;
            ClipItem newItem = await CreateContent(Clipboard.GetContent());

            // set clip content failed
            if (newItem is null) {
                Dogger.Trace("Invalid clip item");
                return;
            }

            // check whether this content is equal with former's.
            if (!AllowSimilarWithFormer && former is not null && former.Equals(newItem)) {
                Dogger.Debug("Content is equal with former");
                return;
            }

            _timer.Start();
            ClipList.Insert(0, newItem);
        }

        private async void LoadClipboardHistory() {
            if (LoadSystemClipboard) {
                // load from system clipboard list
                ClipboardHistoryItemsResult list = await Clipboard.GetHistoryItemsAsync();

                foreach (ClipboardHistoryItem item in list.Items) {
                    ClipItem clip = await CreateContent(item.Content);

                    if (clip is not null) {
                        ClipList.Add(clip);
                    }
                }

                Dogger.Info($"Load {list.Items.Count} items from system clipboard.");
            } else {
                // load serialized clipboard items CreateFileAsync("clipboards.cache", CreationCollisionOption.ReplaceExisting);
                if (!await ApplicationData.Current.LocalFolder.FileExistsAsync("clipboards.cache")) {
                    Dogger.Warn("Cache file not exist.");
                    return;
                }

                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("clipboards.cache");
                IBuffer buf = await FileIO.ReadBufferAsync(file);
                byte[] clipAsh = buf.ToArray();

                if (clipAsh is null || clipAsh.Length == 0) {
                    Dogger.Warn("Load clipboard items failed: ash is null or empty.");
                    return;
                }

                try {
                    var list = await MemoryPackSerializer.DeserializeAsync<List<ClipItemStruct>>(new MemoryStream(clipAsh));

                    if (list is null || list.Count == 0) {
                        Dogger.Info("Clipboard list is empty.");
                        return;
                    }

                    foreach (ClipItemStruct cis in list) {
                        Dogger.Debug(cis.ToString());
                        ClipItem clip = await CreateContent(cis);

                        if (clip is not null) {
                            ClipList.Add(clip);
                        }
                    }
                    Dogger.Info($"Load {list.Count} serialized clipboard items.");
                } catch (Exception e) {
                    Dogger.Fatal($"Error deserializing clip items: {e.Message}");
                }
            }
        }

        public async Task OnClosing() {
            ClipItemStruct[] list = await Task.WhenAll(ClipList.Select(item => item.GetStruct()));
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("clipboards.cache", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(file, MemoryPackSerializer.Serialize(list.ToList()));
            Dogger.Info($"Saved {list.Length} clip items.");
        }

        private async Task<ClipItem> CreateContent(object content) {
            try {
                ClipItem newer = new() {
                    TeleportServer = _teleportServer
                };

                bool success;

                switch (content) {
                    case DataPackageView package:
                        success = await newer.SetContent(package);
                        break;
                    case ClipItemStruct cis:
                        success = await newer.SetContent(cis);
                        break;
                    default:
                        return null;
                }

                if (!success) {
                    return null;
                }

                newer.OnDelete += (item) => {
                    ClipList.Remove(item);
                };

                return newer;
            } catch (Exception e) {
                Dogger.Error($"Error adding clipboard item: {e.Message}");
                return null;
            }
        }

        // CommandBar's Button events
        // --------------------------------------------------------------------------------------------

        private void PinButton_OnClick(object sender, RoutedEventArgs e) {
            bool allPined = ClipListView.SelectedItems.Cast<ClipItem>().Aggregate(true, (current, selectedItem) => current & selectedItem.Pinned);

            foreach (ClipItem selectedItem in ClipListView.SelectedItems) {
                selectedItem.Pinned = !allPined;
            }
        }

        private void SendButton_OnClick(object sender, RoutedEventArgs e) {
            foreach (ClipItem selectedItem in ClipListView.SelectedItems) {
                selectedItem.SendClipContent();
            }
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e) {
            if (ClipListView.SelectedItems.First() is not ClipItem first) {
                return;
            }
            first.CopyToClipboard();
        }

        private void DeleteButton_OnClick(object sender, RoutedEventArgs e) {
            List<ClipItem> selectedItems = ClipListView.SelectedItems.Cast<ClipItem>().ToList();
            selectedItems.ForEach(item => ClipList.Remove(item));
        }

        // CommandBar's secondary switch button events
        // --------------------------------------------------------------------------------------------

        private void ClearButton_OnClick(object sender, RoutedEventArgs e) {
            List<ClipItem> removeList = ClipList.Where(clipListItem => !clipListItem.Pinned).ToList();
            removeList.ForEach((item => ClipList.Remove(item)));
        }

        // TeleportServer settings' events
        // --------------------------------------------------------------------------------------------

        private async void TestConnectionButton_OnClick(object sender, RoutedEventArgs e) {
            TestConnectionStatusContainer.Child = _ConnectingIcon;

            bool connected = await _teleportServer.TestConnection();
            TestConnectionStatusContainer.Child = connected ? _ConnectedIcon : _DisconnectedIcon;
        }

        private void IPAddressTextBox_OnTextChanged(object sender, TextChangedEventArgs e) {
            TestConnectionStatusContainer.Child = _DisconnectedIcon;
        }

        private void PortNumberBox_OnValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) {
            TestConnectionStatusContainer.Child = _DisconnectedIcon;
        }
    }

}
