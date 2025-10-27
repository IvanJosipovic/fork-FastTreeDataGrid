using System;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class DynamicFlatRow : IFastTreeDataGridValueProvider
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

    public int Id { get; }

    public string OrderNumber { get; }

    public string Customer
    {
        get => _customer;
        private set
        {
            if (!string.Equals(_customer, value, StringComparison.CurrentCulture))
            {
                _customer = value;
                NotifyValueChanged(KeyCustomer);
            }
        }
    }

    public string StatusText
    {
        get => _status;
        private set
        {
            if (!string.Equals(_status, value, StringComparison.CurrentCulture))
            {
                _status = value;
                NotifyValueChanged(KeyStatus);
            }
        }
    }

    public decimal Total
    {
        get => _total;
        private set
        {
            if (_total != value)
            {
                _total = value;
                NotifyValueChanged(KeyTotal);
            }
        }
    }

    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        private set
        {
            if (_lastUpdated != value)
            {
                _lastUpdated = value;
                NotifyValueChanged(KeyUpdated);
            }
        }
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
}
