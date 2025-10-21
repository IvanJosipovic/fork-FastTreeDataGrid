using System;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using Avalonia.Collections;
using FastTreeDataGrid.Demo.ViewModels.Charts;
using FastTreeDataGrid.Demo.ViewModels.VariableHeights;
using FastTreeDataGrid.Demo.ViewModels.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        var fileSystem = new FileSystemTreeSource();
        Files = new FilesViewModel(fileSystem);
        if (Files.Source.RowCount > 0)
        {
            Files.Source.ToggleExpansion(0);
        }

        Countries = new CountriesViewModel(DemoDataFactory.CreateCountries());

        Crypto = new CryptoTickersViewModel();
        Charts = new ChartSamplesViewModel();
        VariableHeights = new VariableHeightRowsViewModel();
        VariableHeightsAdaptive = new VariableHeightRowsViewModel(groupCount: 32, itemsPerGroup: 800);

        var widgetNodes = WidgetSamplesFactory.Create();
        var widgetsSource = new FastTreeDataGridFlatSource<WidgetGalleryNode>(widgetNodes, node => node.Children);
        ExpandAllNodes(widgetsSource);
        WidgetsSource = widgetsSource;

        WidgetBoards = new AvaloniaList<WidgetBoard>(WidgetBoardFactory.CreateBoards(widgetNodes));
    }

    public FilesViewModel Files { get; }

    public CountriesViewModel Countries { get; }

    public CryptoTickersViewModel Crypto { get; }

    public ChartSamplesViewModel Charts { get; }

    public VariableHeightRowsViewModel VariableHeights { get; }

    public VariableHeightRowsViewModel VariableHeightsAdaptive { get; }

    public IFastTreeDataGridSource WidgetsSource { get; }

    public AvaloniaList<WidgetBoard> WidgetBoards { get; }

    public void Dispose()
    {
        Crypto.Dispose();
        Files.Dispose();
    }

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
