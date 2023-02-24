using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;

using FancyToys.Logging;
using FancyToys.Utils;

using Microsoft.UI;
using Microsoft.UI.Xaml.Input;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Controls {

    public sealed partial class ClipListItem {
        public enum ClipType {
            Text = 0,
            Image = 1,
            File = 2,
            Uri = 3,
        }

        public delegate void OnDeleteButtonClicked(ClipListItem item);
        public event OnDeleteButtonClicked OnDelete;

        public Messenger TeleportServer;

        private bool _pinned;
        private ClipType _contentType;
        private TextBlock _textBlock;
        
        private Uri _clipUri;
        private string _clipText;
        private RandomAccessStreamReference _clipImageStream;
        private IReadOnlyList<IStorageItem> _clipStorageItems;
        
        
        public Uri ClipUri {
            get => _clipUri;
            set {
                _clipUri = value;
                ContentType = ClipType.Uri;
            }
        }

        public string ClipText {
            get => _clipText;
            set {
                _clipText = value;
                ContentType = ClipType.Text;
            }
        }

        public RandomAccessStreamReference ClipImageStream {
            get => _clipImageStream;
            set {
                _clipImageStream = value;
                ContentType = ClipType.Image;
            }
        }

        public IReadOnlyList<IStorageItem> ClipStorageItems {
            get => _clipStorageItems;
            set {
                _clipStorageItems = value;
                ContentType = ClipType.File;
            }
        }

        public ClipType ContentType {
            get => _contentType;
            private set {
                switch (value) {
                    case ClipType.Image:
                        ClipTypeIcon.Glyph = "\uEB9F";
                        break;
                    case ClipType.File:
                        ClipTypeIcon.Glyph = "\uE8A5"; // ED43
                        break;
                    case ClipType.Uri:
                        bool isMail = _clipUri.Scheme.Equals(Uri.UriSchemeMailto);
                        ClipTypeIcon.Glyph = isMail ? "\uE715" : "\uF6FA"; // E774
                        break;
                    case ClipType.Text:
                    default:
                        ClipTypeIcon.Glyph = null;
                        break;
                }
                _contentType = value;
            }
        }

        public bool Pinned {
            get => _pinned;
            set {
                _pinned = value;
                DeleteBarButton.IsEnabled = !value;
                PinButtonIcon.Glyph = value ? "\uE840" : "\uE718";
                ClipItemBorder.BorderBrush = value ? new SolidColorBrush(Color.FromArgb(0xff, 0x76, 0x56, 0x5d)) : new SolidColorBrush();
                PinBarButton.Background = value ? new SolidColorBrush(Color.FromArgb(0xff, 0x3c, 0x3c, 0x3c)) : new SolidColorBrush();

            }
        }

        private readonly FontIcon _SendSuccessIcon = new() {
            Glyph = "\xE89C",
            FontSize = 16,
            Foreground = new SolidColorBrush(Colors.LightGreen),
        };
        private readonly FontIcon _SendFailedIcon = new() {
            Glyph = "\xE73E",
            FontSize = 16,
            Foreground = new SolidColorBrush(Colors.OrangeRed),
        };
        private readonly FontIcon _CopySuccessIcon = new() {
            Glyph = "\xE73E",
            FontSize = 20,
            Foreground = new SolidColorBrush(Colors.LightGreen),
        };
        private readonly ProgressRing _ProgressRing = new() {
            Width = 16,
            Height = 16,
        };

        private TextDecorations TextDecoration {
            get => _textBlock?.TextDecorations ?? TextDecorations.None;
            set {
                // https://github.com/microsoft/microsoft-ui-xaml/issues/1093#issuecomment-514282402
                if (_textBlock is null) return;
        
                if (value == TextDecorations.None) {
                    _textBlock.Text = string.Empty;
                    _textBlock.TextDecorations = value;
                    _textBlock.Text = ClipText;
                } else {
                    _textBlock.TextDecorations = value;
                }
            }
        }

        public ClipListItem() {
            InitializeComponent();
            DateTimeBlock.Text = $"{DateTime.Now:u}";
        }

        private void PinButton_Click(object sender, RoutedEventArgs e) {
            Pinned = !Pinned;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e) {
            CopyToClipboard();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) {
            OnDelete?.Invoke(this);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) {
            SendClipContent();
        }

        private void PlaceHolderForRightClick_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            CopyToClipboard();
        }

        public async void CopyToClipboard() {
            DataPackage package = new();

            switch (ContentType) {
                case ClipType.File:
                    foreach (IStorageItem item in ClipStorageItems) {
                        Dogger.Debug(Path.Join(item.Path, item.Name));
                    }
                    package.SetStorageItems(ClipStorageItems);
                    package.RequestedOperation = DataPackageOperation.Copy;
                    break;
                case ClipType.Image:
                    package.SetBitmap(ClipImageStream);
                    break;
                case ClipType.Uri:
                case ClipType.Text:
                default:
                    package.SetText(ClipText);
                    break;
            }
            Clipboard.SetContent(package);

            // show a check-mark telling user that copied content successfully
            StateIconBorder.Child = _CopySuccessIcon;
            StateIconBorder.Visibility = Visibility.Visible;
            await Task.Delay(1000);

            await Task.Run(() => {
                DispatcherQueue.TryEnqueue(() => {
                    StateIconBorder.Visibility = Visibility.Collapsed;
                });
            });
        }

        public async void SendClipContent() {
            if (TeleportServer is null) {
                Dogger.Error("Messenger:TeleportServer haven't been set.");
                return;
            }
            StateIconBorder.Child = _ProgressRing;
            StateIconBorder.Visibility = Visibility.Visible;

            bool success;

            switch (ContentType) {
                case ClipType.Image: {
                    using IRandomAccessStreamWithContentType stream = await ClipImageStream.OpenReadAsync();
                    success = await TeleportServer.WriteStream(stream);
                    break;
                }
                case ClipType.File:
                    // success = await TeleportServer.WriteStorageItems(ClipStorageItems);
                    StringBuilder sb = new();
                    foreach (IStorageItem storageItem in ClipStorageItems) {
                        sb.Append(storageItem.Path);
                    }
                    success = await TeleportServer.WriteString(sb.ToString());
                    break;
                case ClipType.Uri:
                    success = await TeleportServer.WriteString(_clipUri.ToString());
                    break;
                case ClipType.Text:
                default:
                    success = await TeleportServer.WriteString(ClipText);
                    break;
            }

            if (success) {
                Dogger.Debug($"Send clip item successfully");
            } else {
                Dogger.Warn($"Send clip item failed.");
            }

            StateIconBorder.Child = success ? _SendSuccessIcon : _SendFailedIcon;

#pragma warning disable CS4014
            Task.Delay(1000)
                .ContinueWith((_) => {
#pragma warning restore CS4014
                    DispatcherQueue.TryEnqueue(() => {
                        StateIconBorder.Visibility = Visibility.Collapsed;
                    });
                });
        }

        public new bool Equals(object obj) {
            if (obj is not ClipListItem another) {
                Dogger.Info(obj.ToString());
                return false;
            }

            if (another.ContentType != ContentType) {
                Dogger.Info(another.ContentType.ToString());
                return false;
            }

            Dogger.Info($"{this.ToString()}, {another.ToString()}");

            return ContentType switch {
                ClipType.File => ((Func<bool>)(() => {
                    for (int i = 0; i < ClipStorageItems.Count; i++) {
                        IStorageItem item1 = ClipStorageItems[i];
                        IStorageItem item2 = another.ClipStorageItems[i];

                        if (!Equals(item1.Attributes, item2.Attributes)
                            || !string.Equals(item1.Path, item2.Path, StringComparison.Ordinal)
                            || !item1.DateCreated.Equals(item2.DateCreated)) {
                            return false;
                        }
                    }
                    return true;
                }))(),
                ClipType.Text => ClipText.Equals(another.ClipText),
                ClipType.Uri => ClipUri.Equals(another.ClipUri),
                ClipType.Image => false,
                _ => false,
            };
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public new string ToString() {
            string actualContent = ContentType switch {
                ClipType.File => ((Func<string>)(() => {
                    if (ClipStorageItems is null) {
                        return "null";
                    }
                    StringBuilder sb = new("[");

                    foreach (IStorageItem item in ClipStorageItems) {
                        sb.Append('\t');
                        sb.AppendLine(item.Path);
                    }

                    sb.Append(']');
                    return sb.ToString();
                }))(),
                ClipType.Text => ClipText,
                ClipType.Uri => ClipUri.ToString(),
                ClipType.Image => ClipImageStream?.ToString(),
                _ => string.Empty,
            };
            StringBuilder sb = new();

            sb.Append(((Func<string>)(() => {
                if (ClipStorageItems is null) {
                    return "files: null; \n";
                }
                StringBuilder s = new("files: [");

                foreach (IStorageItem item in ClipStorageItems) {
                    s.Append('\t');
                    s.AppendLine(item.Path);
                }
                s.Append(']');
                return s.ToString();
            }))());
            sb.AppendLine(ClipText);
            sb.AppendLine(ClipUri?.ToString());
            sb.Append(ClipImageStream?.ToString());

            return $"$ClipListItem {{ Type: {ContentType}, ActualContent: {actualContent}\nContent: \"{sb.ToString()}\" }}";
        }
    }

}
