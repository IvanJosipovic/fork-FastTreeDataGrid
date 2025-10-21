using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class SliderWidget : Widget
{
    private double _minimum = 0;
    private double _maximum = 1;
    private double _value;
    private bool _isDragging;
    private ImmutableSolidColorBrush? _trackBrush;
    private ImmutableSolidColorBrush? _fillBrush;
    private ImmutableSolidColorBrush? _thumbBrush;

    public SliderWidget()
    {
        IsInteractive = true;
    }

    public event EventHandler<WidgetValueChangedEventArgs<double>>? ValueChanged;

    public double Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }
            Value = _value;
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_maximum < _minimum)
            {
                _minimum = _maximum;
            }
            Value = _value;
        }
    }

    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (Math.Abs(_value - clamped) < double.Epsilon)
            {
                return;
            }

            var previous = _value;
            _value = clamped;
            ValueChanged?.Invoke(this, new WidgetValueChangedEventArgs<double>(this, previous, _value));
            RefreshStyle();
        }
    }

    public ImmutableSolidColorBrush? TrackBrush
    {
        get => _trackBrush;
        set => _trackBrush = value;
    }

    public ImmutableSolidColorBrush? FillBrush
    {
        get => _fillBrush;
        set => _fillBrush = value;
    }

    public ImmutableSolidColorBrush? ThumbBrush
    {
        get => _thumbBrush;
        set => _thumbBrush = value;
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        var min = _minimum;
        var max = _maximum;
        var value = _value;
        var enabled = IsEnabled;
        _trackBrush = TrackBrush;
        _fillBrush = FillBrush;
        _thumbBrush = ThumbBrush;

        if (provider is not null && Key is not null)
        {
            var data = provider.GetValue(item, Key);
            switch (data)
            {
                case SliderWidgetValue sliderValue:
                    min = sliderValue.Minimum;
                    max = sliderValue.Maximum;
                    value = sliderValue.Value;
                    enabled = sliderValue.IsEnabled;
                    _trackBrush = sliderValue.TrackBrush ?? TrackBrush;
                    _fillBrush = sliderValue.FillBrush ?? FillBrush;
                    _thumbBrush = sliderValue.ThumbBrush ?? ThumbBrush;
                    break;
                case double numeric:
                    value = numeric;
                    break;
            }
        }

        _minimum = min;
        _maximum = Math.Max(max, _minimum);
        IsEnabled = enabled;
        Value = value;
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);

        var trackHeight = Math.Min(6, rect.Height / 3);
        var trackY = rect.Y + (rect.Height - trackHeight) / 2;
        var trackRect = new Rect(rect.X, trackY, rect.Width, trackHeight);
        var radius = trackHeight / 2;

        var palette = WidgetFluentPalette.Current.Slider;

        var trackBrush = _trackBrush ?? palette.TrackFill.Get(VisualState) ?? palette.TrackFill.Normal ?? new ImmutableSolidColorBrush(Color.FromRgb(220, 220, 220));
        var fillBrush = _fillBrush ?? palette.ValueFill.Get(VisualState) ?? palette.ValueFill.Normal ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
        var thumbBrush = _thumbBrush ?? palette.ThumbFill.Get(VisualState) ?? palette.ThumbFill.Normal ?? new ImmutableSolidColorBrush(Color.FromRgb(255, 255, 255));
        var thumbBorder = palette.ThumbBorder.Get(VisualState) ?? palette.ThumbBorder.Normal ?? new ImmutableSolidColorBrush(Color.FromRgb(200, 200, 200));

        using var rotation = context.PushTransform(CreateRotationMatrix());
        context.DrawRectangle(trackBrush, null, trackRect, radius, radius);

        var percent = _maximum <= _minimum ? 0 : (_value - _minimum) / (_maximum - _minimum);
        percent = Math.Clamp(percent, 0, 1);

        if (percent > 0)
        {
            var fillWidth = Math.Max(2, trackRect.Width * percent);
            var fillRect = new Rect(trackRect.X, trackRect.Y, fillWidth, trackRect.Height);
            context.DrawRectangle(fillBrush, null, fillRect, radius, radius);
        }

        var thumbDiameter = Math.Max(trackHeight * 2.5, 14);
        var thumbX = trackRect.X + (trackRect.Width * percent) - thumbDiameter / 2;
        var thumbY = rect.Y + (rect.Height - thumbDiameter) / 2;
        thumbX = Math.Clamp(thumbX, rect.X, rect.Right - thumbDiameter);

        var thumbRect = new Rect(thumbX, thumbY, thumbDiameter, thumbDiameter);
        context.DrawEllipse(thumbBrush, new Pen(thumbBorder, 1), thumbRect.Center, thumbDiameter / 2, thumbDiameter / 2);
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsInteractive)
        {
            return handled;
        }

        if (!IsEnabled)
        {
            return handled;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Pressed:
                _isDragging = true;
                UpdateValueFromPoint(e.Position);
                break;
            case WidgetPointerEventKind.Moved:
                if (_isDragging)
                {
                    UpdateValueFromPoint(e.Position);
                }
                break;
            case WidgetPointerEventKind.Released:
                if (_isDragging)
                {
                    UpdateValueFromPoint(e.Position);
                }
                _isDragging = false;
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isDragging = false;
                break;
        }

        return true;
    }

    private void UpdateValueFromPoint(Point localPoint)
    {
        var width = Math.Max(1, Bounds.Width);
        var x = Math.Clamp(localPoint.X, 0, width);
        var percent = x / width;
        var newValue = _minimum + ((_maximum - _minimum) * percent);
        Value = newValue;
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
