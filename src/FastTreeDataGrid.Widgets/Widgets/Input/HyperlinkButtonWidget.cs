using System;
using System.Windows.Input;
using Avalonia.Media;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Minimal button implementation that renders as hyperlink-styled text while honouring theme palettes.
/// </summary>
public sealed class HyperlinkButtonWidget : ButtonWidgetBase
{
    private AccessTextWidget? _content;
    private string _text = string.Empty;
    private WidgetTypography? _typography;

    static HyperlinkButtonWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(HyperlinkButtonWidget),
                    state,
                    (widget, theme) =>
                    {
                        if (widget is HyperlinkButtonWidget hyperlink)
                        {
                            hyperlink.ApplyPalette(theme.Palette.Text, state);
                        }
                    }));
        }
    }

    public string? Text
    {
        get => _text;
        set
        {
            var updated = value ?? string.Empty;
            if (string.Equals(_text, updated, StringComparison.Ordinal))
            {
                return;
            }

            _text = updated;
            if (_content is null && !IsTemplateApplied)
            {
                ApplyTemplate();
            }

            _content?.SetAccessText(_text);
            UpdateAutomationFromText(_text, _content);
        }
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

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
                _typography = buttonValue.Typography;
                SetAutomationSettings(buttonValue.Automation);
                return true;
            case string str:
                text = str;
                return true;
        }

        return false;
    }

    protected override Widget? CreateDefaultTemplate()
    {
        return new AccessTextWidget
        {
            Trimming = TextTrimming.None,
            TextAlignment = TextAlignment.Left,
            ClipToBounds = false
        };
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        _content = templateRoot as AccessTextWidget;
        _content?.SetAccessText(_text);
        UpdateAutomationFromText(_text, _content);
        RefreshStyle();
    }

    private void ApplyTypography()
    {
        if (_content is null || _typography is null)
        {
            return;
        }

        if (_typography.FontFamily is not null)
        {
            _content.FontFamily = _typography.FontFamily;
        }

        if (_typography.FontSize.HasValue)
        {
            _content.EmSize = Math.Max(1, _typography.FontSize.Value);
        }

        if (_typography.FontWeight.HasValue)
        {
            _content.FontWeight = _typography.FontWeight.Value;
        }

        if (_typography.FontStyle.HasValue)
        {
            _content.FontStyle = _typography.FontStyle.Value;
        }

        if (_typography.FontStretch.HasValue)
        {
            _content.FontStretch = _typography.FontStretch.Value;
        }
    }

    protected override void OnPointerPressed(in WidgetPointerEvent e)
    {
        base.OnPointerPressed(e);
        RefreshStyle();
    }

    protected override void OnPointerReleased(bool executedClick, in WidgetPointerEvent e)
    {
        base.OnPointerReleased(executedClick, e);
        RefreshStyle();
    }

    protected override void OnPointerCancelled()
    {
        base.OnPointerCancelled();
        RefreshStyle();
    }

    private void ApplyPalette(WidgetFluentPalette.TextPalette palette, WidgetVisualState state)
    {
        if (_content is null)
        {
            return;
        }

        var brush = palette.Hyperlink.Get(state) ?? palette.Hyperlink.Normal;
        if (brush is not null)
        {
            _content.Foreground = brush;
        }

        if (_typography is not null)
        {
            return;
        }

        var body = palette.Typography.Body;
        if (body.FontFamily is not null)
        {
            _content.FontFamily = body.FontFamily;
        }

        if (body.FontSize > 0 && Math.Abs(_content.EmSize - body.FontSize) > double.Epsilon)
        {
            _content.EmSize = body.FontSize;
        }

        _content.FontWeight = body.FontWeight;
    }
}
