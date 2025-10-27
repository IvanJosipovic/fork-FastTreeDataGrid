using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ExpanderWidget : HeaderedContentControlWidget
{
    private readonly GeometryWidget _indicator;
    private readonly StackLayoutWidget _headerRow;
    private Widget? _body;
    private bool _isExpanded = true;

    static ExpanderWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(ExpanderWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not ExpanderWidget expander)
                {
                    return;
                }

                var border = theme.Palette.Border;
                var text = theme.Palette.Text;
                var layout = theme.Palette.Layout;

                if (expander.BorderBrush is null)
                {
                    expander.BorderBrush = border.ControlBorder.Get(WidgetVisualState.Normal);
                }

                if (expander.BorderThickness == default)
                {
                    expander.BorderThickness = new Thickness(1);
                }

                if (expander.CornerRadius == default)
                {
                    expander.CornerRadius = layout.ControlCornerRadius;
                }

                if (expander.Padding == default)
                {
                    expander.Padding = layout.ContentPadding;
                }

                if (expander.HeaderTextWidget is { } headerText)
                {
                    if (headerText.Foreground is null && text.HeaderForeground is not null)
                    {
                        headerText.Foreground = text.HeaderForeground;
                    }

                    var headerTypography = text.Typography.Header;
                    if (headerTypography.FontFamily is not null && headerText.FontFamily != headerTypography.FontFamily)
                    {
                        headerText.FontFamily = headerTypography.FontFamily;
                    }

                    if (headerTypography.FontSize > 0 && Math.Abs(headerText.EmSize - headerTypography.FontSize) > double.Epsilon)
                    {
                        headerText.EmSize = headerTypography.FontSize;
                    }

                    if (headerText.FontWeight != headerTypography.FontWeight)
                    {
                        headerText.FontWeight = headerTypography.FontWeight;
                    }

                    if (text.HeaderMargin.HasValue)
                    {
                        headerText.Margin = text.HeaderMargin.Value;
                    }
                }
            }));
    }

    public ExpanderWidget()
    {
        IsInteractive = true;
        _indicator = new GeometryWidget
        {
            Stretch = Stretch.Uniform,
            DesiredWidth = 12,
            DesiredHeight = 12,
            Padding = 0,
        };
        _indicator.SetGeometry(StreamGeometry.Parse("M0,0 L8,0 L4,5 Z"));

        _headerRow = new StackLayoutWidget
        {
            Orientation = Orientation.Horizontal,
        };
        _headerRow.Children.Add(_indicator);

        Header = _headerRow;
        UpdateIndicator();
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            UpdateBodyVisibility();
            UpdateIndicator();
        }
    }

    public new Widget? Content
    {
        get => _body;
        set
        {
            _body = value;
            UpdateBodyVisibility();
        }
    }

    public new string? HeaderText
    {
        get => _headerRow.Children.Count > 1 && _headerRow.Children[1] is FormattedTextWidget text ? text.Text : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                if (_headerRow.Children.Count > 1)
                {
                    _headerRow.Children.RemoveAt(1);
                }

                return;
            }

            var headerTypography = WidgetFluentPalette.Current.Text.Typography.Header;
            FormattedTextWidget textWidget;
            if (_headerRow.Children.Count > 1 && _headerRow.Children[1] is FormattedTextWidget existing)
            {
                textWidget = existing;
            }
            else
            {
                textWidget = new FormattedTextWidget
                {
                    DesiredHeight = 20,
                };
                if (_headerRow.Children.Count > 1)
                {
                    _headerRow.Children[1] = textWidget;
                }
                else
                {
                    _headerRow.Children.Add(textWidget);
                }
            }

            if (headerTypography.FontFamily is not null)
            {
                textWidget.FontFamily = headerTypography.FontFamily;
            }

            textWidget.FontWeight = headerTypography.FontWeight;

            if (headerTypography.FontSize > 0)
            {
                textWidget.EmSize = headerTypography.FontSize;
            }

            textWidget.SetText(value);
        }
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsEnabled)
        {
            return handled;
        }

        if (e.Kind == WidgetPointerEventKind.Released && Bounds.Contains(e.Position))
        {
            IsExpanded = !IsExpanded;
            handled = true;
        }

        return handled;
    }

    private void UpdateBodyVisibility()
    {
        BodyHost.Children.Clear();
        if (_isExpanded && _body is not null)
        {
            BodyHost.Children.Add(_body);
        }
    }

    private void UpdateIndicator()
    {
        _indicator.Rotation = _isExpanded ? 180 : 0;
    }
}
