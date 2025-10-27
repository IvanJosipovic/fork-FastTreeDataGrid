using System;

namespace FastTreeDataGrid.DataSourcesDemo.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        DataSources = new DataSourceSamplesViewModel();
        DynamicDataSources = new DynamicDataSourcesViewModel();
        LiveMutations = new LiveMutationDataSourcesViewModel();
        RowReorder = new RowReorderSamplesViewModel();
    }

    public DataSourceSamplesViewModel DataSources { get; }

    public DynamicDataSourcesViewModel DynamicDataSources { get; }

    public LiveMutationDataSourcesViewModel LiveMutations { get; }

    public RowReorderSamplesViewModel RowReorder { get; }

    public void Dispose()
    {
        DataSources.Dispose();
        DynamicDataSources.Dispose();
        LiveMutations.Dispose();
    }
}
