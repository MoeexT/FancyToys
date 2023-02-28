using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;

using FancyToys.Logging;


namespace FancyToys.Service.Teleport;

public partial class ClipItem {
    public async Task<bool> SetContent(DataPackageView package) {
        // Not support yet: rtf, html,
        if (package.AvailableFormats.Count == 0) {
            return false;
        }

        Dogger.Trace(string.Join(", ", package.AvailableFormats));

        // copy multiple files once
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
            ClipStorageItems = await package.GetStorageItemsAsync();

            for (int i = 0; i < ClipStorageItems.Count; i++) {
                IStorageItem storageItem = ClipStorageItems[i];

                if (CreateFileElement(storageItem.Path, i == 0) is { } storageItemElement) {
                    panel.Children.Add(storageItemElement);
                }
            }

            ClipJar.Children.Insert(0, panel);
            Dogger.Debug($"Create clipboard files: {ClipStorageItems.Count}");
            return panel.Children.Count > 0;
        }

        // bitmap image
        if (package.Contains(StandardDataFormats.Bitmap)) {
            ClipJar.Children.Insert(0, await CreateImage(await package.GetBitmapAsync()));
            return true;
        }

        // text, uri, ...
        if (CreateTextElement(await package.GetTextAsync()) is { } textElement) {
            ClipJar.Children.Insert(0, textElement);
            return true;
        }
        return false;
    }

    internal async Task<bool> SetContent(ClipItemStruct cis) {
        switch (cis.ContentType) {
            case ClipType.Uri:
            case ClipType.Text:
                string content = string.IsNullOrEmpty(cis.Text) ? cis.Uri : cis.Text;

                if (CreateTextElement(content) is { } textElement) {
                    ClipJar.Children.Insert(0, textElement);
                    return true;
                }
                return false;
            case ClipType.Image: {
                if (cis.ImageBytes is null || cis.ImageBytes.Length == 0) {
                    return false;
                }
                InMemoryRandomAccessStream randomStream = new();
                await using MemoryStream stream = new(cis.ImageBytes);
                await RandomAccessStream.CopyAsync(stream.AsInputStream(), randomStream);
                ClipJar.Children.Insert(0, await CreateImage(RandomAccessStreamReference.CreateFromStream(randomStream)));
                return true;
            }
            case ClipType.File:
                StackPanel panel = new();
                var storageItems = new List<IStorageItem>();

                for (int i = 0; i < cis.Paths.Length; i++) {
                    string path = cis.Paths[i];

                    if (CreateFileElement(path, i == 0) is { } storageItemElement) {
                        panel.Children.Add(storageItemElement);

                        if (File.Exists(path)) {
                            storageItems.Add(await StorageFile.GetFileFromPathAsync(path));
                        }

                        if (Directory.Exists(path)) {
                            storageItems.Add(await StorageFolder.GetFolderFromPathAsync(path));
                        }
                    }
                }
                ClipStorageItems = storageItems;
                ClipJar.Children.Insert(0, panel);
                return panel.Children.Count > 0;
            default:
                return false;
        }
    }

    internal async Task<ClipItemStruct> GetStruct() {
        byte[] bytes = Array.Empty<byte>();

        if (_clipImageStream is not null) {
            IRandomAccessStreamWithContentType stream = await _clipImageStream.OpenReadAsync();
            await using MemoryStream memoryStream = new();
            await stream.AsStreamForRead().CopyToAsync(memoryStream);
            bytes = memoryStream.ToArray();
        }

        return new ClipItemStruct() {
            Pinned = Pinned,
            ContentType = ContentType,
            Uri = ClipUri?.ToString() ?? string.Empty,
            Text = ClipText ?? string.Empty,
            Paths = ClipStorageItems?.Select(item => item.Path).ToArray() ?? Array.Empty<string>(),
            ImageBytes = bytes,
        };
    }

    private FrameworkElement CreateFileElement(string text, bool first = true) {
        if (string.IsNullOrEmpty(text)) {
            return null;
        }

        bool validUri = Uri.TryCreate(text, UriKind.Absolute, out Uri uri);

        if (validUri) {
            // directory
            if (Directory.Exists(text)) {
                return CreateHyperlink(uri, (_, _) => Process.Start("explorer.exe", text), first);
            }

            // set file
            if (File.Exists(text)) {
                // https://stackoverflow.com/a/696144/9013632
                return CreateHyperlink(uri, (_, _) => Process.Start("explorer.exe", $"/select, \"{text}\""), first);
            }
        }

        // not a valid file uri, set text to ClipItem
        Dogger.Debug($"Not a valid file, creat clipboard text: {text}");
        ClipText = text;
        return CreateTextBlock(text);
    }

    private FrameworkElement CreateTextElement(string text) {
        if (string.IsNullOrEmpty(text)) {
            return null;
        }

        bool validUri = Uri.TryCreate(text, UriKind.Absolute, out Uri uri);

        // not a uri, set text to ClipItem
        if (!validUri) {
            ClipText = text;
            Dogger.Debug($"Not a valid uri, creat clipboard text: {text}");
            return CreateTextBlock(text);
        }

        // uri but not file
        ClipUri = uri;

        Dogger.Debug($"Creat clipboard uri: {text}");

        return CreateHyperlink(uri, (_, _) => Process.Start(new ProcessStartInfo(text) {
            UseShellExecute = true,
        }));
    }

    private async Task<Image> CreateImage(RandomAccessStreamReference streamReference) {
        ClipImageStream = streamReference;
        using IRandomAccessStreamWithContentType stream = await streamReference.OpenReadAsync();
        BitmapImage bi = new();
        bi.SetSource(stream);
        Dogger.Debug("Create clipboard image.");

        return new Image {
            Source = bi,
            Height = 70,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    private TextBlock CreateTextBlock(string path) {
        _textBlock = new TextBlock {
            Text = path,
            MaxLines = 3,
            Width = 350,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        _textBlock.PointerEntered += (_, _) => {
            _textBlock.TextDecorations = TextDecorations.Underline;
        };

        _textBlock.PointerExited += (_, _) => {
            TextDecoration = TextDecorations.None;
        };
        return _textBlock;
    }

    private static HyperlinkButton CreateHyperlink(Uri uri, RoutedEventHandler handler, bool first = true) {
        HyperlinkButton btn = new() {
            Height = 30,
            Width = 350,
            NavigateUri = uri,
            Content = uri.ToString(),
            FontStyle = FontStyle.Italic,
            CornerRadius = new CornerRadius(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(1, -1, 0, -1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x3c, 0x3c, 0x3c)),
            BorderThickness = first ? new Thickness(1) : new Thickness(1, 0, 1, 1),
        };
        ToolTipService.SetToolTip(btn, uri.AbsoluteUri);

        if (handler is not null) {
            btn.Click += handler;
        }

        return btn;
    }
}
