using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Engine.Models;

namespace FastTreeDataGrid.Control.Controls;

internal sealed class FastTreeDataGridAutomationPeer : ControlAutomationPeer, ISelectionProvider, IScrollProvider
{
    private readonly FastTreeDataGrid _owner;

    public FastTreeDataGridAutomationPeer(FastTreeDataGrid owner)
        : base(owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

    protected override string GetClassNameCore() => nameof(FastTreeDataGrid);

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
    {
        var viewport = _owner.GetAutomationViewport();
        if (viewport.IsEmpty || viewport.LastIndexExclusive <= viewport.FirstIndex)
        {
            return Array.Empty<AutomationPeer>();
        }

        var children = new List<AutomationPeer>();
        for (var i = viewport.FirstIndex; i < viewport.LastIndexExclusive; i++)
        {
            if (_owner.TryGetRowForAutomation(i, out _))
            {
                children.Add(new FastTreeDataGridRowAutomationPeer(this, _owner, i));
            }
        }

        return children;
    }

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(ISelectionProvider))
        {
            return this;
        }

        if (providerType == typeof(IScrollProvider))
        {
            return this;
        }

        return base.GetProviderCore(providerType);
    }

    bool ISelectionProvider.CanSelectMultiple => _owner.SelectionMode != FastTreeDataGridSelectionMode.Single;

    bool ISelectionProvider.IsSelectionRequired => false;

    IReadOnlyList<AutomationPeer> ISelectionProvider.GetSelection()
    {
        var selected = _owner.SelectedIndices;
        if (selected.Count == 0)
        {
            return Array.Empty<AutomationPeer>();
        }

        var peers = new List<AutomationPeer>(selected.Count);
        foreach (var index in selected)
        {
            if (_owner.TryGetRowForAutomation(index, out _))
            {
                peers.Add(new FastTreeDataGridRowAutomationPeer(this, _owner, index));
            }
        }

        return peers;
    }

    bool IScrollProvider.HorizontallyScrollable
    {
        get
        {
            var metrics = _owner.GetScrollMetrics();
            return metrics.ExtentWidth - metrics.ViewportWidth > 0.5;
        }
    }

    bool IScrollProvider.VerticallyScrollable
    {
        get
        {
            var metrics = _owner.GetScrollMetrics();
            return metrics.ExtentHeight - metrics.ViewportHeight > 0.5;
        }
    }

    double IScrollProvider.HorizontalScrollPercent
    {
        get
        {
            var metrics = _owner.GetScrollMetrics();
            if (metrics.ExtentWidth - metrics.ViewportWidth <= 0.5)
            {
                return double.NaN;
            }

            return (metrics.HorizontalOffset / Math.Max(1, metrics.ExtentWidth - metrics.ViewportWidth)) * 100d;
        }
    }

    double IScrollProvider.VerticalScrollPercent
    {
        get
        {
            var metrics = _owner.GetScrollMetrics();
            if (metrics.ExtentHeight - metrics.ViewportHeight <= 0.5)
            {
                return double.NaN;
            }

            return (metrics.VerticalOffset / Math.Max(1, metrics.ExtentHeight - metrics.ViewportHeight)) * 100d;
        }
    }

    double IScrollProvider.HorizontalViewSize
    {
        get
        {
            var metrics = _owner.GetScrollMetrics();
            if (metrics.ExtentWidth <= 0)
            {
                return 100d;
            }

            return Math.Clamp((metrics.ViewportWidth / metrics.ExtentWidth) * 100d, 0d, 100d);
        }
    }

    double IScrollProvider.VerticalViewSize
    {
        get
        {
            var metrics = _owner.GetScrollMetrics();
            if (metrics.ExtentHeight <= 0)
            {
                return 100d;
            }

            return Math.Clamp((metrics.ViewportHeight / metrics.ExtentHeight) * 100d, 0d, 100d);
        }
    }

    void IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
    {
        var metrics = _owner.GetScrollMetrics();

        if (metrics.ExtentWidth - metrics.ViewportWidth <= 0.5)
        {
            horizontalAmount = ScrollAmount.NoAmount;
        }

        if (metrics.ExtentHeight - metrics.ViewportHeight <= 0.5)
        {
            verticalAmount = ScrollAmount.NoAmount;
        }

        var targetHorizontal = metrics.HorizontalOffset + CalculateScrollDelta(horizontalAmount, metrics.ViewportWidth);
        var targetVertical = metrics.VerticalOffset + CalculateScrollDelta(verticalAmount, metrics.ViewportHeight);

        _owner.AutomationScrollToOffset(
            double.IsNaN(targetHorizontal) ? null : targetHorizontal,
            double.IsNaN(targetVertical) ? null : targetVertical);
    }

    void IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent)
    {
        var metrics = _owner.GetScrollMetrics();
        double? targetHorizontal = null;
        double? targetVertical = null;

        if (!double.IsNaN(horizontalPercent))
        {
            if (metrics.ExtentWidth - metrics.ViewportWidth <= 0.5)
            {
                targetHorizontal = 0d;
            }
            else
            {
                targetHorizontal = Math.Clamp(horizontalPercent, 0d, 100d) / 100d * (metrics.ExtentWidth - metrics.ViewportWidth);
            }
        }

        if (!double.IsNaN(verticalPercent))
        {
            if (metrics.ExtentHeight - metrics.ViewportHeight <= 0.5)
            {
                targetVertical = 0d;
            }
            else
            {
                targetVertical = Math.Clamp(verticalPercent, 0d, 100d) / 100d * (metrics.ExtentHeight - metrics.ViewportHeight);
            }
        }

        _owner.AutomationScrollToOffset(targetHorizontal, targetVertical);
    }

    private static double CalculateScrollDelta(ScrollAmount amount, double viewport)
    {
        switch (amount)
        {
            case ScrollAmount.LargeDecrement:
                return -viewport;
            case ScrollAmount.SmallDecrement:
                return -Math.Max(1, viewport * 0.1);
            case ScrollAmount.LargeIncrement:
                return viewport;
            case ScrollAmount.SmallIncrement:
                return Math.Max(1, viewport * 0.1);
            default:
                return double.NaN;
        }
    }
}

