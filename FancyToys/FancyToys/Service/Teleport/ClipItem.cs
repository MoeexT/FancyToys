using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;


namespace FancyToys.Service.Teleport;

public partial class ClipItem {
    public async Task<bool> SetContent(DataPackageView package) {
        // Not support yet: rtf, html,
        if (package.AvailableFormats.Count == 0) {
            return false;
        }

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
            return panel.Children.Count > 0;
        }

        // bitmap image
        if (package.Contains(StandardDataFormats.Bitmap)) {
            ClipImageStream = await package.GetBitmapAsync();
            ClipJar.Children.Insert(0, await CreateImage(ClipImageStream));
            return true;
        }

        // text, uri, ...
        // ReSharper disable InvertIf
        if (CreateTextElement(await package.GetTextAsync()) is { } textElement) {
            // ReSharper restore InvertIf
            ClipJar.Children.Insert(0, textElement);
            return true;
        }
        return false;
    }

    private FrameworkElement CreateFileElement(string text, bool first = true) {
        if (string.IsNullOrEmpty(text)) {
            return null;
        }

        bool validUri = Uri.TryCreate(text, UriKind.Absolute, out Uri uri);

        if (validUri) {
            // directory
            if (Directory.Exists(text)) {
                return CreateHyperlink(uri, (s, _) => Process.Start("explorer.exe", text), first);
            }

            // set file
            if (File.Exists(text)) {
                // https://stackoverflow.com/a/696144/9013632
                return CreateHyperlink(uri, (s, _) => Process.Start("explorer.exe", $"/select, \"{text}\""), first);
            }
        }

        // not a uri, set text to ClipItem
        ClipText = text;
        return CreateTextBlock(text);
    }

    private FrameworkElement CreateTextElement(string text) {
        if (string.IsNullOrEmpty(text)) { // mailto://hammerma
            return null;
        }

        bool validUri = Uri.TryCreate(text, UriKind.Absolute, out Uri uri);

        // not a uri, set text to ClipItem
        if (!validUri) {
            ClipText = text;
            return CreateTextBlock(text);
        }

        // uri but not file
        ClipUri = uri;

        return CreateHyperlink(uri, (_, _) => Process.Start(new ProcessStartInfo(text) {
            UseShellExecute = true,
        }));
    }

    private static async Task<Image> CreateImage(RandomAccessStreamReference streamReference) {
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
