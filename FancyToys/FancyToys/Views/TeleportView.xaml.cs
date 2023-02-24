using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using FancyToys.Controls;
using FancyToys.Logging;
using FancyToys.Utils;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Views {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TeleportView: Page {

        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        private ObservableCollection<ClipListItem> ClipList;
        private bool _allowClip;
        private bool _allowSimilarWithFormer;
        private readonly Messenger _teleportServer;

        private static readonly FontIcon _ConnectedIcon = new() {
            Glyph = "\xF0B9",
            FontSize = 18,
            Foreground = new SolidColorBrush(Colors.LightGreen),
        };
        private static readonly FontIcon _DisconnectedIcon = new() {
            Glyph = "\xF384",
            FontSize = 18,
            Foreground = new SolidColorBrush(Colors.Red),
        };
        private static readonly ProgressRing _ConnectingIcon = new() {
            Width = 25,
            Height = 25,
            IsActive = true,
        };

        private string TeleportServerIP {
            get => (string)ApplicationData.Current.LocalSettings.Values[nameof(TeleportServerIP)] ?? string.Empty;
            set {
                _teleportServer.IP = value;
                ApplicationData.Current.LocalSettings.Values[nameof(TeleportServerIP)] = value;
            }
        }

        private int TeleportServerPort {
            get => (int)(ApplicationData.Current.LocalSettings.Values[nameof(TeleportServerPort)] ?? 0);
            set {
                _teleportServer.Port = value;
                ApplicationData.Current.LocalSettings.Values[nameof(TeleportServerPort)] = value;
            }
        }

        public TeleportView() {
            /*
             * icon:
             * Radar, used for unconnected situation
             * wifi,
             * 
             */
            InitializeComponent();

            _allowClip = true;
            bool allowSpanClip = true;
            ClipList = new ObservableCollection<ClipListItem>();
            TestConnectionStatusContainer.Child = _DisconnectedIcon;

            _teleportServer = new Messenger() {
                IP = TeleportServerIP,
                Port = TeleportServerPort,
            };

            _teleportServer.OnServerConnected += () => {
                TestConnectionStatusContainer.Child = _ConnectedIcon;
            };
            LoadClipboardHistory();

            Timer timer = new() {
                AutoReset = true,
                Interval = 500,
            };

            timer.Elapsed += (_, _) => {
                Dogger.Trace("timer elapsed");
                allowSpanClip = true;
                timer.Stop();
            };

            Clipboard.ContentChanged += async (_, _) => {
                if (!_allowClip || !allowSpanClip) return;

                ClipListItem former = ClipList[0];
                ClipListItem newItem = await CreateContent(Clipboard.GetContent());

                // set clip content failed
                if (newItem is null) {
                    Dogger.Trace("invalid clip item");
                    return;
                }

                // check whether this content is equal with former's.
                if (!_allowSimilarWithFormer && former is not null && former.Equals(newItem)) {
                    Dogger.Debug("equal with former");
                    return;
                }

                timer.Start();
                allowSpanClip = false;
                ClipList.Insert(0, newItem);
            };
        }

        private async void LoadClipboardHistory() {
            ClipboardHistoryItemsResult list = await Clipboard.GetHistoryItemsAsync();

            foreach (ClipboardHistoryItem item in list.Items) {
                ClipList.Add(await CreateContent(item.Content));
            }
        }

        private async Task<ClipListItem> CreateContent(DataPackageView package) {
            ClipListItem newer = new() {
                TeleportServer = _teleportServer
            };

            if (!await newer.SetContent(package)) {
                return null;
            }
            
            newer.OnDelete += (item) => {
                ClipList.Remove(item);
            };

            return newer;
        }

        // CommandBar's Button events
        // --------------------------------------------------------------------------------------------

        private void PinButton_OnClick(object sender, RoutedEventArgs e) {
            bool allPined = ClipListView.SelectedItems.Cast<ClipListItem>().Aggregate(true, (current, selectedItem) => current & selectedItem.Pinned);

            foreach (ClipListItem selectedItem in ClipListView.SelectedItems) {
                selectedItem.Pinned = !allPined;
            }
        }

        private void SendButton_OnClick(object sender, RoutedEventArgs e) {
            foreach (ClipListItem selectedItem in ClipListView.SelectedItems) {
                selectedItem.SendClipContent();
            }
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e) {
            if (ClipListView.SelectedItems.First() is not ClipListItem first) {
                return;
            }
            first.CopyToClipboard();
        }

        private void DeleteButton_OnClick(object sender, RoutedEventArgs e) {
            List<ClipListItem> selectedItems = ClipListView.SelectedItems.Cast<ClipListItem>().ToList();
            selectedItems.ForEach(item => ClipList.Remove(item));
        }

        // CommandBar's secondary switch button events
        // --------------------------------------------------------------------------------------------

        private void ListenSwitch_OnToggled(object sender, RoutedEventArgs e) {
            if (sender is not AppBarToggleButton ts) return;
            _allowClip = ts.IsChecked ?? false;
        }

        private void CheckSimilarSwitch_OnToggled(object sender, RoutedEventArgs e) {
            if (sender is not AppBarToggleButton tb) return;
            _allowSimilarWithFormer = tb.IsChecked ?? false;
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e) {
            List<ClipListItem> removeList = ClipList.Where(clipListItem => !clipListItem.Pinned).ToList();
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
