using AudioSwitcher.AudioApi.CoreAudio;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FancyToys.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VolumeView: Page {

        private double _curVolume;
        private bool _locked;
        private bool Locked {
            get => _locked;
            set {
                if (_locked != value) {
                    _locked = value;
                    LockButton.Content = _locked ? "\xE72E" : "\xE785";
                    VolumeSlider.IsEnabled = !_locked;
                }
            }
        }

        public VolumeView() {
            this.InitializeComponent();
            CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            _curVolume = defaultPlaybackDevice.Volume;
            VolumeSlider.Value = defaultPlaybackDevice.Volume;
        }

        private void LockIcon_Click(object sender, RoutedEventArgs e) {
            if (sender is not ToggleButton) {
                return;
            }
            Locked = !Locked;
        }
    }
}
