using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class BadgeWidget : Widget
{
    private string _text = string.Empty;
    private ImmutableSolidColorBrush? _background;
    private ImmutableSolidColorBrush? _foreground;
    private ImmutableSolidColorBrush? _backgroundOverride;
    private ImmutableSolidColorBrush? _foregroundOverride;
    private double _cornerRadius = 8;
    private double _padding = 6;
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

    public double CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = value;
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
        _cornerRadius = CornerRadius;
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
                    _cornerRadius = badge.CornerRadius;
                    _padding = badge.Padding;
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

        var background = _background ?? BackgroundBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
        var foreground = _foreground ?? ForegroundBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(255, 255, 255));

        var drawRect = rect.Deflate(_padding);
        if (drawRect.Width <= 0 || drawRect.Height <= 0)
        {
            drawRect = rect;
        }

        context.DrawRectangle(background, null, drawRect, _cornerRadius, _cornerRadius);

        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        var formatted = GetOrCreateText(foreground);
        var originX = drawRect.X + Math.Max(0, (drawRect.Width - formatted.Width) / 2);
        var originY = drawRect.Y + Math.Max(0, (drawRect.Height - formatted.Height) / 2);

        using var rotation = context.PushTransform(CreateRotationMatrix());
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

    private Matrix CreateRotationMatrix()
    {
        if (Math.Abs(Rotation) <= double.Epsilon)
        {
            return Matrix.Identity;
        }

        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        return Matrix.CreateTranslation(-centerX, -centerY)
               * Matrix.CreateRotation(Matrix.ToRadians(Rotation))
               * Matrix.CreateTranslation(centerX, centerY);
    }
}
