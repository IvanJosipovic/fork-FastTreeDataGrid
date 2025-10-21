using System;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using FastTreeDataGrid.Demo.ViewModels.Widgets;
using Avalonia.Collections;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        FileSystem = new FileSystemTreeSource();
        FilesSource = FileSystem;
        if (FileSystem.RowCount > 0)
        {
            FileSystem.ToggleExpansion(0);
        }

        var countries = DemoDataFactory.CreateCountries();
        var countriesSource = new FastTreeDataGridFlatSource<CountryNode>(countries, node => node.Children);
        ExpandAllNodes(countriesSource);
        CountriesSource = countriesSource;

        Crypto = new CryptoTickersViewModel();

        var widgetNodes = WidgetSamplesFactory.Create();
        var widgetsSource = new FastTreeDataGridFlatSource<WidgetGalleryNode>(widgetNodes, node => node.Children);
        ExpandAllNodes(widgetsSource);
        WidgetsSource = widgetsSource;

        WidgetBoards = new AvaloniaList<WidgetBoard>(WidgetBoardFactory.CreateBoards(widgetNodes));
    }

    public FileSystemTreeSource FileSystem { get; }

    public IFastTreeDataGridSource FilesSource { get; }

    public IFastTreeDataGridSource CountriesSource { get; }

    public CryptoTickersViewModel Crypto { get; }

    public IFastTreeDataGridSource WidgetsSource { get; }

    public AvaloniaList<WidgetBoard> WidgetBoards { get; }

    public void Dispose()
    {
        Crypto.Dispose();
        FileSystem.Dispose();
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
