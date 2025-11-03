using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets.Hosting;

public sealed class WidgetHostExtension : MarkupExtension
{
    [Content]
    public Widget? Root { get; set; }

    public double Width { get; set; } = double.NaN;

    public double Height { get; set; } = double.NaN;

    public IBrush? Background { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Root is null)
        {
            return AvaloniaProperty.UnsetValue;
        }

        return WidgetHost.Create(Root, Width, Height, Background);
    }
}