internal sealed class FastTreeDataGridRowAutomationPeer : AutomationPeer, ISelectionItemProvider, IExpandCollapseProvider
{
    private readonly FastTreeDataGridAutomationPeer _parent;
    private readonly FastTreeDataGrid _owner;
    private readonly int _rowIndex;

    public FastTreeDataGridRowAutomationPeer(FastTreeDataGridAutomationPeer parent, FastTreeDataGrid owner, int rowIndex)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _rowIndex = rowIndex;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

    protected override string GetClassNameCore() => "FastTreeDataGridRow";

    protected override AutomationPeer? GetParentCore() => _parent;

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(ISelectionItemProvider))
        {
            return this;
        }

        if (providerType == typeof(IExpandCollapseProvider) && SupportsExpandCollapse)
        {
            return this;
        }

        return base.GetProviderCore(providerType);
    }

    protected override string? GetNameCore()
    {
        if (!_owner.TryGetRowForAutomation(_rowIndex, out var row))
        {
            return $"Row {_rowIndex + 1}";
        }

        if (_owner.Columns.Count == 0)
        {
            return $"Row {_rowIndex + 1}";
        }

        var firstColumn = _owner.Columns[0];
        var text = _owner.GetAutomationCellText(row, firstColumn);
        return string.IsNullOrWhiteSpace(text) ? $"Row {_rowIndex + 1}" : text;
    }

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
    {
        if (_owner.Columns.Count == 0 || !_owner.TryGetRowForAutomation(_rowIndex, out _))
        {
            return Array.Empty<AutomationPeer>();
        }

        var children = new List<AutomationPeer>(_owner.Columns.Count);
        for (var i = 0; i < _owner.Columns.Count; i++)
        {
            children.Add(new FastTreeDataGridCellAutomationPeer(this, _owner, _rowIndex, i));
        }

        return children;
    }

    protected override void BringIntoViewCore() => _owner.ScrollRowIntoViewForAutomation(_rowIndex);

    protected override string GetAcceleratorKeyCore() => string.Empty;

    protected override string GetAccessKeyCore() => string.Empty;

    protected override Rect GetBoundingRectangleCore()
    {
        return _owner.GetRowBoundsForAutomation(_rowIndex);
    }

    protected override string GetAutomationIdCore() => $"Row{_rowIndex}";

    protected override AutomationPeer? GetLabeledByCore() => null;

    protected override string GetLocalizedControlTypeCore() => "data row";

    protected override string GetHelpTextCore() => string.Empty;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override bool HasKeyboardFocusCore() =>
        _owner.IsKeyboardFocusWithin && _owner.SelectedIndex == _rowIndex;

    protected override bool IsKeyboardFocusableCore() => false;

    protected override bool IsEnabledCore() => _owner.IsEffectivelyEnabled;

    protected override void SetFocusCore()
    {
        _owner.SetCurrentValue(FastTreeDataGrid.SelectedIndexProperty, _rowIndex);
    }

    protected override bool ShowContextMenuCore() => false;

    protected override bool TrySetParent(AutomationPeer? parent) =>
        ReferenceEquals(parent, _parent);

    private bool SupportsExpandCollapse =>
        _owner.TryGetRowForAutomation(_rowIndex, out var row) && row.HasChildren;

    bool ISelectionItemProvider.IsSelected =>
        _owner.IsRowSelected(_rowIndex);

    ISelectionProvider ISelectionItemProvider.SelectionContainer => _parent;

    void ISelectionItemProvider.AddToSelection()
    {
        _owner.AutomationAddRowToSelection(_rowIndex);
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        _owner.AutomationRemoveRowFromSelection(_rowIndex);
    }

    void ISelectionItemProvider.Select()
    {
        _owner.AutomationSelectSingleRow(_rowIndex);
    }

    void IExpandCollapseProvider.Expand()
    {
        _owner.AutomationSetRowExpansion(_rowIndex, expand: true);
    }

    void IExpandCollapseProvider.Collapse()
    {
        _owner.AutomationSetRowExpansion(_rowIndex, expand: false);
    }

    ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
    {
        get
        {
            if (!_owner.TryGetRowForAutomation(_rowIndex, out var row) || !row.HasChildren)
            {
                return ExpandCollapseState.LeafNode;
            }

            return row.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }
    }

    bool IExpandCollapseProvider.ShowsMenu => false;
}

