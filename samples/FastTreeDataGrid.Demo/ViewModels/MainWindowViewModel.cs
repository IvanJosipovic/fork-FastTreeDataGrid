using System;
using FastTreeDataGrid.Demo.ViewModels.Crud;
using FastTreeDataGrid.Demo.ViewModels.Crypto;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;
using FastTreeDataGrid.Demo.ViewModels.Charts;

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

        var sqliteService = new SqliteCrudService();
        SqliteHierarchy = new SqliteHierarchicalCrudViewModel(sqliteService);
        SqliteFlat = new SqliteFlatCrudViewModel(sqliteService);

        DynamicHierarchy = new DynamicHierarchyCrudViewModel();
        DynamicFlat = new DynamicFlatCrudViewModel();

        Crypto = new CryptoTickersViewModel();
        Charts = new ChartSamplesViewModel();
    }

    public FilesViewModel Files { get; }

    public CountriesViewModel Countries { get; }

    public SqliteHierarchicalCrudViewModel SqliteHierarchy { get; }

    public SqliteFlatCrudViewModel SqliteFlat { get; }

    public DynamicHierarchyCrudViewModel DynamicHierarchy { get; }

    public DynamicFlatCrudViewModel DynamicFlat { get; }

    public CryptoTickersViewModel Crypto { get; }

    public ChartSamplesViewModel Charts { get; }

    public void Dispose()
    {
        DynamicHierarchy.Dispose();
        DynamicFlat.Dispose();
        SqliteHierarchy.Dispose();
        SqliteFlat.Dispose();
        Crypto.Dispose();
        Files.Dispose();
    }
}
