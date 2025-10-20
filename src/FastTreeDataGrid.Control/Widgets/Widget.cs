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

    public abstract void Draw(DrawingContext context);

    public virtual void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
    }
}
