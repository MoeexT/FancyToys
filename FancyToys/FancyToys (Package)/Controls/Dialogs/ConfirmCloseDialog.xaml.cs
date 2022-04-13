using Microsoft.UI.Xaml.Controls;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板


namespace FancyToys.Controls.Dialogs {

    public enum CloseAction {
        Terminate,
        Systray,
        Consolidate
    }

    public sealed partial class ConfirmCloseDialog: ContentDialog {
        public CloseAction Result { get; private set; }

        public ConfirmCloseDialog() {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            if (rbTerminate.IsChecked == true) {
                Result = CloseAction.Terminate;
            } else if (rbSystray.IsChecked == true) {
                Result = CloseAction.Systray;
            } else {
                Result = CloseAction.Consolidate;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) { }
    }

}
