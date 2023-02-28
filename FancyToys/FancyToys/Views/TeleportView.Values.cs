using Windows.Storage;

using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace FancyToys.Views;

public partial class TeleportView {

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

    private bool AllowClip {
        get => (bool)(_dataContainer.Values[nameof(AllowClip)] ?? true);
        set => _dataContainer.Values[nameof(AllowClip)] = value;
    }

    private bool LoadSystemClipboard {
        get => (bool)(_dataContainer.Values[nameof(LoadSystemClipboard)] ?? false);
        set => _dataContainer.Values[nameof(LoadSystemClipboard)] = value;
    }

    private bool AllowSimilarWithFormer {
        get => (bool)(_dataContainer.Values[nameof(AllowSimilarWithFormer)] ?? false);
        set => _dataContainer.Values[nameof(AllowSimilarWithFormer)] = value;
    }

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

}
