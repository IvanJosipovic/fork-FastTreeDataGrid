using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class MenuSeparatorWidget : Widget
{
    public MenuSeparatorWidget()
    {
        DesiredHeight = 12;
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.Menu;
        var brush = palette.PresenterBorder ?? new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00));
        var thickness = palette.PresenterBorderThickness.Top > 0
            ? palette.PresenterBorderThickness.Top
            : 1;

        var pen = new Pen(brush, thickness);
        var y = rect.Y + rect.Height / 2;

        var left = rect.X + palette.ItemPadding.Left;
        var right = rect.Right - palette.ItemPadding.Right;

        if (left >= right)
        {
            left = rect.X;
            right = rect.Right;
        }

        using var clip = PushClip(context);
        context.DrawLine(pen, new Point(left, y), new Point(right, y));
    }
}
