using System.Diagnostics;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;

using FancyToys.Logging;
using FancyToys.Utils;

using Microsoft.UI.Dispatching;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Views {

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ServerView: Page {
        public static ServerView CurrentInstance { get; private set; }
        private double FancyToysPanelOpacity;

        public unsafe ServerView() {
            InitializeComponent();
            CurrentInstance = this;

            fixed (double* p = &FancyToysPanelOpacity) {
                Notifier.Subscribe(Notifier.Keys.ServerPanelOpacity, p);
            }
        }

        public async void PrintLog(LogStruct ls) {
            Debug.WriteLine("PrintLog");

            DispatcherQueue.GetForCurrentThread().TryEnqueue(() => {
                Color color = Consts.LogForegroundColors[ls.Level];
                bool highlight = Consts.HighlightedLogLevels.Contains(ls.Level);
                FontWeight weight = highlight ? FontWeights.Bold : FontWeights.Normal;

                Paragraph p = new();

                Run src = new() {
                    Text = ls.Source + ' ',
                    Foreground = new SolidColorBrush(highlight ? color : Colors.Gray),
                    // FontWeight = weight,
                };

                Run msg = new() {
                    Text = ls.Content,
                    Foreground = new SolidColorBrush(color),
                    FontWeight = weight,
                };

                p.Inlines.Add(src);
                p.Inlines.Add(msg);
                FancyToysPanel.Blocks.Add(p);
            });
        }

        public void PrintStd(StdStruct ss) {
            DispatcherQueue.GetForCurrentThread().TryEnqueue(() => {
                Paragraph p = new();

                Run src = new() {
                    Text = $"<{ss.Sender}> ",
                    Foreground = new SolidColorBrush(Colors.RoyalBlue),
                };

                Run msg = new() {
                    Text = ss.Content,
                    Foreground = new SolidColorBrush(Consts.StdForegroundColors[ss.Level]),
                };

                p.Inlines.Add(src);
                p.Inlines.Add(msg);
                FancyToysPanel.Blocks.Add(p);
            });


        }

        private void FancyToysPanelLoaded(object sender, RoutedEventArgs e) {
            Dogger.Flush();
            StdLogger.Flush();
        }
    }

}
