using Avalonia.Controls;
using Avalonia.Media;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Control.Controls;

public class WidgetHost : Avalonia.Controls.Control
{
    private Widget? _widget;

    protected TextWidget CreateTextWidget()
    {
        return new FormattedTextWidget();
    }

    protected void SetWidget(Widget? widget)
    {
        _widget = widget;
        InvalidateVisual();
    }

    protected void SetWidgets(ReadOnlySpan<Widget> widgets)
    {
        _widget = widgets.Length > 0 ? widgets[0] : null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _widget?.Draw(context);
    }
}
