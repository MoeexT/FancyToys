using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace FancyToys.Controls {

    public class CategoryBase { }

    public class Category : CategoryBase
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string Tooltip { get; set; }
        public string Glyph { get; set; }
        public Brush Foreground { get; set; }
        //public Type TargetType { get; set; }
    }

    public class Separator : CategoryBase { }

    public class Header : CategoryBase
    {
        public string Name { get; set; }
    }

}
