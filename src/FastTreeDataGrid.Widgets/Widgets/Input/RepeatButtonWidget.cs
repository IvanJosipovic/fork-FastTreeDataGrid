using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Button widget that continuously invokes its command while the pointer remains pressed.
/// Mirrors the behaviour of Avalonia's <c>RepeatButton</c> while reusing the shared command pipeline.
/// </summary>
public sealed class RepeatButtonWidget : ButtonWidgetBase
{
    private const double DefaultMinWidth = 34;
    private static readonly Thickness DefaultPadding = new(6, 4);

    private BorderWidget? _borderPart;
    private SurfaceWidget? _contentHost;
    private AccessTextWidget? _contentPart;
    private ImmutableSolidColorBrush? _backgroundOverride;
    private ImmutableSolidColorBrush? _borderOverride;
    private WidgetTypography? _typography;
    private string _text = string.Empty;
    private DispatcherTimer? _delayTimer;
    private DispatcherTimer? _repeatTimer;

    static RepeatButtonWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(RepeatButtonWidget),
                    state,
                    (widget, theme) =>
                    {
                        if (widget is not RepeatButtonWidget repeat)
                        {
                            return;
                        }

                        repeat.ApplyPalette(theme.Palette.RepeatButton, theme.Palette.Layout, state);
                    }));
        }
    }

    /// <summary>
    /// Gets or sets the initial delay before repeating begins after the first click.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// Gets or sets the interval between repeated clicks.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the text displayed inside the repeat button.
    /// </summary>
    public string? Text
    {
        get => _text;
        set
        {
            var resolved = value ?? string.Empty;
            if (string.Equals(_text, resolved, StringComparison.Ordinal))
            {
                return;
            }

            _text = resolved;
            EnsureContent();
            _contentPart?.SetAccessText(_text);
            UpdateAutomationFromText(_text, _contentPart);
        }
    }

    public void SetContentWidget(Widget? widget)
    {
        EnsureContentHost();
        if (_contentHost is null)
        {
            return;
        }

        _contentHost.Children.Clear();

        if (widget is not null)
        {
            _contentHost.Children.Add(widget);
        }
        else
        {
            EnsureContent();
            if (_contentPart is not null && !_contentHost.Children.Contains(_contentPart))
            {
                _contentHost.Children.Add(_contentPart);
            }
        }

        UpdateAutomationFromText(_text, _contentPart);
    }

    /// <summary>
    /// Gets or sets additional padding applied to the button content.
    /// </summary>
    public Thickness Padding { get; set; } = DefaultPadding;

    /// <summary>
    /// Gets or sets the minimum width for the repeat button.
    /// </summary>
    public double MinWidth { get; set; } = DefaultMinWidth;

    protected override Widget? CreateDefaultTemplate()
    {
        _contentPart = new AccessTextWidget
        {
            TextAlignment = TextAlignment.Center,
            Trimming = TextTrimming.None,
            ClipToBounds = false
        };
        _contentPart.SetAccessText(_text);

        _contentHost = new SurfaceWidget();
        _contentHost.Children.Add(_contentPart);

        _borderPart = new BorderWidget
        {
            BorderThickness = new Thickness(1),
            Padding = Padding,
            DesiredWidth = DesiredWidth,
            DesiredHeight = DesiredHeight,
            Child = _contentHost
        };

        return _borderPart;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        _borderPart = templateRoot as BorderWidget;
        if (_borderPart is not null)
        {
            _borderPart.Padding = Padding;
            _borderPart.BorderThickness = new Thickness(1);
        }

        if (_borderPart?.Child is SurfaceWidget host)
        {
            _contentHost = host;
            if (_contentHost.Children.Count > 0 && _contentHost.Children[0] is AccessTextWidget text)
            {
                _contentPart = text;
            }
        }
        else
        {
            _contentHost ??= new SurfaceWidget();
            if (_contentPart is not null && !_contentHost.Children.Contains(_contentPart))
            {
                _contentHost.Children.Add(_contentPart);
            }

            if (_borderPart is not null)
            {
                _borderPart.Child = _contentHost;
            }
        }

        _contentPart ??= new AccessTextWidget
        {
            TextAlignment = TextAlignment.Center,
            Trimming = TextTrimming.None,
            ClipToBounds = false
        };
        if (_contentHost is not null && !_contentHost.Children.Contains(_contentPart))
        {
            _contentHost.Children.Add(_contentPart);
        }
        EnsureContent();
        UpdateAutomationFromText(_text, _contentPart);
        RefreshStyle();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        _backgroundOverride = null;
        _borderOverride = null;
        _typography = null;
        var enabled = true;
        var command = Command;
        var commandParameter = CommandParameter;
        string text = _text;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value, ref enabled, ref command, ref commandParameter, ref text))
            {
                goto Apply;
            }
        }

        if (ApplyValue(item, ref enabled, ref command, ref commandParameter, ref text))
        {
            goto Apply;
        }

        if (!ReferenceEquals(item, this) && item is not null)
        {
            text = item.ToString() ?? string.Empty;
        }

    Apply:
        Text = text;
        Command = command;
        CommandParameter = commandParameter;
        SetIsEnabledFromData(enabled);
        ApplyTypography();
        RefreshStyle();
    }

    protected override void OnPointerPressed(in WidgetPointerEvent e)
    {
        base.OnPointerPressed(e);
        StartRepeating();
    }

    protected override void OnPointerReleased(bool executedClick, in WidgetPointerEvent e)
    {
        StopRepeating();
        base.OnPointerReleased(executedClick, e);
    }

    protected override void OnPointerCancelled()
    {
        StopRepeating();
        base.OnPointerCancelled();
    }

    protected override bool ShouldExecuteClick(in WidgetPointerEvent e) => false;

    private bool ApplyValue(object? value, ref bool enabled, ref ICommand? command, ref object? commandParameter, ref string text)
    {
        SetAutomationSettings(null);

        switch (value)
        {
            case ButtonWidgetValue buttonValue:
                text = buttonValue.Text;
                enabled = buttonValue.IsEnabled;
                command = buttonValue.Command ?? command;
                commandParameter = buttonValue.CommandParameter ?? commandParameter;
                _backgroundOverride = buttonValue.Background;
                _borderOverride = buttonValue.BorderBrush;
                if (buttonValue.Typography is not null)
                {
                    _typography = buttonValue.Typography;
                }

                SetAutomationSettings(buttonValue.Automation);

                return true;
            case string str:
                text = str;
                return true;
        }

        return false;
    }

    private void EnsureContent()
    {
        if (_contentPart is null)
        {
            ApplyTemplate();
        }

        EnsureContentHost();
    }

    private void EnsureContentHost()
    {
        if (_contentHost is not null)
        {
            return;
        }

        ApplyTemplate();

        if (_contentHost is null)
        {
            _contentHost = new SurfaceWidget();
            if (_contentPart is not null)
            {
                _contentHost.Children.Add(_contentPart);
            }

            if (_borderPart is not null)
            {
                _borderPart.Child = _contentHost;
            }
        }
    }

    private void ApplyTypography()
    {
        if (_contentPart is null || _typography is null)
        {
            return;
        }

        if (_typography.FontFamily is not null)
        {
            _contentPart.FontFamily = _typography.FontFamily;
        }

        if (_typography.FontSize.HasValue)
        {
            _contentPart.EmSize = Math.Max(1, _typography.FontSize.Value);
        }

        if (_typography.FontWeight.HasValue)
        {
            _contentPart.FontWeight = _typography.FontWeight.Value;
        }

        if (_typography.FontStyle.HasValue)
        {
            _contentPart.FontStyle = _typography.FontStyle.Value;
        }

        if (_typography.FontStretch.HasValue)
        {
            _contentPart.FontStretch = _typography.FontStretch.Value;
        }
    }

    private void ApplyPalette(WidgetFluentPalette.RepeatButtonPalette palette, WidgetFluentPalette.LayoutPalette layout, WidgetVisualState state)
    {
        if (_borderPart is null)
        {
            return;
        }

        var background = _backgroundOverride ?? palette.Background.Get(state) ?? palette.Background.Normal;
        if (background is not null)
        {
            _borderPart.Background = background;
        }

        var border = _borderOverride ?? palette.Border.Get(state) ?? palette.Border.Normal;
        if (border is not null)
        {
            _borderPart.BorderBrush = border;
        }

        _borderPart.BorderThickness = new Thickness(1);
        _borderPart.Padding = Padding;
        _borderPart.CornerRadius = layout.ControlCornerRadius;

        var minWidth = Math.Max(MinWidth, 0);
        var desiredWidth = double.IsNaN(DesiredWidth) ? minWidth : Math.Max(DesiredWidth, minWidth);
        _borderPart.DesiredWidth = desiredWidth;

        if (_contentPart is not null)
        {
            var foreground = palette.Foreground.Get(state) ?? palette.Foreground.Normal;
            if (foreground is not null)
            {
                _contentPart.Foreground = foreground;
            }

            if (_typography is null)
            {
                var body = WidgetFluentPalette.Current.Text.Typography.Body;
                if (body.FontFamily is not null)
                {
                    _contentPart.FontFamily = body.FontFamily;
                }

                if (body.FontSize > 0)
                {
                    _contentPart.EmSize = body.FontSize;
                }

                _contentPart.FontWeight = body.FontWeight;
            }
        }
    }

    private void StartRepeating()
    {
        StopTimers();
        TriggerClick();

        if (Interval <= TimeSpan.Zero)
        {
            return;
        }

        if (Delay <= TimeSpan.Zero)
        {
            StartRepeatTimer();
        }
        else
        {
            _delayTimer = CreateTimer(Delay, DelayElapsed);
            _delayTimer.Start();
        }
    }

    private void DelayElapsed(object? sender, EventArgs e)
    {
        _delayTimer?.Stop();
        if (_delayTimer is not null)
        {
            _delayTimer.Tick -= DelayElapsed;
            _delayTimer = null;
        }

        StartRepeatTimer();
    }

    private void StartRepeatTimer()
    {
        _repeatTimer = CreateTimer(Interval, RepeatElapsed);
        _repeatTimer.Start();
    }

    private void RepeatElapsed(object? sender, EventArgs e)
    {
        TriggerClick();
    }

    private void StopRepeating()
    {
        StopTimers();
    }

    private void StopTimers()
    {
        if (_delayTimer is not null)
        {
            _delayTimer.Stop();
            _delayTimer.Tick -= DelayElapsed;
            _delayTimer = null;
        }

        if (_repeatTimer is not null)
        {
            _repeatTimer.Stop();
            _repeatTimer.Tick -= RepeatElapsed;
            _repeatTimer = null;
        }
    }

    private void TriggerClick()
    {
        if (IsEnabled)
        {
            base.OnClick();
        }
    }

    private static DispatcherTimer CreateTimer(TimeSpan interval, EventHandler handler)
    {
        var timer = new DispatcherTimer { Interval = interval };
        timer.Tick += handler;
        return timer;
    }
}
