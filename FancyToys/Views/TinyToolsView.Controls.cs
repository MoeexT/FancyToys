namespace FancyToys.Views;

public partial class TinyToolsView {
    // private TextBlock DefaultView;
    // private StackPanel VolumeLockerView;
    //
    // // volume view controls
    // private FontIcon VolumeIcon;
    // private Slider VolumeSlider;
    // private ToggleButton SystemVolumeLockButton;
    //
    // private void InitializeVolumeLockerControls() {
    //     VolumeIcon = new FontIcon() {
    //         Margin = new Thickness(0, 0, 10, 0)
    //     };
    //
    //     VolumeSlider = new Slider() {
    //         Width = 200,
    //         Minimum = 0,
    //         Maximum = 100,
    //         StepFrequency = 1,
    //         TickFrequency = 10,
    //         TickPlacement = TickPlacement.Outside,
    //     };
    //
    //     VolumeSlider.SetBinding(RangeBase.ValueProperty, new Binding() {
    //         Source = SystemVolumeMax,
    //         Mode = BindingMode.TwoWay,
    //         UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
    //     });
    //
    //     SystemVolumeLockButton = new ToggleButton() {
    //         Margin = new Thickness(0, 0, 12, 0),
    //         Padding = new Thickness(6),
    //         FontFamily = new FontFamily("Segoe MDL2 Assets"),
    //         FontSize = 16,
    //         Content = "\xE72E"
    //     };
    //
    //     SystemVolumeLockButton.SetBinding(ToggleButton.IsCheckedProperty, new Binding() {
    //         Source = SystemVolumeLocked,
    //         Mode = BindingMode.TwoWay,
    //         UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
    //     });
    // }
    //
    // private StackPanel CreateSystemVolumeView() {
    //     StackPanel panel = new();
    //
    //     TextBlock header = new() {
    //         Text = "System Volume Locker",
    //         FontSize = 26,
    //     };
    //
    //     StackPanel innerPanel = new() {
    //         Orientation = Orientation.Horizontal,
    //         Margin = new Thickness(0, 20, 0, 0)
    //     };
    //     
    //     TextBlock volumeIndicator = new() {
    //         Margin = new Thickness(20, 0, 20, 0),
    //         FontSize = 20,
    //     };
    //
    //     volumeIndicator.SetBinding(TextBlock.TextProperty, new Binding() {
    //         Source = VolumeSlider.Value,
    //         Mode = BindingMode.OneWay,
    //         UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
    //     });
    //
    //     InitializeVolumeLockerControls();
    //
    //     innerPanel.Children.Add(VolumeIcon);
    //     innerPanel.Children.Add(VolumeSlider);
    //     innerPanel.Children.Add(volumeIndicator);
    //     innerPanel.Children.Add(SystemVolumeLockButton);
    //
    //     panel.Children.Add(header);
    //     panel.Children.Add(innerPanel);
    //     
    //     Dogger.Trace("Created VolumeLockerView");
    //
    //     return panel;
    // }
    //
    // private static TextBlock CreateDefaultView() {
    //     Dogger.Trace("Created DefaultView");
    //
    //     return new TextBlock() {
    //         Text = "Expect nice things.",
    //         FontSize = 36,
    //     };
    // }
}
