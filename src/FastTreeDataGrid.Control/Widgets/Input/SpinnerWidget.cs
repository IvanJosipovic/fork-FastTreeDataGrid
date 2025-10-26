using System;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

[Flags]
public enum WidgetValidSpinDirections
{
    None = 0,
    Increase = 1,
    Decrease = 2
}

public enum WidgetSpinDirection
{
    Increase = WidgetValidSpinDirections.Increase,
    Decrease = WidgetValidSpinDirections.Decrease
}

public readonly struct WidgetSpinEventArgs
{
    public WidgetSpinEventArgs(WidgetSpinDirection direction, bool usingPointer = false)
    {
        Direction = direction;
        UsingPointer = usingPointer;
    }

    public WidgetSpinDirection Direction { get; }

    public bool UsingPointer { get; }
}

public abstract class SpinnerWidget : TemplatedWidget
{
    private WidgetValidSpinDirections _validSpinDirections = WidgetValidSpinDirections.Increase | WidgetValidSpinDirections.Decrease;

    public event EventHandler<WidgetSpinEventArgs>? Spin;

    public WidgetValidSpinDirections ValidSpinDirections
    {
        get => _validSpinDirections;
        set
        {
            if (_validSpinDirections == value)
            {
                return;
            }

            var old = _validSpinDirections;
            _validSpinDirections = value;
            OnValidSpinDirectionsChanged(old, value);
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value))
            {
                return;
            }
        }

        ApplyValue(item);
    }

    protected bool CanSpin(WidgetSpinDirection direction)
    {
        return (_validSpinDirections & (WidgetValidSpinDirections)direction) != WidgetValidSpinDirections.None;
    }

    protected void RaiseSpin(WidgetSpinDirection direction, bool usingPointer = false)
    {
        if (!CanSpin(direction))
        {
            return;
        }

        Spin?.Invoke(this, new WidgetSpinEventArgs(direction, usingPointer));
    }

    protected virtual void OnValidSpinDirectionsChanged(WidgetValidSpinDirections oldValue, WidgetValidSpinDirections newValue)
    {
    }

    private bool ApplyValue(object? value)
    {
        if (value is SpinnerWidgetValue spinner)
        {
            if (spinner.ValidSpinDirections.HasValue)
            {
                ValidSpinDirections = spinner.ValidSpinDirections.Value;
            }

            return true;
        }

        return false;
    }
}
