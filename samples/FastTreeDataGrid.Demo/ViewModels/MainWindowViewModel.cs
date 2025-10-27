using System;
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

        Countries = new CountriesViewModel();

        Crypto = new CryptoTickersViewModel();
        Charts = new ChartSamplesViewModel();
    }

    public FilesViewModel Files { get; }

    public CountriesViewModel Countries { get; }

    public CryptoTickersViewModel Crypto { get; }

    public ChartSamplesViewModel Charts { get; }

    public void Dispose()
    {
        Crypto.Dispose();
        Files.Dispose();
    }
}
