using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;


namespace FancyToys.Controls;

public class ClipItem {
    private Uri _uriSource;
    private string _textSource;
    private BitmapImage _imageSource;

    public Visibility ShowUri { get; private set; } = Visibility.Collapsed;
    public Visibility ShowText { get; private set; } = Visibility.Collapsed;
    public Visibility ShowImage { get; private set; } = Visibility.Collapsed;
    public ClipItem Self { get; set; }
    //
    // public ClipItem() {
    //     Self = this;
    // }
    
    public Uri UriSource {
        get => _uriSource;
        set {
            _uriSource = value;
            ShowUri = Visibility.Visible;
        }
    }

    public string TextSource {
        get => _textSource;
        set {
            _textSource = value;
            ShowText = Visibility.Visible;
        }
    }

    public BitmapImage ImageSource {
        get => _imageSource;
        set {
            _imageSource = value;
            ShowImage = Visibility.Visible;
        }
    }
}
