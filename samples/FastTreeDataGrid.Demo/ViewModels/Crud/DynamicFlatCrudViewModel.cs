using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Demo.ViewModels;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class DynamicFlatCrudViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly FastTreeDataGridFlatSource<DynamicFlatRow> _source;
    private readonly AsyncCommand _addOrderCommand;
    private readonly AsyncCommand _saveOrderCommand;
    private readonly AsyncCommand _deleteOrderCommand;
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<string> _statuses = new() { "Pending", "Processing", "Shipped", "Completed" };
    private List<DynamicFlatRow> _rows = new();
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private DynamicFlatRow? _selectedRow;
    private string _newOrderCustomer = string.Empty;
    private string _newOrderTotalText = "120.00";
    private string _newOrderStatus = "Pending";
    private string _editOrderCustomer = string.Empty;
    private string _editOrderTotalText = string.Empty;
    private string _editOrderStatus = "Pending";
    private string _status = "Ready";
    private int _nextOrderNumber = 1001;
    private bool _disposed;

    public DynamicFlatCrudViewModel()
    {
        _source = new FastTreeDataGridFlatSource<DynamicFlatRow>(_rows, _ => Array.Empty<DynamicFlatRow>());
        _addOrderCommand = new AsyncCommand(_ => AddOrderAsync(), _ => CanAddOrder());
        _saveOrderCommand = new AsyncCommand(_ => SaveOrderAsync(), _ => CanSaveOrder());
        _deleteOrderCommand = new AsyncCommand(_ => DeleteOrderAsync(), _ => _selectedRow is not null);

        SeedInitialOrders();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public AsyncCommand AddOrderCommand => _addOrderCommand;

    public AsyncCommand SaveOrderCommand => _saveOrderCommand;

    public AsyncCommand DeleteOrderCommand => _deleteOrderCommand;

    public IReadOnlyList<int> SelectedIndices
    {
        get => _selectedIndices;
        set
        {
            var normalized = value ?? Array.Empty<int>();
            if (SetProperty(ref _selectedIndices, normalized, nameof(SelectedIndices)))
            {
                UpdateSelection();
            }
        }
    }

    public IReadOnlyList<string> Statuses => _statuses;

    public string NewOrderCustomer
    {
        get => _newOrderCustomer;
        set
        {
            if (SetProperty(ref _newOrderCustomer, value ?? string.Empty, nameof(NewOrderCustomer)))
            {
                _addOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewOrderTotalText
    {
        get => _newOrderTotalText;
        set
        {
            if (SetProperty(ref _newOrderTotalText, value ?? string.Empty, nameof(NewOrderTotalText)))
            {
                _addOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewOrderStatus
    {
        get => _newOrderStatus;
        set
        {
            if (SetProperty(ref _newOrderStatus, value ?? string.Empty, nameof(NewOrderStatus)))
            {
                _addOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditOrderCustomer
    {
        get => _editOrderCustomer;
        set
        {
            if (SetProperty(ref _editOrderCustomer, value ?? string.Empty, nameof(EditOrderCustomer)))
            {
                _saveOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditOrderTotalText
    {
        get => _editOrderTotalText;
        set
        {
            if (SetProperty(ref _editOrderTotalText, value ?? string.Empty, nameof(EditOrderTotalText)))
            {
                _saveOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditOrderStatus
    {
        get => _editOrderStatus;
        set
        {
            if (SetProperty(ref _editOrderStatus, value ?? string.Empty, nameof(EditOrderStatus)))
            {
                _saveOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value, nameof(Status));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void SeedInitialOrders()
    {
        _rows = new List<DynamicFlatRow>
        {
            CreateOrder("Lena Ortiz", "Pending", 189.00m),
            CreateOrder("Marcus Shaw", "Processing", 249.50m),
            CreateOrder("Rei Nakamura", "Shipped", 459.20m),
            CreateOrder("Farah Idris", "Pending", 129.99m),
        };

        _source.Reset(_rows, preserveExpansion: false);
    }

    private DynamicFlatRow CreateOrder(string customer, string status, decimal total)
    {
        var id = _nextOrderNumber++;
        var orderNumber = $"SO-{id:0000}";
        return new DynamicFlatRow(id, orderNumber, customer, status, total, DateTimeOffset.UtcNow);
    }

    private Task AddOrderAsync()
    {
        var customer = NewOrderCustomer?.Trim();
        if (string.IsNullOrEmpty(customer))
        {
            Status = "Customer name cannot be empty.";
            return Task.CompletedTask;
        }

        if (!TryParseTotal(NewOrderTotalText, out var total))
        {
            Status = "Enter a valid order total.";
            return Task.CompletedTask;
        }

        var status = string.IsNullOrWhiteSpace(NewOrderStatus) ? "Pending" : NewOrderStatus;
        var row = CreateOrder(customer, status, total);
        _rows.Insert(0, row);
        _source.Reset(_rows, preserveExpansion: false);
        Status = $"Created order {row.OrderNumber}.";
        NewOrderCustomer = string.Empty;
        return Task.CompletedTask;
    }

    private Task SaveOrderAsync()
    {
        var row = _selectedRow;
        if (row is null)
        {
            return Task.CompletedTask;
        }

        var customer = EditOrderCustomer?.Trim();
        if (string.IsNullOrEmpty(customer))
        {
            Status = "Customer name cannot be empty.";
            return Task.CompletedTask;
        }

        if (!TryParseTotal(EditOrderTotalText, out var total))
        {
            Status = "Enter a valid order total.";
            return Task.CompletedTask;
        }

        var status = string.IsNullOrWhiteSpace(EditOrderStatus) ? "Pending" : EditOrderStatus;
        row.Update(customer, status, total, DateTimeOffset.UtcNow);
        Status = $"Updated order {row.OrderNumber}.";
        _source.Reset(_rows, preserveExpansion: false);
        return Task.CompletedTask;
    }

    private Task DeleteOrderAsync()
    {
        var row = _selectedRow;
        if (row is null)
        {
            return Task.CompletedTask;
        }

        _rows.Remove(row);
        _source.Reset(_rows, preserveExpansion: false);
        Status = $"Deleted order {row.OrderNumber}.";
        SelectedIndices = Array.Empty<int>();
        return Task.CompletedTask;
    }

    public Task CommitEditAsync(DynamicFlatRow row, CancellationToken cancellationToken)
    {
        if (row is null)
        {
            return Task.CompletedTask;
        }

        var customer = string.IsNullOrWhiteSpace(row.Customer) ? "Guest" : row.Customer.Trim();
        if (!string.Equals(row.Customer, customer, StringComparison.CurrentCulture))
        {
            row.Customer = customer;
        }

        var status = string.IsNullOrWhiteSpace(row.StatusText) ? "Pending" : row.StatusText.Trim();
        if (!string.Equals(row.StatusText, status, StringComparison.CurrentCulture))
        {
            row.StatusText = status;
        }

        var total = row.Total < 0 ? 0m : decimal.Round(row.Total, 2);
        if (row.Total != total)
        {
            row.Total = total;
        }

        row.LastUpdated = DateTimeOffset.UtcNow;
        Status = $"Updated order {row.OrderNumber}.";
        return Task.CompletedTask;
    }

    private bool CanAddOrder()
    {
        if (string.IsNullOrWhiteSpace(NewOrderCustomer))
        {
            return false;
        }

        return TryParseTotal(NewOrderTotalText, out _);
    }

    private bool CanSaveOrder()
    {
        if (_selectedRow is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditOrderCustomer))
        {
            return false;
        }

        return TryParseTotal(EditOrderTotalText, out _);
    }

    private void UpdateSelection()
    {
        DynamicFlatRow? selected = null;
        if (_selectedIndices.Count > 0)
        {
            var index = _selectedIndices[^1];
            if (_source.TryGetMaterializedRow(index, out var row) && row.Item is DynamicFlatRow item)
            {
                selected = item;
            }
        }

        _selectedRow = selected;
        if (selected is null)
        {
            EditOrderCustomer = string.Empty;
            EditOrderTotalText = string.Empty;
            EditOrderStatus = "Pending";
        }
        else
        {
            EditOrderCustomer = selected.Customer;
            EditOrderTotalText = selected.Total.ToString("F2", CultureInfo.CurrentCulture);
            EditOrderStatus = selected.StatusText;
        }

        _saveOrderCommand.RaiseCanExecuteChanged();
        _deleteOrderCommand.RaiseCanExecuteChanged();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        var row = _rows[_random.Next(_rows.Count)];
        var delta = (decimal)(_random.NextDouble() * 40.0 - 20.0);
        var newTotal = Math.Max(25m, row.Total + decimal.Round(delta, 2));
        var status = NextStatus(row.StatusText);
        row.Update(row.Customer, status, newTotal, DateTimeOffset.UtcNow);
        Status = $"Auto-updated {row.OrderNumber} to {status}.";
    }

    private string NextStatus(string current)
    {
        var index = _statuses.IndexOf(current);
        if (index < 0 || index >= _statuses.Count - 1)
        {
            return _statuses.Last();
        }

        return _statuses[index + 1];
    }

    private static bool TryParseTotal(string? text, out decimal total) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out total) && total >= 0m;

    private bool SetProperty<T>(ref T storage, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