internal sealed class FastTreeDataGridCellAutomationPeer : AutomationPeer
{
    private readonly FastTreeDataGridRowAutomationPeer _rowPeer;
    private readonly FastTreeDataGrid _owner;
    private readonly int _rowIndex;
    private readonly int _columnIndex;

    public FastTreeDataGridCellAutomationPeer(FastTreeDataGridRowAutomationPeer rowPeer, FastTreeDataGrid owner, int rowIndex, int columnIndex)
    {
        _rowPeer = rowPeer ?? throw new ArgumentNullException(nameof(rowPeer));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _rowIndex = rowIndex;
        _columnIndex = columnIndex;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetClassNameCore() => "FastTreeDataGridCell";

    protected override AutomationPeer? GetParentCore() => _rowPeer;

    protected override string? GetNameCore()
    {
        if (_columnIndex < 0 || _columnIndex >= _owner.Columns.Count)
        {
            return string.Empty;
        }

        if (!_owner.TryGetRowForAutomation(_rowIndex, out var row))
        {
            return string.Empty;
        }

        var column = _owner.Columns[_columnIndex];
        var value = _owner.GetAutomationCellText(row, column);
        var header = column.Header?.ToString();
        return string.IsNullOrWhiteSpace(header) ? value : $"{header}: {value}";
    }

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => Array.Empty<AutomationPeer>();

    protected override void BringIntoViewCore() => _owner.ScrollRowIntoViewForAutomation(_rowIndex);

    protected override string GetAcceleratorKeyCore() => string.Empty;

    protected override string GetAccessKeyCore() => string.Empty;

    protected override Rect GetBoundingRectangleCore()
    {
        return _owner.GetCellBoundsForAutomation(_rowIndex, _columnIndex);
    }

    protected override string GetAutomationIdCore() => $"Cell{_rowIndex}_{_columnIndex}";

    protected override AutomationPeer? GetLabeledByCore() => null;

    protected override string GetLocalizedControlTypeCore() => "data cell";

    protected override string GetHelpTextCore() => string.Empty;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override bool HasKeyboardFocusCore() => false;

    protected override bool IsKeyboardFocusableCore() => false;

    protected override bool IsEnabledCore() => _owner.IsEffectivelyEnabled;

    protected override void SetFocusCore()
    {
        _owner.SetCurrentValue(FastTreeDataGrid.SelectedIndexProperty, _rowIndex);
    }

    protected override bool ShowContextMenuCore() => false;

    protected override bool TrySetParent(AutomationPeer? parent) =>
        ReferenceEquals(parent, _rowPeer);
}
