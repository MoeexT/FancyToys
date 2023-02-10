using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Drawing;
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
    public sealed partial class TinyToolsView: Page {
        public TinyToolsView() {
            this.InitializeComponent();
        }

        private void GenerateImageFromText(object sender, RoutedEventArgs e) {
            TargetText.Text = $"\\X{InputBox.Text}";
            //TargetImage.Source = DrawText(TargetText.Text, Color.Red);
        }


        private ImageSource DrawText(string text, Color color) {
            var bi = new BitmapImage();
            return null;
        }

        //private Image DrawText(String text, Font font, Color textColor, Color backColor) {
        //    //first, create a dummy bitmap just to get a graphics object
        //    Image img = new Bitmap(1, 1);
        //    Graphics drawing = Graphics.FromImage(img);

        //    //measure the string to see how big the image needs to be
        //    SizeF textSize = drawing.MeasureString(text, font);

        //    //free up the dummy image and old graphics object
        //    img.Dispose();
        //    drawing.Dispose();

        //    //create a new image of the right size
        //    img = new Bitmap((int)textSize.Width, (int)textSize.Height);

        //    drawing = Graphics.FromImage(img);

        //    //paint the background
        //    drawing.Clear(backColor);

        //    //create a brush for the text
        //    Brush textBrush = new SolidBrush(textColor);

        //    drawing.DrawString(text, font, textBrush, 0, 0);

        //    drawing.Save();

        //    textBrush.Dispose();
        //    drawing.Dispose();

        //    return img;

        //}
    }
}
