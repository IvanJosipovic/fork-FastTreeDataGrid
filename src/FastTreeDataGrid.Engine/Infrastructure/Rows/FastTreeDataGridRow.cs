using System;
using System.ComponentModel;

namespace FastTreeDataGrid.Engine.Infrastructure;

public sealed class FastTreeDataGridRow
{
    public FastTreeDataGridRow(object? item, int level, bool hasChildren, bool isExpanded, Action? requestMeasureCallback)
    {
        Item = item;
        Level = level;
        HasChildren = hasChildren;
        IsExpanded = isExpanded;
        _requestMeasureCallback = requestMeasureCallback;

        if (item is IFastTreeDataGridValueProvider provider)
        {
            ValueProvider = provider;
            provider.ValueInvalidated += OnValueInvalidated;
        }

        if (item is IFastTreeDataGridGroup group)
        {
            IsGroup = group.IsGroup;
        }

        if (item is IFastTreeDataGridSummaryRow summary)
        {
            IsSummary = summary.IsSummary;
        }

        if (item is INotifyDataErrorInfo notify)
        {
            notify.ErrorsChanged += OnErrorsChanged;
        }
    }

    private readonly Action? _requestMeasureCallback;

    public object? Item { get; }

    public IFastTreeDataGridValueProvider? ValueProvider { get; }

    public bool IsGroup { get; }

    public bool IsSummary { get; }

    public int Level { get; internal set; }

    public bool HasChildren { get; internal set; }

    public bool IsExpanded { get; internal set; }

    internal void NotifyChanged()
    {
        _requestMeasureCallback?.Invoke();
    }

    private void OnValueInvalidated(object? sender, ValueInvalidatedEventArgs e)
    {
        _requestMeasureCallback?.Invoke();
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        _requestMeasureCallback?.Invoke();
    }
}
