using System;
using System.Collections.Generic;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels.Charts;

internal sealed class ChartSampleNode : IFastTreeDataGridValueProvider
{
    public const string KeyTitle = "Charts.Title";
    public const string KeyDescription = "Charts.Description";
    public const string KeyChart = "Charts.Chart";

    private static readonly ChartSampleNode[] EmptyChildren = Array.Empty<ChartSampleNode>();

    public ChartSampleNode(string title, string description, ChartWidgetValue chart)
    {
        Title = title;
        Description = description;
        Chart = chart;
    }

    public string Title { get; }

    public string Description { get; }

    public ChartWidgetValue Chart { get; }

    public IReadOnlyList<ChartSampleNode> Children => EmptyChildren;

    public object? GetValue(object? item, string key) => key switch
    {
        KeyTitle => Title,
        KeyDescription => Description,
        KeyChart => Chart,
        _ => null,
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }
}
