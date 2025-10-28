using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Presenter rendering group footer labels (summary rows) for FastTreeDataGrid.
/// </summary>
public sealed class FastTreeDataGridGroupSummaryPresenter : Widget
{
    private const double MinFontSize = 11;
    private const double MaxFontSize = 16;

    private string _label = string.Empty;
    private double _indent;
    private double _trailingPadding;

    private FormattedText? _formatted;
    private ImmutableSolidColorBrush? _labelBrush;
    private double _widthConstraint = double.NaN;

    static FastTreeDataGridGroupSummaryPresenter()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(FastTreeDataGridGroupSummaryPresenter),
                WidgetVisualState.Normal,
                static (widget, theme) =>
                {
                    if (widget is not FastTreeDataGridGroupSummaryPresenter presenter)
                    {
                        return;
                    }

                    presenter.Foreground ??= theme.Palette.Text.Foreground.Get(WidgetVisualState.Normal);
                }));
    }

    /// <summary>
    /// Updates label and layout offsets for the presenter.
    /// </summary>
    /// <param name="label">Summary label text.</param>
    /// <param name="indent">Indent applied before the label.</param>
    /// <param name="padding">Trailing padding inside the cell.</param>
    public void Update(string? label, double indent, double padding)
    {
        _label = label ?? string.Empty;
        _indent = Math.Max(0, indent);
        _trailingPadding = Math.Max(0, padding);

        _formatted = null;
        _labelBrush = null;
        _widthConstraint = double.NaN;
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var startX = rect.X + _indent;
        var endX = rect.Right - _trailingPadding;
        if (endX <= startX)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);

        var palette = WidgetFluentPalette.Current;
        var brush = Foreground ?? palette.Text.Foreground.Get(WidgetVisualState.Normal) ?? new ImmutableSolidColorBrush(Color.FromRgb(33, 33, 33));
        var text = GetLabelText(brush, endX - startX, rect.Height);
        var origin = new Point(startX, rect.Y + Math.Max(0, (rect.Height - text.Height) / 2));

        context.DrawText(text, origin);
    }

    private FormattedText GetLabelText(ImmutableSolidColorBrush brush, double widthConstraint, double rowHeight)
    {
        var constraint = double.IsFinite(widthConstraint) ? Math.Max(0, widthConstraint) : double.PositiveInfinity;

        if (_formatted is not null &&
            ReferenceEquals(_labelBrush, brush) &&
            Math.Abs(_widthConstraint - constraint) < 0.5)
        {
            return _formatted;
        }

        var fontSize = Math.Clamp(rowHeight * 0.48, MinFontSize, MaxFontSize);
        var formatted = new FormattedText(
            _label,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            brush)
        {
            Trimming = TextTrimming.CharacterEllipsis
        };

        formatted.SetFontWeight(FontWeight.SemiBold);

        if (double.IsFinite(constraint))
        {
            formatted.MaxTextWidth = constraint;
        }

        _formatted = formatted;
        _labelBrush = brush;
        _widthConstraint = constraint;
        return formatted;
    }
}
