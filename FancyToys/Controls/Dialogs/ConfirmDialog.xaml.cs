﻿using Microsoft.UI.Xaml.Controls;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板


namespace FancyToys.Controls.Dialogs {

    public sealed partial class ConfirmDialog: ContentDialog {
        public ConfirmDialog() { this.InitializeComponent(); }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) { }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) { }
    }

}
