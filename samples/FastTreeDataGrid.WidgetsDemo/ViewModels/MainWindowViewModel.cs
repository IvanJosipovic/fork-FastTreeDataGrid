using System;
using System.Linq;
using FastTreeDataGrid.Control.Infrastructure;
using Avalonia.Collections;
using FastTreeDataGrid.WidgetsDemo.ViewModels.Widgets;

namespace FastTreeDataGrid.WidgetsDemo.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel()
    {
        var widgetNodes = WidgetSamplesFactory.Create();
        var widgetsSource = new FastTreeDataGridFlatSource<WidgetGalleryNode>(widgetNodes, node => node.Children);
        ExpandAllNodes(widgetsSource);
        WidgetsSource = widgetsSource;

        var boards = WidgetBoardFactory.CreateBoards(widgetNodes);
        WidgetBoards = new AvaloniaList<WidgetBoard>(boards);
        VirtualizingLayoutBoard = WidgetBoards.FirstOrDefault(b =>
            string.Equals(b.Title, WidgetBoardFactory.VirtualizingBoardTitle, StringComparison.Ordinal));
        WidgetsGalleryScenarios = new AvaloniaList<WidgetsGalleryScenario>(WidgetsGalleryScenarioFactory.Create());
    }

    public IFastTreeDataGridSource WidgetsSource { get; }

    public AvaloniaList<WidgetBoard> WidgetBoards { get; }

    public WidgetBoard? VirtualizingLayoutBoard { get; }

    public AvaloniaList<WidgetsGalleryScenario> WidgetsGalleryScenarios { get; }

    private static void ExpandAllNodes(IFastTreeDataGridSource source)
    {
        for (var i = 0; i < source.RowCount; i++)
        {
            var row = source.GetRow(i);
            if (row.HasChildren && !row.IsExpanded)
            {
                source.ToggleExpansion(i);
            }
        }
    }
}
