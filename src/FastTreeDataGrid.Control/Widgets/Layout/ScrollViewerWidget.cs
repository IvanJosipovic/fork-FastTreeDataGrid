using System;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ScrollViewerWidget : PanelLayoutWidget
{
    private readonly ScrollViewerLayoutAdapter _adapter = new();
    private double _horizontalOffset;
    private double _verticalOffset;
    private double? _extentWidth;
    private double? _extentHeight;
    private Rect _lastArrangeBounds;

    public double HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            if (Math.Abs(_horizontalOffset - value) <= double.Epsilon)
            {
                return;
            }

            _horizontalOffset = value;
            NotifyViewportChanged();
        }
    }

    public double VerticalOffset
    {
        get => _verticalOffset;
        set
        {
            if (Math.Abs(_verticalOffset - value) <= double.Epsilon)
            {
                return;
            }

            _verticalOffset = value;
            NotifyViewportChanged();
        }
    }

    public double? ExtentWidth
    {
        get => _extentWidth;
        set
        {
            if (_extentWidth == value)
            {
                return;
            }

            _extentWidth = value;
            NotifyViewportChanged();
        }
    }

    public double? ExtentHeight
    {
        get => _extentHeight;
        set
        {
            if (_extentHeight == value)
            {
                return;
            }

            _extentHeight = value;
            NotifyViewportChanged();
        }
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override bool SupportsSpacing => false;

    protected override PanelLayoutContext CreateContext(Rect bounds, Rect innerBounds)
    {
        var options = new ScrollViewerLayoutOptions(
            HorizontalOffset,
            VerticalOffset,
            ExtentWidth,
            ExtentHeight);

        return new PanelLayoutContext(bounds, innerBounds, 0, Padding, options);
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        _lastArrangeBounds = bounds;
        NotifyViewportChanged();
    }

    private void NotifyViewportChanged()
    {
        if (Children.Count == 0 || Children[0] is not IVirtualizingWidgetHost host)
        {
            return;
        }

        if (_lastArrangeBounds.Width <= 0 && _lastArrangeBounds.Height <= 0)
        {
            return;
        }

        var viewportSize = new Size(
            Math.Max(0, _lastArrangeBounds.Width - (Padding.Left + Padding.Right)),
            Math.Max(0, _lastArrangeBounds.Height - (Padding.Top + Padding.Bottom)));

        host.UpdateViewport(new VirtualizingWidgetViewport(viewportSize, new Point(HorizontalOffset, VerticalOffset)));
    }
}
