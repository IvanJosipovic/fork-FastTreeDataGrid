using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Lightweight rectangle visual for chrome decorations.
/// </summary>
public sealed class BorderVisualWidget : Widget
{
    static BorderVisualWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(BorderVisualWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not BorderVisualWidget border)
                {
                    return;
                }

                var layout = theme.Palette.Layout;
                var borderPalette = theme.Palette.Border;

                if (border.Stroke is null)
                {
                    border.Stroke = borderPalette.ControlBorder.Get(border.VisualState) ?? new ImmutableSolidColorBrush(Color.FromRgb(0xDE, 0xDE, 0xDE));
                }

                if (border.Fill is null)
                {
                    border.Fill = layout.SplitViewPaneBackground ?? new ImmutableSolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
                }
            }));
    }

    public ImmutableSolidColorBrush? Fill { get; set; }

    public ImmutableSolidColorBrush? Stroke { get; set; }

    public double StrokeThickness { get; set; } = 1;

    public override void Draw(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        using var transform = PushRenderTransform(context);

        var pen = Stroke is not null && StrokeThickness > 0 ? new Pen(Stroke, StrokeThickness) : null;
        var radius = CornerRadius == default ? 0 : CornerRadius.TopLeft;
        context.DrawRectangle(Fill, pen, Bounds, radius, radius);
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);
    }
}
