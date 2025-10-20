using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public abstract class Widget
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Rotation { get; set; }

    public ImmutableSolidColorBrush? Foreground { get; set; }

    public string? Key { get; set; }

    public double DesiredWidth { get; set; } = double.NaN;

    public double DesiredHeight { get; set; } = double.NaN;

    public bool ClipToBounds { get; set; } = true;

    public Rect Bounds { get; private set; }

    public virtual void Arrange(Rect bounds)
    {
        Bounds = bounds;
        X = bounds.X;
        Y = bounds.Y;
    }

    protected IDisposable? PushClip(DrawingContext context)
    {
        if (!ClipToBounds)
        {
            return null;
        }

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return null;
        }

        return context.PushClip(Bounds);
    }

    public abstract void Draw(DrawingContext context);

    public virtual void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
    }
}
