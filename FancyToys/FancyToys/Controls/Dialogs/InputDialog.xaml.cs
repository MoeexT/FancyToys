using Microsoft.UI.Xaml.Controls;


namespace FancyToys.Controls.Dialogs {

    public sealed partial class InputDialog: ContentDialog {
        public bool isSaved = false;
        public string inputContent = "";

        public InputDialog(string title, string desc, string content = "") {
            InitializeComponent();
            Title = title;
            DialogText.Text = desc;
            DialogInput.Text = content;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            inputContent = DialogInput.Text;
            isSaved = true;
            Hide();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            isSaved = false;
            Hide();
        }
    }

}
