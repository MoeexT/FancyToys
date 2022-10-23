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

        public delegate void OnDeleteButtonClicked(ClipListItem item);
        public event OnDeleteButtonClicked OnDelete;

        private bool _pinned;

        private Uri _clipUri;
        private string _clipText;
        private RandomAccessStreamReference _clipImageStream;
        private IReadOnlyList<IStorageItem> _clipStorageItems;
        
        private TextBlock _textBlock { get; set; }

        public ClipType ContentType { get; private set; }

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

            if (package.Contains("FileDrop")) {
                ContentType = ClipType.File;
                _clipStorageItems = await package.GetStorageItemsAsync();
                StackPanel panel = new();

                for (int i = 0; i < _clipStorageItems.Count; i++) {
                    IStorageItem storageItem = _clipStorageItems[i];
                    panel.Children.Add(ClassifyText(storageItem.Path, i + 1));
                    Debug.WriteLine($"{storageItem.Path}, {storageItem.Name}, {storageItem.DateCreated}");
                }
                ClipJar.Children.Insert(0, panel);
            } else if (package.Contains(StandardDataFormats.Bitmap)) {
                ContentType = ClipType.Image;
                ClipJar.Children.Insert(0, await SetImageStream(await package.GetBitmapAsync()));
            } else {
                ClipJar.Children.Insert(0, ClassifyText(await package.GetTextAsync()));
            }
        }

        private FrameworkElement ClassifyText(string path, int counter = 1) {
            if (path is null) {
                return null;
            }
            bool validUri = Uri.TryCreate(path, UriKind.Absolute, out Uri uri);

            // not a uri, set text to ClipItem
            if (!validUri) {
                ContentType = ClipType.Text;
                _clipText = path;
                return CreateTextBlock(path);
            }

            ContentType = ClipType.Uri;
            _clipUri = uri;
            string scheme = uri.Scheme;

            if (scheme.Equals(Uri.UriSchemeFile)) {
                // file, open with explorer
                Debug.WriteLine($"是文件！！！{path}");
                Dogger.Debug($"是文件！！！{path}");
                return CreateHyperlink(path, null, (s, _) => {
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
                return CreateHyperlink(path, uri, null);
            }
            if (scheme.Equals(Uri.UriSchemeMailto)) { //  || isEmailAddress
                // email, open with `Email`
                return CreateHyperlink(path, null, (s, _) => {
                    if (s is not HyperlinkButton) {
                        return;
                    }

                    // https://github.com/dotnet/runtime/issues/30303#issuecomment-513210581
                    Process.Start(new ProcessStartInfo(path) {
                        UseShellExecute = true
                    });
                });
            }

            // unknown uri
            return CreateTextBlock(path);
        }

        private async Task<Image> SetImageStream(RandomAccessStreamReference streamReference) {
            _clipImageStream = streamReference;
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
            return _textBlock = new TextBlock {
                Text = path,
                MaxLines = 3,
                // Height = 70,
                Width = 300,
                // TextDecorations = TextDecorations.Underline,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
        }

        private static HyperlinkButton CreateHyperlink(string path, Uri uri, RoutedEventHandler handler, bool odd = true) {
            HyperlinkButton btn = new() {
                Content = path,
                Height = 30,
                FontStyle = FontStyle.Italic,
                BorderThickness = odd ? new Thickness(1) : new Thickness(1, 0, 1, 0),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x3c, 0x3c, 0x3c)),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0, -1, 0, -1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            ToolTipService.SetToolTip(btn, path);

            if (uri is not null) {
                btn.NavigateUri = uri;
            }

            if (handler is not null) {
                btn.Click += handler;
            }

            return btn;
        }

        private void PinButton_Click(object sender, RoutedEventArgs e) {
            Pinned = !Pinned;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e) {
            DataPackage package = new();

            switch (ContentType) {
                case ClipType.File:
                    package.SetStorageItems(_clipStorageItems);
                    break;
                case ClipType.Image:
                    package.SetBitmap(_clipImageStream);
                    break;
                case ClipType.Uri:
                    package.SetUri(_clipUri);
                    break;
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
