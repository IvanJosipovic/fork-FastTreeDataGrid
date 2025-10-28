using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class DynamicFlatRow : IFastTreeDataGridValueProvider, IEditableObject, INotifyPropertyChanged
{
    public const string KeyOrder = "DynamicFlat.Order";
    public const string KeyCustomer = "DynamicFlat.Customer";
    public const string KeyStatus = "DynamicFlat.Status";
    public const string KeyTotal = "DynamicFlat.Total";
    public const string KeyUpdated = "DynamicFlat.Updated";

    private string _customer;
    private string _status;
    private decimal _total;
    private DateTimeOffset _lastUpdated;
    private EditSnapshot? _snapshot;

    public DynamicFlatRow(int id, string orderNumber, string customer, string status, decimal total, DateTimeOffset lastUpdated)
    {
        Id = id;
        OrderNumber = orderNumber;
        _customer = customer;
        _status = status;
        _total = total;
        _lastUpdated = lastUpdated;
    }

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }

    public string OrderNumber { get; }

    public string Customer
    {
        get => _customer;
        set => SetProperty(ref _customer, value ?? string.Empty, KeyCustomer, nameof(Customer));
    }

    public string StatusText
    {
        get => _status;
        set => SetProperty(ref _status, value ?? string.Empty, KeyStatus, nameof(StatusText));
    }

    public decimal Total
    {
        get => _total;
        set => SetProperty(ref _total, value, KeyTotal, nameof(Total));
    }

    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value, KeyUpdated, nameof(LastUpdated));
    }

    public void Update(string customer, string status, decimal total, DateTimeOffset timestamp)
    {
        Customer = customer;
        StatusText = status;
        Total = total;
        LastUpdated = timestamp;
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyOrder => OrderNumber,
            KeyCustomer => Customer,
            KeyStatus => StatusText,
            KeyTotal => Total.ToString("C", CultureInfo.CurrentCulture),
            KeyUpdated => LastUpdated.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            _ => string.Empty,
        };
    }

    public void NotifyValueChanged(string? key = null)
    {
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, key));
    }

    public void BeginEdit()
    {
        _snapshot ??= new EditSnapshot(_customer, _status, _total);
    }

    public void EndEdit()
    {
        _snapshot = null;
    }

    public void CancelEdit()
    {
        if (_snapshot is not { } snapshot)
        {
            return;
        }

        SetProperty(ref _customer, snapshot.Customer, KeyCustomer, nameof(Customer));
        SetProperty(ref _status, snapshot.Status, KeyStatus, nameof(StatusText));
        SetProperty(ref _total, snapshot.Total, KeyTotal, nameof(Total));
        _snapshot = null;
    }

    private bool SetProperty<T>(ref T storage, T value, string key, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        NotifyValueChanged(key);
        return true;
    }

    private readonly struct EditSnapshot
    {
        public EditSnapshot(string customer, string status, decimal total)
        {
            Customer = customer;
            Status = status;
            Total = total;
        }

        public string Customer { get; }

        public string Status { get; }

        public decimal Total { get; }
    }
}
