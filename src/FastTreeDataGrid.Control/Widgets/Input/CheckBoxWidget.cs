using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;
using CheckBoxPalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.CheckBoxPalette;
using CheckBoxValuePalette = FastTreeDataGrid.Control.Theming.WidgetFluentPalette.CheckBoxValuePalette;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CheckBoxWidget : TemplatedWidget
{
    private bool? _value;
    private bool? _sourceValue;
    private BorderWidget? _outerBorderPart;
    private BorderWidget? _boxPart;
    private GeometryWidget? _glyphPart;

    static CheckBoxWidget()
    {
        foreach (WidgetVisualState state in Enum.GetValues(typeof(WidgetVisualState)))
        {
            WidgetStyleManager.Register(
                themeName: string.Empty,
                new WidgetStyleRule(
                    typeof(CheckBoxWidget),
                    state,
                    widget =>
                    {
                        if (widget is CheckBoxWidget checkBox)
                        {
                            checkBox.RefreshTemplateAppearance();
                        }
                    }));
        }
    }

    public CheckBoxWidget()
    {
        IsInteractive = true;
    }

    public event EventHandler<WidgetEventArgs>? Click;

    public event EventHandler<WidgetEventArgs>? Checked;

    public event EventHandler<WidgetEventArgs>? Unchecked;

    public event EventHandler<WidgetEventArgs>? Indeterminate;

    public event EventHandler<WidgetValueChangedEventArgs<bool?>>? IsCheckedChanged;

    public double StrokeThickness { get; set; } = double.NaN;

    public double Padding { get; set; } = double.NaN;

    public bool? Value => _value;

    public void SetValue(bool? value)
    {
        _value = value;
        _sourceValue = value;
        RefreshStyle();
    }

    protected override Widget? CreateDefaultTemplate()
    {
        var glyph = new GeometryWidget
        {
            Stretch = Stretch.Uniform,
            Padding = 0,
            ClipToBounds = false
        };
        var box = new BorderWidget
        {
            Child = glyph,
            ClipToBounds = true
        };

        var outer = new BorderWidget
        {
            ClipToBounds = true
        };

        var root = new SurfaceWidget();
        root.Children.Add(outer);
        root.Children.Add(box);

        _outerBorderPart = outer;
        _boxPart = box;
        _glyphPart = glyph;

        return root;
    }

    protected override void OnTemplateApplied(Widget? templateRoot)
    {
        RefreshTemplateAppearance();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        _value = null;
        _sourceValue = null;
        var enabled = true;

        if (provider is not null && Key is not null)
        {
            var data = provider.GetValue(item, Key);
            switch (data)
            {
                case CheckBoxWidgetValue checkBoxValue:
                    _value = checkBoxValue.IsChecked;
                    _sourceValue = _value;
                    enabled = checkBoxValue.IsEnabled;
                    break;
                case bool boolean:
                    _value = boolean;
                    _sourceValue = _value;
                    break;
                case null:
                    _value = null;
                    _sourceValue = null;
                    break;
            }
        }

        IsEnabled = enabled;
        RefreshStyle();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        if (!IsTemplateApplied)
        {
            return;
        }

        _outerBorderPart?.Arrange(bounds);

        var palette = WidgetFluentPalette.Current.CheckBox;
        var padding = GetEffectivePadding(palette);
        var contentRect = Deflate(bounds, padding);

        if (_boxPart is null)
        {
            return;
        }

        var boxSize = Math.Min(Math.Min(contentRect.Width, contentRect.Height), palette.BoxSize);
        if (boxSize <= 0)
        {
            _boxPart.Arrange(new Rect(contentRect.X, contentRect.Y, 0, 0));
            return;
        }

        var x = contentRect.X + (contentRect.Width - boxSize) / 2;
        var y = contentRect.Y + (contentRect.Height - boxSize) / 2;
        var boxRect = new Rect(x, y, boxSize, boxSize);
        _boxPart.Arrange(boxRect);
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsEnabled)
        {
            return handled;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Released:
                if (IsWithinBounds(e.Position))
                {
                    OnClick();
                }
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _value = _sourceValue;
                RefreshStyle();
                break;
        }

        return true;
    }

    private void OnClick()
    {
        if (!IsEnabled)
        {
            return;
        }

        ToggleValueInternal();
        Click?.Invoke(this, new WidgetEventArgs(this));
    }

    private void ToggleValueInternal()
    {
        var oldValue = _value;
        _value = _value switch
        {
            null => true,
            true => false,
            false => true,
        };

        _sourceValue = _value;
        RaiseCheckedEvents(oldValue, _value);
        RefreshStyle();
    }

    private void RaiseCheckedEvents(bool? oldValue, bool? newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        IsCheckedChanged?.Invoke(this, new WidgetValueChangedEventArgs<bool?>(this, oldValue, newValue));

        switch (newValue)
        {
            case true:
                Checked?.Invoke(this, new WidgetEventArgs(this));
                break;
            case false:
                Unchecked?.Invoke(this, new WidgetEventArgs(this));
                break;
            default:
                Indeterminate?.Invoke(this, new WidgetEventArgs(this));
                break;
        }
    }

    private void RefreshTemplateAppearance()
    {
        if (!IsTemplateApplied && !ApplyTemplate())
        {
            return;
        }

        var palette = WidgetFluentPalette.Current.CheckBox;
        var valuePalette = GetValuePalette(palette);

        if (_outerBorderPart is not null)
        {
            _outerBorderPart.Background = GetBrushForState(valuePalette.Background, VisualState) ?? new ImmutableSolidColorBrush(Colors.Transparent);
            _outerBorderPart.BorderBrush = GetBrushForState(valuePalette.Border, VisualState);
            var thickness = palette.StrokeThickness;
            _outerBorderPart.BorderThickness = thickness > 0 ? new Thickness(thickness) : default;
            _outerBorderPart.CornerRadius = palette.CornerRadius;
        }

        if (_boxPart is not null)
        {
            var strokeThickness = double.IsNaN(StrokeThickness) ? palette.StrokeThickness : StrokeThickness;
            _boxPart.Background = GetBrushForState(valuePalette.BoxFill, VisualState) ?? new ImmutableSolidColorBrush(Colors.Transparent);
            _boxPart.BorderBrush = GetBrushForState(valuePalette.BoxStroke, VisualState);
            _boxPart.BorderThickness = strokeThickness > 0 ? new Thickness(strokeThickness) : default;
            _boxPart.CornerRadius = palette.CornerRadius;
        }

        if (_glyphPart is not null)
        {
            _glyphPart.Stretch = Stretch.Uniform;
            _glyphPart.Padding = 0;

            var glyphBrush = Foreground ?? GetBrushForState(valuePalette.Glyph, VisualState);
            if (glyphBrush is null)
            {
                glyphBrush = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40));
            }

            if (valuePalette.GlyphGeometry is not null)
            {
                _glyphPart.SetGeometry(valuePalette.GlyphGeometry, Stretch.Uniform, glyphBrush, null, 0);
            }
            else
            {
                _glyphPart.SetGeometry(null);
            }
        }
    }

    private CheckBoxValuePalette GetValuePalette(CheckBoxPalette palette)
    {
        return _value switch
        {
            true => palette.Checked,
            null => palette.Indeterminate,
            _ => palette.Unchecked,
        };
    }

    private static ImmutableSolidColorBrush? GetBrushForState(WidgetFluentPalette.BrushState state, WidgetVisualState visualState)
    {
        return state.Get(visualState) ?? state.Normal;
    }

    private Thickness GetEffectivePadding(CheckBoxPalette palette)
    {
        var padding = palette.Padding;
        if (!double.IsNaN(Padding) && Padding > 0)
        {
            var extra = Padding;
            padding = new Thickness(
                padding.Left + extra,
                padding.Top + extra,
                padding.Right + extra,
                padding.Bottom + extra);
        }

        return padding;
    }

    private static Rect Deflate(Rect rect, Thickness padding)
    {
        if (padding == default)
        {
            return rect;
        }

        var left = Math.Max(0, padding.Left);
        var top = Math.Max(0, padding.Top);
        var right = Math.Max(0, padding.Right);
        var bottom = Math.Max(0, padding.Bottom);

        var width = Math.Max(0, rect.Width - left - right);
        var height = Math.Max(0, rect.Height - top - bottom);
        return new Rect(rect.X + left, rect.Y + top, width, height);
    }

    private bool IsWithinBounds(Point position)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        return rect.Contains(position);
    }

}
