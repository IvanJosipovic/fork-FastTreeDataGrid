using System;
using Avalonia;
using Avalonia.Layout;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public class SplitButtonWidget : TemplatedWidget
{
    private BorderWidget? _border;
    private StackLayoutWidget? _root;
    private ButtonWidget? _primaryButton;
    private DropDownButtonWidget? _dropDownButton;

    public event EventHandler<WidgetEventArgs>? PrimaryClick
    {
        add
        {
            EnsureTemplate();
            if (_primaryButton is not null)
            {
                _primaryButton.Click += value;
            }
        }
        remove
        {
            if (_primaryButton is not null)
            {
                _primaryButton.Click -= value;
            }
        }
    }

    public event EventHandler<WidgetDropDownEventArgs>? DropDownRequested
    {
        add
        {
            EnsureTemplate();
            if (_dropDownButton is not null)
            {
                _dropDownButton.DropDownRequested += value;
            }
        }
        remove
        {
            if (_dropDownButton is not null)
            {
                _dropDownButton.DropDownRequested -= value;
            }
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        SplitButtonWidgetValue? splitValue = null;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case ToggleSplitButtonWidgetValue toggleValue:
                    splitValue = toggleValue;
                    ApplyToggleState(toggleValue.IsChecked);
                    break;
                case SplitButtonWidgetValue split:
                    splitValue = split;
                    break;
            }
        }

        if (splitValue is null)
        {
            switch (item)
            {
                case ToggleSplitButtonWidgetValue toggleValue:
                    splitValue = toggleValue;
                    ApplyToggleState(toggleValue.IsChecked);
                    break;
                case SplitButtonWidgetValue split:
                    splitValue = split;
                    break;
            }
        }

        if (splitValue is not null)
        {
            ApplySplitValue(splitValue);
        }
        else
        {
            base.UpdateValue(provider, item);
        }
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        EnsureTemplate();
        _border?.Arrange(bounds);
    }

    protected virtual void ApplyToggleState(bool isChecked)
    {
        // Default split button has no toggle state.
    }

    protected virtual ButtonWidget CreatePrimaryButton()
    {
        var button = new ButtonWidget();
        button.Click += (_, args) => OnPrimaryClick(args);
        return button;
    }

    protected virtual DropDownButtonWidget CreateDropDownButton()
    {
        var button = new DropDownButtonWidget
        {
            ExecutePrimaryCommand = false
        };
        button.DropDownRequested += (_, args) => OnDropDownRequested(args);
        return button;
    }

    protected virtual void OnPrimaryClick(WidgetEventArgs args)
    {
        // Consumers can hook PrimaryClick event; default no-op.
    }

    protected virtual void OnDropDownRequested(WidgetDropDownEventArgs args)
    {
        // Consumers can hook DropDownRequested event; default no-op.
    }

    protected ButtonWidget? PrimaryButton => _primaryButton;

    protected DropDownButtonWidget? DropDownButton => _dropDownButton;

    protected override Widget? CreateDefaultTemplate()
    {
        _primaryButton = CreatePrimaryButton();
        _dropDownButton = CreateDropDownButton();
        _dropDownButton.SetText("\u25BE");

        _root = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0
        };
        _root.Children.Add(_primaryButton);
        _root.Children.Add(_dropDownButton);

        _border = new BorderWidget
        {
            Child = _root,
            BorderThickness = new Thickness(1)
        };

        return _border;
    }

    protected void EnsureTemplate()
    {
        if (!IsTemplateApplied)
        {
            ApplyTemplate();
        }

        SyncEnabledState();
    }

    private void SyncEnabledState()
    {
        if (_primaryButton is not null)
        {
            _primaryButton.IsEnabled = IsEnabled;
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.IsEnabled = IsEnabled;
        }
    }

    private void ApplySplitValue(SplitButtonWidgetValue value)
    {
        EnsureTemplate();

        if (_primaryButton is not null)
        {
            _primaryButton.ApplyButtonValue(value.PrimaryButton);
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.ApplyButtonValue(value.DropDownButton);
            _dropDownButton.ApplyDropDownDescriptor(value.DropDownButton);
            _dropDownButton.ExecutePrimaryCommand = value.DropDownButton.ExecutePrimaryCommand;
            if (string.IsNullOrEmpty(value.DropDownButton.Text))
            {
                _dropDownButton.SetText("\u25BE");
            }
        }

        var corner = WidgetFluentPalette.Current.Layout.ControlCornerRadius;
        if (_primaryButton is not null)
        {
            _primaryButton.CornerRadius = new CornerRadius(corner.TopLeft, 0, 0, corner.BottomLeft);
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.CornerRadius = new CornerRadius(0, corner.TopRight, corner.BottomRight, 0);
        }

        SyncEnabledState();

        if (value is ToggleSplitButtonWidgetValue toggleValue)
        {
            ApplyToggleState(toggleValue.IsChecked);
        }
    }
}

public sealed class ToggleSplitButtonWidget : SplitButtonWidget
{
    private bool _isChecked;

    public event EventHandler<WidgetValueChangedEventArgs<bool>>? Toggled;
    public event EventHandler<WidgetEventArgs>? Checked;
    public event EventHandler<WidgetEventArgs>? Unchecked;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetIsChecked(value, true);
    }

    protected override void ApplyToggleState(bool isChecked)
    {
        SetIsChecked(isChecked, false);
    }

    private void SetIsChecked(bool value, bool raise)
    {
        if (_isChecked == value)
        {
            return;
        }

        var old = _isChecked;
        _isChecked = value;
        UpdateToggleAppearance();

        if (raise)
        {
            var args = new WidgetValueChangedEventArgs<bool>(this, old, _isChecked);
            Toggled?.Invoke(this, args);
            if (_isChecked)
            {
                Checked?.Invoke(this, new WidgetEventArgs(this));
            }
            else
            {
                Unchecked?.Invoke(this, new WidgetEventArgs(this));
            }
        }
    }

    private void UpdateToggleAppearance()
    {
        EnsureTemplate();
        if (PrimaryButton is null)
        {
            return;
        }

        if (_isChecked)
        {
            var palette = WidgetFluentPalette.Current.Button.GetVariant(ButtonWidgetVariant.Accent);
            PrimaryButton.Background = palette.Background.Normal;
            PrimaryButton.BorderBrush = palette.Border.Normal;
            PrimaryButton.Foreground = palette.Foreground.Normal;
        }
        else
        {
            PrimaryButton.Background = null;
            PrimaryButton.BorderBrush = null;
            PrimaryButton.Foreground = null;
        }
        PrimaryButton.RefreshStyle();
    }

    protected override void OnPrimaryClick(WidgetEventArgs args)
    {
        SetIsChecked(!_isChecked, true);
    }
}
