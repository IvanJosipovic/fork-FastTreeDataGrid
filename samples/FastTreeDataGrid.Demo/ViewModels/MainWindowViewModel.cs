using System;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using FastTreeDataGrid.Demo.ViewModels.Charts;
using FastTreeDataGrid.Demo.ViewModels.VariableHeights;
using FastTreeDataGrid.Demo.ViewModels.Virtualization;
using FastTreeDataGrid.Demo.ViewModels.Extensibility;

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
        VariableHeightsAdaptive = new VariableHeightRowsViewModel(groupCount: 320, itemsPerGroup: 800);
        DataSources = new DataSourceSamplesViewModel();
        DynamicDataSources = new DynamicDataSourcesViewModel();
        LiveMutations = new LiveMutationDataSourcesViewModel();
        Virtualization = new VirtualizationSamplesViewModel();
        Adapters = new AdaptersSamplesViewModel();
        Extensibility = new ExtensibilitySamplesViewModel();
    }

    public FilesViewModel Files { get; }

    public CountriesViewModel Countries { get; }

    public CryptoTickersViewModel Crypto { get; }

    public ChartSamplesViewModel Charts { get; }

    public VariableHeightRowsViewModel VariableHeights { get; }

    public VariableHeightRowsViewModel VariableHeightsAdaptive { get; }

    public DataSourceSamplesViewModel DataSources { get; }

    public DynamicDataSourcesViewModel DynamicDataSources { get; }

    public LiveMutationDataSourcesViewModel LiveMutations { get; }

    public VirtualizationSamplesViewModel Virtualization { get; }

    public AdaptersSamplesViewModel Adapters { get; }

    public ExtensibilitySamplesViewModel Extensibility { get; }

    public void Dispose()
    {
        Crypto.Dispose();
        Files.Dispose();
        DataSources.Dispose();
        DynamicDataSources.Dispose();
        LiveMutations.Dispose();
        Extensibility.Dispose();
    }
}
