using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using RepeatButtonPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.RepeatButtonPalette;
using SpinnerPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.SpinnerPalette;

namespace FastTreeDataGrid.Control.Widgets;

public enum ButtonSpinnerLocation
{
    Left,
    Right
}

public class ButtonSpinnerWidget : SpinnerWidget
{
    private BorderWidget? _border;
    private StackLayoutWidget? _root;
    private StackLayoutWidget? _spinnerPanel;
    private BorderWidget? _spinnerHost;
    private SurfaceWidget? _contentHost;
    private RepeatButtonWidget? _increaseButton;
    private RepeatButtonWidget? _decreaseButton;
    private GeometryWidget? _increaseIcon;
    private GeometryWidget? _decreaseIcon;
    private SpinnerCommand? _increaseCommandWrapper;
    private SpinnerCommand? _decreaseCommandWrapper;
    private Widget? _content;
    private WidgetTypography? _typography;
    private WidgetCommandSettings? _increaseCommand;
    private WidgetCommandSettings? _decreaseCommand;
    private bool _showSpinner = true;
    private ButtonSpinnerLocation _spinnerLocation = ButtonSpinnerLocation.Right;

    static ButtonSpinnerWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                string.Empty,
                new WidgetStyleRule(
                    typeof(ButtonSpinnerWidget),
                    state,
                    (widget, theme) =>
                    {
                        if (widget is not ButtonSpinnerWidget spinner)
                        {
                            return;
                        }

                        spinner.ApplyPalette(theme.Palette.Spinner, theme.Palette.RepeatButton, theme.Palette.Layout, state);
                    }));
        }
    }

    public bool ShowButtonSpinner
    {
        get => _showSpinner;
        set
        {
            if (_showSpinner == value)
            {
                return;
            }

            _showSpinner = value;
            UpdateSpinnerVisibility();
            RefreshStyle();
        }
    }

    public ButtonSpinnerLocation SpinnerLocation
    {
        get => _spinnerLocation;
        set
        {
            if (_spinnerLocation == value)
            {
                return;
            }

            _spinnerLocation = value;
            UpdateSpinnerOrder();
            RefreshStyle();
        }
    }

    public Widget? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(_content, value))
            {
                return;
            }

            _content = value;
            UpdateContentHost();
            ApplyTypography();
        }
    }

    protected override Widget? CreateDefaultTemplate()
    {
        _contentHost = new SurfaceWidget();

        _increaseIcon = new GeometryWidget { Stretch = Stretch.Uniform, Padding = 6 };
        _decreaseIcon = new GeometryWidget { Stretch = Stretch.Uniform, Padding = 6 };

        _increaseButton = CreateSpinnerButton(WidgetSpinDirection.Increase, ref _increaseCommandWrapper, _increaseIcon);
        _decreaseButton = CreateSpinnerButton(WidgetSpinDirection.Decrease, ref _decreaseCommandWrapper, _decreaseIcon);

        _spinnerPanel = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };
        _spinnerPanel.Children.Add(_increaseButton);
        _spinnerPanel.Children.Add(_decreaseButton);

        _spinnerHost = new BorderWidget
        {
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Child = _spinnerPanel
        };

        _root = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0
        };

        _border = new BorderWidget
        {
            Child = _root,
            BorderThickness = new Thickness(1)
        };

        UpdateSpinnerOrder();
        UpdateSpinnerVisibility();

        return _border;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        _border = templateRoot as BorderWidget ?? _border;
        if (_border is not null && _root is null)
        {
            _root = _border.Child as StackLayoutWidget;
        }

        _root ??= new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0
        };

        if (_border is not null)
        {
            _border.Child = _root;
        }

        _contentHost ??= new SurfaceWidget();
        if (!_root.Children.Contains(_contentHost))
        {
            _root.Children.Add(_contentHost);
        }

        _spinnerPanel ??= new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        };

        if (!_spinnerPanel.Children.Contains(_increaseButton ??= CreateSpinnerButton(WidgetSpinDirection.Increase, ref _increaseCommandWrapper, _increaseIcon ??= new GeometryWidget { Stretch = Stretch.Uniform, Padding = 6 })))
        {
            _spinnerPanel.Children.Add(_increaseButton);
        }

        if (!_spinnerPanel.Children.Contains(_decreaseButton ??= CreateSpinnerButton(WidgetSpinDirection.Decrease, ref _decreaseCommandWrapper, _decreaseIcon ??= new GeometryWidget { Stretch = Stretch.Uniform, Padding = 6 })))
        {
            _spinnerPanel.Children.Add(_decreaseButton);
        }

        _spinnerHost ??= new BorderWidget
        {
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
        _spinnerHost.Child = _spinnerPanel;

        UpdateContentHost();
        UpdateSpinnerOrder();
        UpdateSpinnerVisibility();
        UpdateButtonEnabledStates();
        ApplySpinnerCommands();
        RefreshStyle();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        _typography = null;
        _increaseCommand = null;
        _decreaseCommand = null;
        var showSpinner = ShowButtonSpinner;
        var location = SpinnerLocation;
        Widget? content = _content;
        var validDirections = (WidgetValidSpinDirections?)null;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref showSpinner, ref location, ref content, ref validDirections))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref showSpinner, ref location, ref content, ref validDirections))
        {
            goto Apply;
        }

    Apply:
        ShowButtonSpinner = showSpinner;
        SpinnerLocation = location;
        Content = content;
        if (validDirections.HasValue)
        {
            ValidSpinDirections = validDirections.Value;
        }
        ApplyTypography();
        ApplySpinnerCommands();
        RefreshStyle();
    }

    protected override void OnValidSpinDirectionsChanged(WidgetValidSpinDirections oldValue, WidgetValidSpinDirections newValue)
    {
        UpdateButtonEnabledStates();
    }

    private bool ApplyValue(object? value, ref bool showSpinner, ref ButtonSpinnerLocation location, ref Widget? content, ref WidgetValidSpinDirections? validDirections)
    {
        switch (value)
        {
            case ButtonSpinnerWidgetValue spinnerValue:
                if (spinnerValue.ShowSpinner.HasValue)
                {
                    showSpinner = spinnerValue.ShowSpinner.Value;
                }

                if (spinnerValue.SpinnerLocation.HasValue)
                {
                    location = spinnerValue.SpinnerLocation.Value;
                }

                if (spinnerValue.Typography is not null)
                {
                    _typography = spinnerValue.Typography;
                }

                _increaseCommand = spinnerValue.IncreaseCommand;
                _decreaseCommand = spinnerValue.DecreaseCommand;

                if (spinnerValue.ValidSpinDirections.HasValue)
                {
                    validDirections = spinnerValue.ValidSpinDirections.Value;
                }

                content = ResolveContent(spinnerValue.Content, spinnerValue.ContentTemplate, spinnerValue.ContentFactory, content);
                return true;
            case IWidgetTemplate template:
                content = template.Build();
                return true;
            case Func<Widget?> factory:
                content = factory();
                return true;
            case Widget widget:
                content = widget;
                return true;
            case string text:
                content = CreateTextContent(text);
                return true;
        }

        return false;
    }

    private static Widget? ResolveContent(Widget? content, IWidgetTemplate? template, Func<Widget?>? factory, Widget? existing)
    {
        if (content is not null)
        {
            return content;
        }

        if (template is not null)
        {
            return template.Build();
        }

        if (factory is not null)
        {
            return factory();
        }

        return existing;
    }

    private Widget CreateTextContent(string text)
    {
        var widget = new AccessTextWidget
        {
            TextAlignment = TextAlignment.Left,
            Trimming = TextTrimming.None,
            ClipToBounds = false
        };
        widget.SetAccessText(text);
        return widget;
    }

    private RepeatButtonWidget CreateSpinnerButton(WidgetSpinDirection direction, ref SpinnerCommand? commandWrapper, GeometryWidget icon)
    {
        var button = new RepeatButtonWidget
        {
            Delay = TimeSpan.FromMilliseconds(400),
            Interval = TimeSpan.FromMilliseconds(75),
            Padding = new Thickness(6, 4),
            MinWidth = 30
        };

        commandWrapper = new SpinnerCommand(this, direction);
        button.Command = commandWrapper;
        button.SetContentWidget(icon);
        var commandLabel = direction == WidgetSpinDirection.Increase ? "Increase" : "Decrease";
        button.Automation.Name = commandLabel;
        button.Automation.CommandLabel = commandLabel;

        return button;
    }

    private void ApplyPalette(SpinnerPalette spinnerPalette, RepeatButtonPalette repeatPalette, WidgetFluentPalette.LayoutPalette layout, WidgetVisualState state)
    {
        if (_border is not null)
        {
            _border.Background = spinnerPalette.Background;
            _border.BorderBrush = spinnerPalette.BorderBrush;
            _border.BorderThickness = spinnerPalette.BorderThickness;
            _border.CornerRadius = layout.ControlCornerRadius;
            _border.Padding = layout.ContentPadding;
        }

        if (_spinnerHost is not null)
        {
            _spinnerHost.Background = spinnerPalette.TrackBrush ?? spinnerPalette.Background;
            _spinnerHost.BorderBrush = spinnerPalette.TrackStrokeBrush ?? spinnerPalette.BorderBrush;
            var strokeThickness = Math.Max(0, spinnerPalette.TrackStrokeThickness);
            _spinnerHost.BorderThickness = _spinnerLocation == ButtonSpinnerLocation.Left
                ? new Thickness(0, 0, strokeThickness, 0)
                : new Thickness(strokeThickness, 0, 0, 0);

            var corner = layout.ControlCornerRadius;
            _spinnerHost.CornerRadius = _spinnerLocation == ButtonSpinnerLocation.Left
                ? new CornerRadius(corner.TopLeft, 0, 0, corner.BottomLeft)
                : new CornerRadius(0, corner.TopRight, corner.BottomRight, 0);
        }

        if (_root is not null)
        {
            _root.Spacing = layout.DefaultSpacing;
        }

        UpdateGlyph(_increaseIcon, spinnerPalette.IncreaseGlyph, spinnerPalette.Foreground);
        UpdateGlyph(_decreaseIcon, spinnerPalette.DecreaseGlyph, spinnerPalette.Foreground);

        ApplyButtonPalette(_increaseButton, repeatPalette, state);
        ApplyButtonPalette(_decreaseButton, repeatPalette, state);

        if (_contentHost is not null)
        {
            foreach (var child in _contentHost.Children)
            {
                if (child.Foreground is null)
                {
                    child.Foreground = spinnerPalette.Foreground;
                }
            }
        }

        UpdateButtonEnabledStates();
    }

    private static void UpdateGlyph(GeometryWidget? widget, Geometry? geometry, ImmutableSolidColorBrush? foreground)
    {
        if (widget is null)
        {
            return;
        }

        widget.Foreground = foreground;
        widget.SetGeometry(geometry, Stretch.Uniform, foreground, null, 4);
    }

    private void ApplyButtonPalette(RepeatButtonWidget? button, RepeatButtonPalette palette, WidgetVisualState state)
    {
        if (button is null)
        {
            return;
        }

        var foreground = palette.Foreground.Get(state) ?? palette.Foreground.Normal;

        if (button == _increaseButton && _increaseIcon is not null)
        {
            _increaseIcon.Foreground = foreground;
        }

        if (button == _decreaseButton && _decreaseIcon is not null)
        {
            _decreaseIcon.Foreground = foreground;
        }
    }

    private void UpdateContentHost()
    {
        if (_contentHost is null)
        {
            ApplyTemplate();
            if (_contentHost is null)
            {
                return;
            }
        }

        _contentHost.Children.Clear();
        if (_content is not null)
        {
            _contentHost.Children.Add(_content);
        }
    }

    private void UpdateSpinnerOrder()
    {
        if (_root is null || _contentHost is null || _spinnerHost is null)
        {
            return;
        }

        _root.Children.Clear();
        if (_spinnerLocation == ButtonSpinnerLocation.Left)
        {
            if (ShowButtonSpinner)
            {
                _root.Children.Add(_spinnerHost);
            }

            _root.Children.Add(_contentHost);
        }
        else
        {
            _root.Children.Add(_contentHost);
            if (ShowButtonSpinner)
            {
                _root.Children.Add(_spinnerHost);
            }
        }
    }

    private void UpdateSpinnerVisibility()
    {
        UpdateSpinnerOrder();
    }

    private void UpdateButtonEnabledStates()
    {
        var increaseEnabled = IsEnabled && CanSpin(WidgetSpinDirection.Increase) && (_increaseCommand?.IsEnabled ?? true);
        var decreaseEnabled = IsEnabled && CanSpin(WidgetSpinDirection.Decrease) && (_decreaseCommand?.IsEnabled ?? true);

        if (_increaseButton is not null)
        {
            _increaseButton.IsEnabled = increaseEnabled;
        }

        if (_decreaseButton is not null)
        {
            _decreaseButton.IsEnabled = decreaseEnabled;
        }

        _increaseCommandWrapper?.RaiseCanExecuteChanged();
        _decreaseCommandWrapper?.RaiseCanExecuteChanged();
    }

    private void ApplyTypography()
    {
        if (_typography is null || _contentHost is null)
        {
            return;
        }

        foreach (var child in _contentHost.Children)
        {
            if (child is TextWidget text)
            {
                if (_typography.FontFamily is not null)
                {
                    text.FontFamily = _typography.FontFamily;
                }

                if (_typography.FontSize.HasValue)
                {
                    text.EmSize = Math.Max(1, _typography.FontSize.Value);
                }

                if (_typography.FontWeight.HasValue)
                {
                    text.FontWeight = _typography.FontWeight.Value;
                }

                if (_typography.FontStyle.HasValue)
                {
                    text.FontStyle = _typography.FontStyle.Value;
                }

                if (_typography.FontStretch.HasValue)
                {
                    text.FontStretch = _typography.FontStretch.Value;
                }
            }
        }
    }

    private void ApplySpinnerCommands()
    {
        ApplyCommandSettings(_increaseCommandWrapper, _increaseCommand);
        ApplyCommandSettings(_decreaseCommandWrapper, _decreaseCommand);
    }

    private void ApplyCommandSettings(SpinnerCommand? wrapper, WidgetCommandSettings? settings)
    {
        if (wrapper is null)
        {
            return;
        }

        wrapper.Settings = settings;
        wrapper.RaiseCanExecuteChanged();
    }

    private sealed class SpinnerCommand : ICommand
    {
        private readonly ButtonSpinnerWidget _owner;
        private readonly WidgetSpinDirection _direction;

        public SpinnerCommand(ButtonSpinnerWidget owner, WidgetSpinDirection direction)
        {
            _owner = owner;
            _direction = direction;
        }

        public WidgetCommandSettings? Settings { get; set; }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (Settings is not null && !Settings.IsEnabled)
            {
                return false;
            }

            return _owner.IsEnabled && _owner.CanSpin(_direction);
        }

        public void Execute(object? parameter)
        {
            _owner.RaiseSpin(_direction, usingPointer: true);

            if (Settings?.Command is { } command)
            {
                var commandParameter = Settings.CommandParameter ?? parameter;
                if (command.CanExecute(commandParameter))
                {
                    command.Execute(commandParameter);
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
