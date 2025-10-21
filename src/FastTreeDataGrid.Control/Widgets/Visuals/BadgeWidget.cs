using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class BadgeWidget : Widget
{
    private string _text = string.Empty;
    private ImmutableSolidColorBrush? _background;
    private ImmutableSolidColorBrush? _foreground;
    private ImmutableSolidColorBrush? _backgroundOverride;
    private ImmutableSolidColorBrush? _foregroundOverride;
    private double _padding = double.NaN;
    private FormattedText? _formattedText;
    private string? _cachedText;

    public ImmutableSolidColorBrush? BackgroundBrush
    {
        get => _backgroundOverride;
        set
        {
            _backgroundOverride = value;
            _background = value;
        }
    }

    public ImmutableSolidColorBrush? ForegroundBrush
    {
        get => _foregroundOverride;
        set
        {
            _foregroundOverride = value;
            _foreground = value;
        }
    }

    public double Padding
    {
        get => _padding;
        set => _padding = value;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _text = string.Empty;
        _background = _backgroundOverride;
        _foreground = _foregroundOverride;
        CornerRadius = default;
        _padding = Padding;

        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            switch (value)
            {
                case BadgeWidgetValue badge:
                    _text = badge.Text ?? string.Empty;
                    _background = badge.Background ?? BackgroundBrush;
                    _foreground = badge.Foreground ?? ForegroundBrush;
                    if (badge.CornerRadius.HasValue)
                    {
                        CornerRadius = new CornerRadius(badge.CornerRadius.Value);
                    }
                    _padding = badge.Padding ?? _padding;
                    break;
                case string text:
                    _text = text;
                    break;
            }
        }

        _formattedText = null;
        _cachedText = null;
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);

        var palette = WidgetFluentPalette.Current.Badge;
        var background = _background ?? BackgroundBrush ?? palette.Background;
        var foreground = _foreground ?? ForegroundBrush ?? palette.Foreground;

        var padding = double.IsNaN(_padding) ? palette.Padding : _padding;
        var drawRect = rect.Deflate(padding);
        if (drawRect.Width <= 0 || drawRect.Height <= 0)
        {
            drawRect = rect;
        }

        var corner = CornerRadius == default ? palette.CornerRadius : CornerRadius.TopLeft;
        context.DrawRectangle(background, null, drawRect, corner, corner);

        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        var formatted = GetOrCreateText(foreground);
        var originX = drawRect.X + Math.Max(0, (drawRect.Width - formatted.Width) / 2);
        var originY = drawRect.Y + Math.Max(0, (drawRect.Height - formatted.Height) / 2);

        using var rotation = PushRenderTransform(context);
        context.DrawText(formatted, new Point(originX, originY));
    }

    public void SetText(string text)
    {
        _text = text ?? string.Empty;
        _formattedText = null;
    }

    private FormattedText GetOrCreateText(ImmutableSolidColorBrush foreground)
    {
        if (_formattedText is not null && string.Equals(_cachedText, _text, StringComparison.Ordinal))
        {
            return _formattedText;
        }

        _formattedText = new FormattedText(
            _text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            Math.Max(12, Bounds.Height * 0.5),
            foreground);

        _cachedText = _text;
        return _formattedText;
    }

}
