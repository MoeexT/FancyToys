using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;

using FancyToys.Logging;
using FancyToys.Utils;

using Microsoft.UI;


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

        private struct ClipStruct {
            public byte[] offsets;
            public ClipType type;
            public string uri;
            public DateTime createTime;
            public byte[] content;
        }

        public delegate void OnDeleteButtonClicked(ClipListItem item);
        public event OnDeleteButtonClicked OnDelete;

        private bool _pinned;

        private Uri _clipUri;
        private string _clipText;
        private RandomAccessStreamReference _clipImageStream;
        private IReadOnlyList<IStorageItem> _clipStorageItems;

        private ClipType _contentType;
        private TextBlock _textBlock { get; set; }

        public ClipType ContentType {
            get => _contentType;
            private set {
                switch (value) {
                    case ClipType.Image:
                        ClipTypeIcon.Glyph = "\uEB9F";
                        // ToolTipService.SetToolTip(ClipTypeIcon, "Image");
                        break;
                    case ClipType.File:
                        ClipTypeIcon.Glyph = "\uEC50"; // E8A5
                        // ToolTipService.SetToolTip(ClipTypeIcon, "File");
                        break;
                    case ClipType.Uri:
                        bool isMail = _clipUri.Scheme.Equals(Uri.UriSchemeMailto);
                        ClipTypeIcon.Glyph = isMail ? "\uE715" : "\uF6FA"; // E774
                        // ToolTipService.SetToolTip(ClipTypeIcon, isMail ? "Email" : "Uri");
                        break;
                    case ClipType.Text:
                    default:
                        ClipTypeIcon.Glyph = null;
                        // ToolTipService.SetToolTip(ClipTypeIcon, null);
                        // ClipTypeIcon.Glyph = "\uE8A4";
                        // ToolTipService.SetToolTip(ClipTypeIcon, "Text");
                        break;
                }
                _contentType = value;
            }
        }

        public TextDecorations TextDecoration {
            get => _textBlock?.TextDecorations ?? TextDecorations.None;
            set {
                // https://github.com/microsoft/microsoft-ui-xaml/issues/1093#issuecomment-514282402
                if (_textBlock is null) return;

                if (value == TextDecorations.None) {
                    _textBlock.Text = string.Empty;
                    _textBlock.TextDecorations = value;
                    _textBlock.Text = _clipText;
                } else {
                    _textBlock.TextDecorations = value;
                }
            }
        }

        public bool Pinned {
            get => _pinned;
            set {
                _pinned = value;
                DeleteBarButton.IsEnabled = !value;
                PinButtonIcon.Glyph = value ? "\uE840" : "\uE718";
                Border.BorderBrush = value ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush();
                PinBarButton.Background = value ? new SolidColorBrush(Color.FromArgb(0xff, 0x3c, 0x3c, 0x3c)) : new SolidColorBrush();
            }
        }

        public ClipListItem(DataPackageView package) {
            InitializeComponent();
            SetContent(package);
            DateTimeBlock.Text = $"{DateTime.Now:u}";
        }

        private async void SetContent(DataPackageView package) {
            Debug.WriteLine(string.Join(", ", package.AvailableFormats));
            // Not support yet: rtf, html,

            if (package.Contains("FileDrop") || (
                package.Contains("Shell IDList Array") &&
                package.Contains("Preferred DropEffect"))) {
                /* 
                 * Shell IDList Array,
                 * DataObjectAttributes,
                 * DataObjectAttributesRequiringElevation,
                 * Shell Object Offsets,
                 * Preferred DropEffect,
                 * AsyncFlag,
                 * FileDrop,
                 * FileName,
                 * FileContents,
                 * FileNameW,
                 * FileGroupDescriptorW
                 */

                StackPanel panel = new();
                _clipStorageItems = await package.GetStorageItemsAsync();

                for (int i = 0; i < _clipStorageItems.Count; i++) {
                    IStorageItem storageItem = _clipStorageItems[i];
                    panel.Children.Add(ClassifyText(storageItem.Path, i + 1));
                }
                ContentType = ClipType.File;
                ClipJar.Children.Insert(0, panel);
            } else if (package.Contains(StandardDataFormats.Bitmap)) {
                ClipJar.Children.Insert(0, await SetImageStream(await package.GetBitmapAsync()));
            } else {
                ClipJar.Children.Insert(0, ClassifyText(await package.GetTextAsync()));
            }
        }

        private FrameworkElement ClassifyText(string path, int counter = 1) {
            if (path is null) {
                return null;
            }
            _clipText = path;
            bool validUri = Uri.TryCreate(path, UriKind.Absolute, out Uri uri);

            // not a uri, set text to ClipItem
            if (!validUri) {
                Dogger.Debug($"invalid uri{path}");
                return CreateTextBlock(path);
            }

            string scheme = uri.Scheme;

            if (scheme.Equals(Uri.UriSchemeFile)) {
                Dogger.Debug($"文件！{path}");

                if (!File.Exists(path) && !Directory.Exists(path)) {
                    return CreateTextBlock(path);
                }

                return CreateHyperlink(ClipType.File, uri, (s, _) => {
                    if (s is not HyperlinkButton) {
                        return;
                    }

                    // https://stackoverflow.com/a/696144/9013632
                    if (File.Exists(path)) {
                        Process.Start("explorer.exe", $"/select, \"{path}\"");
                    }

                    if (Directory.Exists(path)) {
                        Process.Start("explorer.exe", path);
                    }
                }, (counter & 1) == 1);
            }

            if (scheme.Equals(Uri.UriSchemeFtp) || scheme.Equals(Uri.UriSchemeHttp) ||
                scheme.Equals(Uri.UriSchemeHttps)) {
                // internet url, open with browser
                Dogger.Debug($"web link{path}");
                return CreateHyperlink(ClipType.Uri, uri, null);
            }

            if (scheme.Equals(Uri.UriSchemeMailto)) { //  || isEmailAddress
                // email, open with `Email`
                Dogger.Debug($"email uri{path}, {uri.AbsolutePath}, {uri.AbsoluteUri}");

                return CreateHyperlink(ClipType.Uri, uri, (s, _) => {
                    if (s is not HyperlinkButton) {
                        return;
                    }

                    // https://github.com/dotnet/runtime/issues/30303#issuecomment-513210581 
                    Process.Start(new ProcessStartInfo(path) {
                        UseShellExecute = true
                    });
                });
            }

            // unknown uri, set default text
            Dogger.Warn($"unknown uri{path}");
            return CreateTextBlock(path);
        }

        private async Task<Image> SetImageStream(RandomAccessStreamReference streamReference) {
            _clipImageStream = streamReference;
            ContentType = ClipType.Image;
            using IRandomAccessStreamWithContentType stream = await streamReference.OpenReadAsync();
            BitmapImage bi = new();
            bi.SetSource(stream);

            return new Image {
                Source = bi,
                Height = 70,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
        }

        private TextBlock CreateTextBlock(string path) {
            _clipText = path;
            ContentType = ClipType.Text;

            _textBlock = new TextBlock {
                Text = path,
                MaxLines = 3,
                Width = 300,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            _textBlock.PointerEntered += (o, s) => {
                _textBlock.TextDecorations = TextDecorations.Underline;
            };

            _textBlock.PointerExited += (o, s) => {
                TextDecoration = TextDecorations.None;
            };
            return _textBlock;
        }

        private HyperlinkButton CreateHyperlink(ClipType type, Uri uri, RoutedEventHandler handler, bool odd = true) {
            _clipUri = uri;
            ContentType = type;

            HyperlinkButton btn = new() {
                Height = 30,
                Width = 300,
                NavigateUri = uri,
                Content = uri.AbsoluteUri,
                FontStyle = FontStyle.Italic,
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(0, -1, 0, -1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x3c, 0x3c, 0x3c)),
                BorderThickness = odd ? new Thickness(1) : new Thickness(1, 0, 1, 0),
            };
            ToolTipService.SetToolTip(btn, uri.AbsoluteUri);

            if (handler is not null) {
                btn.Click += handler;
            }

            return btn;
        }

        private void PinButton_Click(object sender, RoutedEventArgs e) {
            Pinned = !Pinned;
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e) {
            DataPackage package = new();

            switch (ContentType) {
                case ClipType.File:
                    foreach (IStorageItem item in _clipStorageItems) {
                        Dogger.Debug(Path.Join(item.Path, item.Name));
                    }
                    package.SetStorageItems(_clipStorageItems);
                    package.RequestedOperation = DataPackageOperation.Copy;
                    break;
                case ClipType.Image:
                    package.SetBitmap(_clipImageStream);
                    break;
                case ClipType.Uri:
                case ClipType.Text:
                default:
                    package.SetText(_clipText);
                    break;
            }
            Clipboard.SetContent(package);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) {
            OnDelete?.Invoke(this);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e) {
            Messenger messenger = new();

            switch (ContentType) {
                case ClipType.Image: {
                    using IRandomAccessStreamWithContentType stream = await _clipImageStream.OpenReadAsync();
                    messenger.WriteStream(stream);
                    break;
                }
                case ClipType.File:
                    messenger.WriteStorageItems(_clipStorageItems);
                    break;
                case ClipType.Uri:
                    messenger.WriteString(_clipUri.AbsoluteUri);
                    break;
                case ClipType.Text:
                default:
                    messenger.WriteString(_clipText);
                    break;
            }
        }
    }

}
