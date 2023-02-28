using System.ComponentModel;
using System.Runtime.CompilerServices;

using Windows.Storage;

using FancyToys.Annotations;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using NAudio.CoreAudioApi;


namespace FancyToys.Views;

public partial class TinyToolsView {

    private readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

    public double SystemVolumeMax {
        get => (double)(LocalSettings.Values[nameof(SystemVolumeMax)] ?? 20.0);
        set {
            LocalSettings.Values[nameof(SystemVolumeMax)] = value;

            VolumeIcon.Glyph = value switch {
                0 => "\xE74F",
                < 34 => "\xE993",
                < 67 => "\xE994",
                _ => "\xE995"
            };
            NavLinks[0].Glyph = VolumeIcon.Glyph;
        }
    }

    public bool SystemVolumeLocked {
        get => (bool)(LocalSettings.Values[nameof(SystemVolumeLocked)] ?? true);
        set {
            // TODO fixme: these vars' value don't follow SystemVolumeLockButton's check state 
            LocalSettings.Values[nameof(SystemVolumeLocked)] = value;

            SystemVolumeLockButton.Content = value ? "\xE72E" : "\xE785";
            VolumeSlider.IsEnabled = !value;

            if (_audioDevice == null || !value) return;
            _currentSystemVolume = 0;
            checkAndResetSystemVolume(_audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar);
        }
    }

    private static MMDevice _audioDevice;
    private static float _currentSystemVolume;
}
