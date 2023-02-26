using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using FancyToys.Annotations;
using FancyToys.Logging;

using NAudio.CoreAudioApi;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Views {

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TinyToolsView {

        public ObservableCollection<NavLink> NavLinks { get; } = new() {
            new NavLink { Label = "VolumeLocker", Glyph = "\xE995" },
            new NavLink { Label = "Expect", Glyph = "\xE170" },
        };

        public TinyToolsView() {
            InitializeComponent();

            // init volume locker
            MMDeviceEnumerator enumer = new();
            _audioDevice = enumer.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _currentSystemVolume = _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            _audioDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;

            SystemVolumeMax = SystemVolumeMax;
            SystemVolumeLocked = SystemVolumeLocked;
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data) {
            checkAndResetSystemVolume(data.MasterVolume);
        }

        /// <summary>
        /// set system volume to `SystemVolumeMax` if it's grater than SystemVolumeMax.
        /// </summary>
        /// <param name="deviceVolume"></param>
        private void checkAndResetSystemVolume(float deviceVolume) {
            float max = (float)SystemVolumeMax / 100;

            if (SystemVolumeLocked && deviceVolume > max && Math.Abs(deviceVolume - _currentSystemVolume) > 0.001) {
                _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = max;
                Dogger.Info($"Reset system volume from: ${deviceVolume} to ${max}");
                _currentSystemVolume = max;
            } else {
                _currentSystemVolume = _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            }
        }

        private void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e) {
            if (e.ClickedItem is not NavLink navLink) {
                return;
            }

            VolumeLockerView.Visibility = Visibility.Collapsed;
            DefaultView.Visibility = Visibility.Collapsed;

            Visibility _ = navLink.Label switch {
                "VolumeLocker" => VolumeLockerView.Visibility = Visibility.Visible, //VolumeLockerView ??= CreateSystemVolumeView(),
                "Expect" => DefaultView.Visibility = Visibility.Visible, // DefaultView ??= CreateDefaultView(),
                _ => DefaultView.Visibility = Visibility.Visible // DefaultView ??= CreateDefaultView(),
            };
        }

        private void PanelHeader_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            TinyToolsSplitView.IsPaneOpen = !TinyToolsSplitView.IsPaneOpen;
        }
    }

    public class NavLink: INotifyPropertyChanged {
        private string _label;
        private string _glyph;

        public string Label {
            get => _label;
            set {
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }

        public string Glyph {
            get => _glyph;
            set {
                _glyph = value;
                OnPropertyChanged(nameof(Glyph));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
