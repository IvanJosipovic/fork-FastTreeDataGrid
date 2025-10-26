using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public class ContentControlWidget : BorderWidget
{
    static ContentControlWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(ContentControlWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not ContentControlWidget control)
                {
                    return;
                }

                var layout = theme.Palette.Layout;
                var border = theme.Palette.Border;

                if (control.Padding == default)
                {
                    control.Padding = layout.ContentPadding;
                }

                var skipChrome = control is DecoratorWidget;

                if (!skipChrome && control.Background is null)
                {
                    control.Background = layout.SplitViewPaneBackground ?? new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                }

                if (!skipChrome && control.CornerRadius == default)
                {
                    control.CornerRadius = layout.ControlCornerRadius;
                }

                if (!skipChrome && control.BorderBrush is null)
                {
                    control.BorderBrush = border.ControlBorder.Get(WidgetVisualState.Normal);
                }

                if (!skipChrome && control.BorderThickness == default)
                {
                    control.BorderThickness = new Thickness(1);
                }
            }));
    }

    public Widget? Content
    {
        get => Child;
        set => Child = value;
    }
}
