using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class TextBlockWidget : FormattedTextWidget
{
}

public sealed class LabelWidget : TextBlockWidget
{
    static LabelWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(LabelWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not LabelWidget label)
                {
                    return;
                }

                var textPalette = theme.Palette.Text;
                if (label.Foreground is null && textPalette.Foreground.Get(WidgetVisualState.Normal) is { } brush)
                {
                    label.Foreground = brush;
                }
            }));
    }
}

public class SelectableTextWidget : FormattedTextWidget
{
    private static readonly ImmutableSolidColorBrush DefaultSelectionBrush = new(Color.FromArgb(96, 99, 102, 241));
    private static readonly ImmutableSolidColorBrush DefaultCaretBrush = new(Color.FromRgb(30, 64, 175));

    static SelectableTextWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(SelectableTextWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not SelectableTextWidget selectable)
                {
                    return;
                }

                var textPalette = theme.Palette.Text;
                if (ReferenceEquals(selectable.SelectionBrush, DefaultSelectionBrush) && textPalette.SelectionHighlight is { } highlight)
                {
                    selectable.SelectionBrush = highlight;
                }

                if (ReferenceEquals(selectable.CaretBrush, DefaultCaretBrush) && textPalette.CaretBrush is not null)
                {
                    selectable.CaretBrush = textPalette.CaretBrush;
                }
            }));
    }

    protected int _anchorIndex;
    protected int _selectionIndex;
    protected int _caretIndex;
    private bool _isPointerDown;

    public SelectableTextWidget()
    {
        IsInteractive = true;
    }

    public ImmutableSolidColorBrush SelectionBrush { get; set; } = DefaultSelectionBrush;

    public ImmutableSolidColorBrush CaretBrush { get; set; } = DefaultCaretBrush;

    public double CaretWidth { get; set; } = 1.5;

    public int SelectionStart => Math.Min(_anchorIndex, _selectionIndex);

    public int SelectionEnd => Math.Max(_anchorIndex, _selectionIndex);

    public int SelectionLength => Math.Max(0, SelectionEnd - SelectionStart);

    public bool HasSelection => SelectionLength > 0;

    public void SetSelection(int start, int length)
    {
        var text = Text ?? string.Empty;
        start = Math.Clamp(start, 0, text.Length);
        length = Math.Clamp(length, 0, text.Length - start);
        _anchorIndex = start;
        _selectionIndex = start + length;
        _caretIndex = _selectionIndex;
        Invalidate();
    }

    public string GetSelectedText()
    {
        if (!HasSelection || Text is null)
        {
            return string.Empty;
        }

        var start = SelectionStart;
        return Text.Substring(start, SelectionLength);
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (value is SelectableTextWidgetValue selectable)
            {
                SetText(selectable.Text);
                if (selectable.SelectionStart.HasValue || selectable.SelectionLength.HasValue)
                {
                    SetSelection(selectable.SelectionStart ?? 0, selectable.SelectionLength ?? 0);
                }
                else
                {
                    _anchorIndex = _selectionIndex = _caretIndex = Math.Min(Text?.Length ?? 0, _caretIndex);
                    Invalidate();
                }
                return;
            }
        }

        base.UpdateValue(provider, item);
        _anchorIndex = _selectionIndex = _caretIndex = Math.Min(Text?.Length ?? 0, _caretIndex);
        Invalidate();
    }

    public override void Draw(DrawingContext context)
    {
        var formatted = EnsureFormattedText();
        if (formatted is null)
        {
            return;
        }

        using var clip = PushClip(context);
        using var transform = PushRenderTransform(context);
        var origin = GetTextOrigin(formatted);

        DrawSelection(context, formatted, origin);
        DrawFormattedText(context, formatted, origin);
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        base.HandlePointerEvent(e);

        if (!IsEnabled)
        {
            return false;
        }

        switch (e.Kind)
        {
            case WidgetPointerEventKind.Pressed:
                _isPointerDown = true;
                MoveCaretToPoint(e.Position, resetAnchor: true);
                return true;
            case WidgetPointerEventKind.Moved:
                if (_isPointerDown)
                {
                    MoveCaretToPoint(e.Position, resetAnchor: false);
                    return true;
                }
                break;
            case WidgetPointerEventKind.Released:
                if (_isPointerDown)
                {
                    MoveCaretToPoint(e.Position, resetAnchor: false);
                    _isPointerDown = false;
                    return true;
                }
                break;
            case WidgetPointerEventKind.Cancelled:
            case WidgetPointerEventKind.CaptureLost:
                _isPointerDown = false;
                break;
        }

        return IsInteractive;
    }

    protected virtual void MoveCaretToPoint(Point position, bool resetAnchor)
    {
        if (Text is null)
        {
            return;
        }

        var formatted = EnsureFormattedText();
        if (formatted is null)
        {
            return;
        }

        var origin = GetTextOrigin(formatted);
        var index = HitTestIndex(position, formatted, origin);

        if (resetAnchor)
        {
            _anchorIndex = index;
        }

        _selectionIndex = index;
        _caretIndex = index;
        Invalidate();
    }

    protected virtual void DrawSelection(DrawingContext context, FormattedText formatted, Point origin)
    {
        if (HasSelection && SelectionBrush is not null)
        {
            var geometry = formatted.BuildHighlightGeometry(origin, SelectionStart, SelectionLength);
            if (geometry is not null)
            {
                context.DrawGeometry(SelectionBrush, null, geometry);
            }
        }

        if (CaretBrush is null)
        {
            return;
        }

        DrawCaret(context, formatted, origin);
    }

    protected virtual void DrawCaret(DrawingContext context, FormattedText formatted, Point origin)
    {
        if (Text is null)
        {
            return;
        }

        Geometry? caretGeometry = formatted.BuildHighlightGeometry(origin, _caretIndex, 0);

        if (caretGeometry is { } g && g.Bounds.Width >= 0)
        {
            var bounds = g.Bounds;
            var caretRect = new Rect(bounds.Left, bounds.Top, CaretWidth, Math.Max(1, bounds.Height));
            context.FillRectangle(CaretBrush, caretRect);
            return;
        }

        if (_caretIndex > 0)
        {
            var geo = formatted.BuildHighlightGeometry(origin, _caretIndex - 1, 1);
            if (geo is not null)
            {
                var bounds = geo.Bounds;
                var caretRect = new Rect(bounds.Right, bounds.Top, CaretWidth, Math.Max(1, bounds.Height));
                context.FillRectangle(CaretBrush, caretRect);
                return;
            }
        }

        var fallback = new Rect(origin.X, origin.Y, CaretWidth, Math.Max(1, formatted.Height));
        if (_caretIndex >= Text.Length)
        {
            fallback = fallback.WithX(origin.X + formatted.Width);
        }
        context.FillRectangle(CaretBrush, fallback);
    }

    protected int HitTestIndex(Point position, FormattedText formatted, Point origin)
    {
        if (Text is null)
        {
            return 0;
        }

        var absolute = new Point(Bounds.X + position.X, Bounds.Y + position.Y);
        var text = Text;

        if (text.Length == 0)
        {
            return 0;
        }

        var bestIndex = 0;
        var bestDistance = double.PositiveInfinity;

        for (var i = 0; i <= text.Length; i++)
        {
            Rect caretRect;

            if (i == text.Length)
            {
                var geo = formatted.BuildHighlightGeometry(origin, text.Length - 1, 1);
                if (geo is null)
                {
                    caretRect = new Rect(origin.X + formatted.Width, origin.Y, 0, formatted.Height);
                }
                else
                {
                    var bounds = geo.Bounds;
                    caretRect = new Rect(bounds.Right, bounds.Top, 0, bounds.Height);
                }
            }
            else
            {
                var geo = formatted.BuildHighlightGeometry(origin, i, 1);
                if (geo is null)
                {
                    continue;
                }

                var bounds = geo.Bounds;
                caretRect = new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
            }

            var distance = DistanceSquaredToRect(absolute, caretRect);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return Math.Clamp(bestIndex, 0, text.Length);
    }

    protected static double DistanceSquaredToRect(Point point, Rect rect)
    {
        var clampedX = Math.Clamp(point.X, rect.Left, rect.Right);
        var clampedY = Math.Clamp(point.Y, rect.Top, rect.Bottom);
        var dx = point.X - clampedX;
        var dy = point.Y - clampedY;
        return (dx * dx) + (dy * dy);
    }

    protected void SetCaretPosition(int caretIndex, bool resetAnchor)
    {
        var text = Text ?? string.Empty;
        var clamped = Math.Clamp(caretIndex, 0, text.Length);
        if (resetAnchor)
        {
            _anchorIndex = clamped;
        }

        _selectionIndex = clamped;
        _caretIndex = clamped;
        Invalidate();
    }

    protected void SetSelectionInternal(int anchor, int caret)
    {
        var text = Text ?? string.Empty;
        _anchorIndex = Math.Clamp(anchor, 0, text.Length);
        _selectionIndex = Math.Clamp(caret, 0, text.Length);
        _caretIndex = _selectionIndex;
        Invalidate();
    }
}

