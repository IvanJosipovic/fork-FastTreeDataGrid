using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using FastTreeDataGrid.Control.Infrastructure;
using Avalonia.Threading;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.Extensibility;

/// <summary>
/// Aggregates the extensibility demo state: custom virtualization provider registration,
/// selection model, and helper commands exposed to the XAML view.
/// </summary>
public sealed class ExtensibilitySamplesViewModel : IDisposable
{
    private readonly DelegateCommand _addItemCommand;
    private readonly DelegateCommand _deleteSelectionCommand;
    private bool _disposed;
    private readonly Random _random = new(8675309);

    public ExtensibilitySamplesViewModel()
    {
        Inventory = new InventoryDataService();
        VirtualizationSettings = new FastTreeDataGridVirtualizationSettings
        {
            PageSize = 128,
            PrefetchRadius = 3,
            MaxPages = 64,
            MaxConcurrentLoads = 6,
            ResetThrottleDelayMilliseconds = 120,
        };

        InventoryProvider = new InventoryVirtualizationProvider(Inventory, VirtualizationSettings);
        SelectionModel = new InventorySelectionModel(Inventory);
        SelectionModel.SelectionChanged += OnSelectionChanged;

        _addItemCommand = new DelegateCommand(_ => AddItemAsync(), _ => true);
        _deleteSelectionCommand = new DelegateCommand(_ => DeleteSelectionAsync(), _ => SelectionModel.SelectedIndices.Count > 0);
    }

    public InventoryDataService Inventory { get; }

    public FastTreeDataGridVirtualizationSettings VirtualizationSettings { get; }

    public InventoryVirtualizationProvider InventoryProvider { get; }

    public InventorySelectionModel SelectionModel { get; }

    public IReadOnlyList<string> Categories => Inventory.Categories;

    public ICommand AddItemCommand => _addItemCommand;

    public ICommand DeleteSelectionCommand => _deleteSelectionCommand;

    public string ProviderSummary =>
        "InventoryVirtualizationProvider demonstrates registering a custom IFastTreeDataVirtualizationProvider " +
        "that listens for service mutations, exposes placeholders, and forwards edits back to the backend.";

    public string EditingSummary =>
        "Rows bind to InventoryRowValueProvider which implements IFastTreeDataGridValueProvider, IEditableObject, " +
        "and INotifyPropertyChanged so editors can commit, cancel, or restore values even while virtualization recycles cells.";

    public string SelectionSummary =>
        "InventorySelectionModel extends FastTreeDataGridSelectionModel to select entire category groups when users " +
        "single-select or toggle individual rows.";

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SelectionModel.SelectionChanged -= OnSelectionChanged;
        InventoryProvider.Dispose();
    }

    public Task CommitEditAsync(InventoryRowValueProvider provider, CancellationToken cancellationToken)
    {
        if (provider is null)
        {
            return Task.CompletedTask;
        }

        return Inventory.UpdateAsync(provider.ToRecord(), cancellationToken);
    }

    private async Task AddItemAsync()
    {
        var category = Categories.Count > 0 ? Categories[_random.Next(Categories.Count)] : "Compute";
        var supplier = $"Inline Supply #{_random.Next(100, 999)}";
        var price = Math.Round((decimal)(_random.NextDouble() * 650.0 + 35.0), 2);
        var stock = _random.Next(0, 250);
        var rating = Math.Round((_random.NextDouble() * 4.0) + 1.0, 1);
        var baseName = $"On-Demand {_random.Next(10, 99):00}";

        var template = new InventoryRecord(
            Id: 0,
            Category: category,
            Name: baseName,
            Supplier: supplier,
            Price: price,
            Stock: stock,
            Rating: rating,
            LastUpdated: DateTimeOffset.UtcNow);

        await Inventory.CreateAsync(template, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task DeleteSelectionAsync()
    {
        var selected = SelectionModel.SelectedIndices;
        if (selected.Count == 0)
        {
            return;
        }

        var snapshot = selected.Distinct().OrderByDescending(i => i).ToArray();
        foreach (var index in snapshot)
        {
            if (Inventory.TryGetRecord(index, out var record))
            {
                await Inventory.DeleteAsync(record.Id, CancellationToken.None).ConfigureAwait(false);
            }
        }

        SelectionModel.Clear();
    }

    private void OnSelectionChanged(object? sender, FastTreeDataGridSelectionChangedEventArgs e)
    {
        _deleteSelectionCommand.RaiseCanExecuteChanged();
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        public DelegateCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
            {
                return false;
            }

            return _canExecute?.Invoke(parameter) ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _executeAsync(parameter).ConfigureAwait(false);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            if (Dispatcher.UIThread.CheckAccess())
            {
                Raise();
            }
            else
            {
                Dispatcher.UIThread.Post(Raise, DispatcherPriority.Normal);
            }
        }
    }
}
