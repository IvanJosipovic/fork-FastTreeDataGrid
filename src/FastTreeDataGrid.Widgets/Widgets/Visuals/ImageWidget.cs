using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Utilities;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class ImageWidget : Widget
{
    public IImage? Source { get; set; }

    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public StretchDirection StretchDirection { get; set; } = StretchDirection.Both;

    public double Padding { get; set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        if (provider is null || Key is null)
        {
            switch (item)
            {
                case ImageWidgetValue imageValue:
                    ApplyValue(imageValue);
                    return;
            }

            return;
        }

        var value = provider.GetValue(item, Key);
        switch (value)
        {
            case ImageWidgetValue imageValue:
                ApplyValue(imageValue);
                break;
            case IImage image:
                Source = image;
                break;
            default:
                break;
        }
    }

    public override void Draw(DrawingContext context)
    {
        if (Source is not { } image)
        {
            return;
        }

        var sourceSize = TryGetImageSize(image);
        if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        using var transform = PushRenderTransform(context);

        var destRect = CalculateDestRect(Bounds, sourceSize, Padding, Stretch, StretchDirection);
        if (destRect.Width <= 0 || destRect.Height <= 0)
        {
            return;
        }

        var sourceRect = new Rect(sourceSize);
        context.DrawImage(image, sourceRect, destRect);
    }

    protected virtual void ApplyValue(ImageWidgetValue value)
    {
        Source = value.Source;
        Stretch = value.Stretch;
        StretchDirection = value.StretchDirection;
        Padding = value.Padding;
    }

    internal static Size TryGetImageSize(IImage image) => image.Size;

    internal static Rect CalculateDestRect(Rect bounds, Size sourceSize, double padding, Stretch stretch, StretchDirection stretchDirection)
    {
        var availableWidth = Math.Max(0, bounds.Width - (padding * 2));
        var availableHeight = Math.Max(0, bounds.Height - (padding * 2));

        if (availableWidth <= 0 || availableHeight <= 0 || sourceSize.Width <= 0 || sourceSize.Height <= 0)
        {
            return new Rect(bounds.X + padding, bounds.Y + padding, 0, 0);
        }

        var scaleX = availableWidth / sourceSize.Width;
        var scaleY = availableHeight / sourceSize.Height;

        (scaleX, scaleY) = ApplyStretch(scaleX, scaleY, stretch);
        scaleX = ApplyStretchDirection(scaleX, stretchDirection);
        scaleY = ApplyStretchDirection(scaleY, stretchDirection);

        var width = sourceSize.Width * scaleX;
        var height = sourceSize.Height * scaleY;

        if (stretch == Stretch.None)
        {
            width = Math.Min(sourceSize.Width, availableWidth);
            height = Math.Min(sourceSize.Height, availableHeight);
        }

        var x = bounds.X + padding + Math.Max(0, (availableWidth - width) / 2);
        var y = bounds.Y + padding + Math.Max(0, (availableHeight - height) / 2);

        return new Rect(x, y, Math.Max(0, width), Math.Max(0, height));
    }

    private static (double scaleX, double scaleY) ApplyStretch(double scaleX, double scaleY, Stretch stretch)
    {
        return stretch switch
        {
            Stretch.None => (1, 1),
            Stretch.Fill => (scaleX, scaleY),
            Stretch.Uniform => (MinFinite(scaleX, scaleY), MinFinite(scaleX, scaleY)),
            Stretch.UniformToFill => (MaxFinite(scaleX, scaleY), MaxFinite(scaleX, scaleY)),
            _ => (scaleX, scaleY)
        };
    }

    private static double ApplyStretchDirection(double scale, StretchDirection direction)
    {
        return direction switch
        {
            StretchDirection.DownOnly => Math.Min(scale, 1),
            StretchDirection.UpOnly => Math.Max(scale, 1),
            _ => scale,
        };
    }

    private static double MinFinite(double a, double b)
    {
        a = double.IsFinite(a) ? a : 1;
        b = double.IsFinite(b) ? b : 1;
        return Math.Min(a, b);
    }

    private static double MaxFinite(double a, double b)
    {
        a = double.IsFinite(a) ? a : 1;
        b = double.IsFinite(b) ? b : 1;
        return Math.Max(a, b);
    }
}
