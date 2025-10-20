using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridPresenter : Avalonia.Controls.Control
{
    private readonly List<RowRenderInfo> _rows = new();
    private FastTreeDataGrid? _owner;
    private readonly List<double> _columnOffsets = new();

    private readonly SolidColorBrush _selectionBrush = new(Color.FromArgb(40, 49, 130, 206));
    private readonly SolidColorBrush _toggleBackground = new(Color.FromRgb(236, 236, 236));
    private readonly Pen _togglePen = new(new SolidColorBrush(Color.FromRgb(96, 96, 96)), 1);
    private readonly Pen _gridPen = new(new SolidColorBrush(Color.FromRgb(210, 210, 210)), 1);

    public FastTreeDataGridPresenter()
    {
        ClipToBounds = true;
        IsHitTestVisible = true;
    }

    public void SetOwner(FastTreeDataGrid owner)
    {
        _owner = owner;
    }

    public void UpdateContent(IReadOnlyList<RowRenderInfo> rows, double totalWidth, double totalHeight, IReadOnlyList<double> columnOffsets)
    {
        _rows.Clear();
        _rows.AddRange(rows);
        _columnOffsets.Clear();
        _columnOffsets.AddRange(columnOffsets);
        Width = totalWidth;
        Height = totalHeight;
        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();
    }

    public void UpdateSelection(int selectedIndex)
    {
        var changed = false;
        foreach (var row in _rows)
        {
            var shouldSelect = row.RowIndex == selectedIndex;
            if (row.IsSelected != shouldSelect)
            {
                row.IsSelected = shouldSelect;
                changed = true;
            }
        }

        if (changed)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        foreach (var row in _rows)
        {
            var rowRect = new Rect(0, row.Top, Width, row.Height);

            // Transparent fill ensures pointer hit-testing works across the entire row surface.
            context.FillRectangle(Brushes.Transparent, rowRect);

            if (row.IsSelected)
            {
                context.FillRectangle(_selectionBrush, rowRect);
            }

            if (row.HasChildren)
            {
                DrawToggle(context, row.ToggleRect, row.IsExpanded);
            }

            foreach (var cell in row.Cells)
            {
                if (cell.FormattedText is null)
                {
                    continue;
                }

                context.DrawText(cell.FormattedText, cell.TextOrigin);
            }

            // Horizontal separator
            context.DrawLine(_gridPen, new Point(0, row.Top + row.Height), new Point(Width, row.Top + row.Height));
        }

        // Vertical separators
        foreach (var offset in _columnOffsets)
        {
            if (offset <= 0 || offset >= Width)
            {
                continue;
            }

            context.DrawLine(_gridPen, new Point(offset, 0), new Point(offset, Height));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_owner is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this).Position;
        if (HandlePointer(point, e))
        {
            e.Handled = true;
        }
    }

    internal bool HandlePointer(Point point, PointerPressedEventArgs e)
    {
        var row = HitTestRow(point);
        if (row is null)
        {
            return false;
        }

        var toggleHit = HitTestToggle(row, point);
        _owner?.HandlePresenterPointerPressed(row, point, e.ClickCount, toggleHit);
        return true;
    }

    private RowRenderInfo? HitTestRow(Point point)
    {
        foreach (var row in _rows)
        {
            if (point.Y >= row.Top && point.Y < row.Top + row.Height)
            {
                return row;
            }
        }

        return null;
    }

    private static bool HitTestToggle(RowRenderInfo row, Point point)
    {
        if (!row.HasChildren)
        {
            return false;
        }

        if (row.ToggleRect.Contains(point))
        {
            return true;
        }

        return row.ToggleBounds is { } bounds && bounds.Contains(point);
    }

    private void DrawToggle(DrawingContext context, Rect rect, bool isExpanded)
    {
        if (rect == default)
        {
            return;
        }

        context.FillRectangle(_toggleBackground, rect);
        context.DrawRectangle(_togglePen, rect);

        var center = rect.Center;
        context.DrawLine(_togglePen, new Point(rect.Left + 2, center.Y), new Point(rect.Right - 2, center.Y));

        if (!isExpanded)
        {
            context.DrawLine(_togglePen, new Point(center.X, rect.Top + 2), new Point(center.X, rect.Bottom - 2));
        }
    }

    internal sealed class RowRenderInfo
    {
        public RowRenderInfo(
            FastTreeDataGridRow row,
            int rowIndex,
            double top,
            double height,
            bool isSelected,
            bool hasChildren,
            bool isExpanded,
            Rect toggleRect,
            bool isGroup)
        {
            Row = row;
            RowIndex = rowIndex;
            Top = top;
            Height = height;
            IsSelected = isSelected;
            HasChildren = hasChildren;
            IsExpanded = isExpanded;
            ToggleRect = toggleRect;
            IsGroup = isGroup;
        }

        public FastTreeDataGridRow Row { get; }
        public int RowIndex { get; }
        public double Top { get; }
        public double Height { get; }
        public bool IsSelected { get; set; }
        public bool HasChildren { get; }
        public bool IsExpanded { get; }
        public Rect ToggleRect { get; }
        public Rect? ToggleBounds { get; set; }
        public bool IsGroup { get; }
        public List<CellRenderInfo> Cells { get; } = new();
    }

    internal sealed class CellRenderInfo
    {
        public CellRenderInfo(Rect bounds, FormattedText? formattedText, Point textOrigin)
        {
            Bounds = bounds;
            FormattedText = formattedText;
            TextOrigin = textOrigin;
        }

        public Rect Bounds { get; }
        public FormattedText? FormattedText { get; }
        public Point TextOrigin { get; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(Width) && Width > 0 ? Width : availableSize.Width;
        var height = double.IsFinite(Height) && Height > 0 ? Height : availableSize.Height;

        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            width = 0;
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            height = 0;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = double.IsFinite(Width) && Width > 0 ? Width : finalSize.Width;
        var height = double.IsFinite(Height) && Height > 0 ? Height : finalSize.Height;

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            width = double.IsNaN(finalSize.Width) || double.IsInfinity(finalSize.Width) || finalSize.Width < 0
                ? 0
                : finalSize.Width;
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            height = double.IsNaN(finalSize.Height) || double.IsInfinity(finalSize.Height) || finalSize.Height < 0
                ? 0
                : finalSize.Height;
        }

        return new Size(width, height);
    }

}
