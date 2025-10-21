using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ProgressWidget : Widget
{
    private double _progress;
    private bool _isIndeterminate;
    private ImmutableSolidColorBrush? _foreground;
    private ImmutableSolidColorBrush? _background;

    public ImmutableSolidColorBrush? Background { get; set; }

    public double Progress
    {
        get => _progress;
        set => _progress = Math.Clamp(value, 0, 1);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => _isIndeterminate = value;
    }

    public ImmutableSolidColorBrush? TrackForeground
    {
        get => _foreground;
        set => _foreground = value;
    }

    public ImmutableSolidColorBrush? TrackBackground
    {
        get => _background;
        set => _background = value;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        _progress = 0;
        _isIndeterminate = false;
        _foreground = TrackForeground ?? Foreground;
        _background = TrackBackground ?? Background;

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case ProgressWidgetValue progressValue:
                Progress = progressValue.Progress;
                IsIndeterminate = progressValue.IsIndeterminate;
                TrackForeground = progressValue.Foreground ?? TrackForeground ?? Foreground;
                TrackBackground = progressValue.Background ?? TrackBackground ?? Background;
                break;
            case double numeric:
                Progress = numeric;
                break;
            case float numericFloat:
                Progress = numericFloat;
                break;
        }

        _foreground ??= Foreground;
        _background ??= Background;
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);

        var background = TrackBackground ?? new ImmutableSolidColorBrush(Color.FromRgb(230, 230, 230));
        var foreground = TrackForeground ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));

        var radius = Math.Min(rect.Height / 2, 6);

        using var rotation = context.PushTransform(CreateRotationMatrix());
        context.DrawRectangle(background, null, rect, radius, radius);

        if (IsIndeterminate)
        {
            DrawIndeterminate(context, rect, foreground, radius);
            return;
        }

        if (Progress <= 0)
        {
            return;
        }

        var progressWidth = Math.Max(0, rect.Width * Progress);
        if (progressWidth <= 0)
        {
            return;
        }

        var progressRect = new Rect(rect.X, rect.Y, progressWidth, rect.Height);
        context.DrawRectangle(foreground, null, progressRect, radius, radius);
    }

    private static void DrawIndeterminate(DrawingContext context, Rect rect, IBrush foreground, double radius)
    {
        var segmentWidth = Math.Max(12, rect.Width * 0.25);
        var offset = (rect.Width - segmentWidth) / 2;
        var indeterminateRect = new Rect(rect.X + offset, rect.Y, segmentWidth, rect.Height);
        context.DrawRectangle(foreground, null, indeterminateRect, radius, radius);
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