public sealed class DocumentTextWidget : FormattedTextWidget
{
    private DocumentTextWidgetValue? _currentDocument;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (value is DocumentTextWidgetValue document)
            {
                ApplyDocument(document);
                return;
            }
        }

        if (item is DocumentTextWidgetValue doc)
        {
            ApplyDocument(doc);
            return;
        }

        _currentDocument = null;
        base.UpdateValue(provider, item);
    }

    public void SetDocument(DocumentTextWidgetValue document)
    {
        ApplyDocument(document);
    }

    private void ApplyDocument(DocumentTextWidgetValue document)
    {
        _currentDocument = document;

        var spans = document.Spans ?? Array.Empty<DocumentTextSpan>();

        var text = string.Empty;
        foreach (var span in spans)
        {
            text += span.Text ?? string.Empty;
        }

        SetText(text);
        var formatted = EnsureFormattedText();
        if (formatted is null)
        {
            return;
        }

        var offset = 0;
        foreach (var span in spans)
        {
            var spanText = span.Text ?? string.Empty;
            var length = spanText.Length;

            if (span.Foreground is { } foreground)
            {
                formatted.SetForegroundBrush(foreground, offset, length);
            }

            if (span.FontWeight.HasValue)
            {
                formatted.SetFontWeight(span.FontWeight.Value, offset, length);
            }

            if (span.FontStyle.HasValue)
            {
                formatted.SetFontStyle(span.FontStyle.Value, offset, length);
            }

            offset += length;
        }

        Invalidate();
    }
}

public readonly record struct DocumentTextSpan(
    string Text,
    ImmutableSolidColorBrush? Foreground = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null);
