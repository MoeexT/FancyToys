using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Controls {

    [ContentProperty(Name = "ItemTemplate")]
    public sealed class MenuItemTemplateSelector: DataTemplateSelector {
        public DataTemplate ItemTemplate { get; set; }

        //public string PaneTitle { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) {
            return item switch {
                Separator => SeparatorTemplate,
                Header => HeaderTemplate,
                _ => ItemTemplate
            };
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) {
            return item switch {
                Separator => SeparatorTemplate,
                Header => HeaderTemplate,
                _ => ItemTemplate
            };
        }

        internal readonly DataTemplate HeaderTemplate = (DataTemplate)XamlReader.Load(
            @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                   <NavigationViewItemHeader Content='{Binding Name}' />
                  </DataTemplate>");

        internal readonly DataTemplate SeparatorTemplate = (DataTemplate)XamlReader.Load(
            @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <NavigationViewItemSeparator />
                  </DataTemplate>");
    }

}
