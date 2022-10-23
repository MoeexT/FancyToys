using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI.Text;

using FancyToys.Controls;
using FancyToys.Logging;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Views {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TeleportView: Page {

        private ObservableCollection<ClipListItem> ClipList;
        private Timer _timer;
        private bool _allowSpanClip;
        private bool _allowClip;

        public bool AllowSimilarWithFormer { get; set; } = true;

        public TeleportView() {
            InitializeComponent();

            ClipList = new ObservableCollection<ClipListItem>();

            _timer = new Timer {
                AutoReset = true,
                Interval = 500,
            };

            _timer.Elapsed += (o, s) => {
                Debug.WriteLine("timer elapsed");
                _allowSpanClip = true;
                _timer.Stop();
            };
            _allowSpanClip = true;
            LoadClipboardHistory();

            Clipboard.ContentChanged += (s, e) => {
                if (!_allowClip) return;
                // TcpBridge client = new();
                // SetContent(Clipboard.GetContent());
                Debug.WriteLine("-------------Clipboard.ContentChanged------------------");
                _timer.Start();
                CreateContent(Clipboard.GetContent(), true);
                _allowSpanClip = false;
            };
        }

        private async void LoadClipboardHistory() {
            ClipboardHistoryItemsResult list = await Clipboard.GetHistoryItemsAsync();

            foreach (ClipboardHistoryItem item in list.Items) {
                CreateContent(item.Content);
            }
        }

        private async void CreateContent(DataPackageView package, bool userClip = false) {
            ClipListItem newer = null;

            if (userClip) {
                // if (!_allowSpanClip) { return; }

                // ClipListItem former = ClipList.Count > 0 ? ClipList[0] : null;
                //
                // // check if former-text is same as new-text
                // if (!AllowSimilarWithFormer
                //     && former is not null
                //     && former.ContentType == ClipListItem.ClipType.Text
                //     && (newer = new ClipListItem(package)).ContentType == ClipListItem.ClipType.Text) {
                //     return;
                // }
                // newer ??= new ClipListItem(package);
                newer = new ClipListItem(package);
                newer.OnDelete += (item) => {
                    ClipList.Remove(item);
                };
                ClipList.Insert(0, newer);
            } else {
                newer = new ClipListItem(package);
                newer.OnDelete += (item) => {
                    ClipList.Remove(item);
                };
                ClipList.Add(newer);
            }


        }

        private void ListenSwitch_OnToggled(object sender, RoutedEventArgs e) {
            if (sender is not ToggleSwitch ts) return;
            _allowClip = ts.IsOn;
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e) {
            List<ClipListItem> removeList = ClipList.Where(clipListItem => !clipListItem.Pinned).ToList();
            removeList.ForEach((item => ClipList.Remove(item)));
        }

        private void UnderLineButton_OnClick(object sender, RoutedEventArgs e) {
            if (sender is not ToggleButton tb) return;

            foreach (ClipListItem item in ClipList) {
                item.TextDecoration = tb.IsChecked == true ? TextDecorations.Underline : TextDecorations.None;
            }
        }
    }

}
