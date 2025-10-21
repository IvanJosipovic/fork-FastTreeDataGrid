using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.VariableHeights;

public sealed class VariableHeightSampleRow : IFastTreeDataGridValueProvider, IFastTreeDataGridRowHeightAware, IFastTreeDataGridGroup
{
    public const string KeyTitle = "VariableHeights.Title";
    public const string KeyCategory = "VariableHeights.Category";
    public const string KeyHeight = "VariableHeights.Height";
    public const string KeyDetails = "VariableHeights.Details";

    public VariableHeightSampleRow(
        string title,
        string category,
        double height,
        bool isGroup,
        string details,
        IReadOnlyList<VariableHeightSampleRow>? children = null)
    {
        Title = title;
        Category = category;
        Height = height;
        IsGroup = isGroup;
        Details = details;
        Children = children ?? Array.Empty<VariableHeightSampleRow>();
    }

    public string Title { get; }

    public string Category { get; }

    public double Height { get; }

    public string Details { get; }

    public bool IsGroup { get; }

    public IReadOnlyList<VariableHeightSampleRow> Children { get; }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyTitle => Title,
            KeyCategory => Category,
            KeyHeight => Height.ToString("0.0 px", CultureInfo.InvariantCulture),
            KeyDetails => Details,
            _ => null,
        };
    }

    public double GetRowHeight(double defaultRowHeight)
    {
        var height = Height;
        if (IsGroup)
        {
            return Math.Max(defaultRowHeight, height);
        }

        return Math.Max(20d, height);
    }
}
