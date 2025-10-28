using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Lightweight presenter used to render grouped header rows inside the FastTreeDataGrid.
/// </summary>
public sealed class FastTreeDataGridGroupRowPresenter : Widget
{
    private const double MinFontSize = 10;
    private const double MaxFontSize = 20;
    private const double BadgeHorizontalPadding = 8;
    private const double BadgeVerticalPadding = 4;
    private const double BadgeCornerRadius = 9;
    private const double BadgeSpacing = 8;

    private string _headerText = string.Empty;
    private double _indent;
    private double _trailingPadding;
    private int _itemCount;

    private FormattedText? _headerFormatted;
    private ImmutableSolidColorBrush? _headerBrush;
    private double _headerWidthConstraint = double.NaN;

    private FormattedText? _countFormatted;
    private ImmutableSolidColorBrush? _countBrush;
    private int _countValue = int.MinValue;

    private ImmutableSolidColorBrush? _badgeBackground;
    private ImmutableSolidColorBrush? _badgeForeground;

    static FastTreeDataGridGroupRowPresenter()
    {
        WidgetStyleManager.Register(
            string.Empty,
            new WidgetStyleRule(
                typeof(FastTreeDataGridGroupRowPresenter),
                WidgetVisualState.Normal,
                static (widget, theme) =>
                {
                    if (widget is not FastTreeDataGridGroupRowPresenter presenter)
                    {
                        return;
                    }

                    presenter.Foreground ??= theme.Palette.Text.Foreground.Get(WidgetVisualState.Normal);
                    presenter._badgeBackground ??= theme.Palette.Badge.Background;
                    presenter._badgeForeground ??= theme.Palette.Badge.Foreground;
                }));
    }

    /// <summary>
    /// Updates the presenter with the current group metadata.
    /// </summary>
    /// <param name="headerText">Display text for the group.</param>
    /// <param name="itemCount">Number of rows contained in the group.</param>
    /// <param name="indent">Horizontal offset that accounts for hierarchy indentation and toggle glyph.</param>
    /// <param name="padding">Trailing padding applied by the column cell layout.</param>
    public void Update(string? headerText, int itemCount, double indent, double padding)
    {
        _headerText = headerText ?? string.Empty;
        _itemCount = Math.Max(0, itemCount);
        _indent = Math.Max(0, indent);
        _trailingPadding = Math.Max(0, padding);

        _headerFormatted = null;
        _headerBrush = null;
        _headerWidthConstraint = double.NaN;

        _countFormatted = null;
        _countBrush = null;
        _countValue = int.MinValue;
    }

    public override void Draw(DrawingContext context)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = PushClip(context);
        using var rotation = PushRenderTransform(context);

        var palette = WidgetFluentPalette.Current;
        var textBrush = Foreground ?? palette.Text.Foreground.Get(WidgetVisualState.Normal) ?? new ImmutableSolidColorBrush(Color.FromRgb(33, 33, 33));
        var badgeBackground = _badgeBackground ?? palette.Badge.Background ?? new ImmutableSolidColorBrush(Color.FromRgb(224, 230, 238));
        var badgeForeground = _badgeForeground ?? palette.Badge.Foreground ?? new ImmutableSolidColorBrush(Color.FromRgb(33, 33, 33));

        var startX = rect.X + _indent;
        var endX = rect.Right - _trailingPadding;
        if (endX <= startX)
        {
            return;
        }

        var availableWidth = endX - startX;
        FormattedText? countText = null;
        double badgeWidth = 0;
        double badgeHeight = 0;

        if (_itemCount > 0 && availableWidth > 0)
        {
            countText = GetCountText(badgeForeground, rect.Height);
            badgeHeight = Math.Min(
                Math.Max(0, rect.Height - (BadgeVerticalPadding * 2)),
                Math.Max(18, rect.Height * 0.6));
            badgeWidth = Math.Max(countText.Width + (BadgeHorizontalPadding * 2), badgeHeight);
            availableWidth = Math.Max(0, availableWidth - badgeWidth - BadgeSpacing);
        }

        var headerText = GetHeaderText(textBrush, availableWidth);
        var headerY = rect.Y + Math.Max(0, (rect.Height - headerText.Height) / 2);
        context.DrawText(headerText, new Point(startX, headerY));

        if (countText is null)
        {
            return;
        }

        var badgeX = endX - badgeWidth;
        var badgeY = rect.Y + Math.Max(BadgeVerticalPadding, (rect.Height - badgeHeight) / 2);
        var badgeRect = new Rect(badgeX, badgeY, badgeWidth, badgeHeight);

        context.DrawRectangle(badgeBackground, null, badgeRect, BadgeCornerRadius, BadgeCornerRadius);

        var countOrigin = new Point(
            badgeRect.X + Math.Max(0, (badgeRect.Width - countText.Width) / 2),
            badgeRect.Y + Math.Max(0, (badgeRect.Height - countText.Height) / 2));

        context.DrawText(countText, countOrigin);
    }

    private FormattedText GetHeaderText(ImmutableSolidColorBrush brush, double widthConstraint)
    {
        var constraint = double.IsFinite(widthConstraint) ? Math.Max(0, widthConstraint) : double.PositiveInfinity;

        if (_headerFormatted is not null &&
            ReferenceEquals(_headerBrush, brush) &&
            Math.Abs(_headerWidthConstraint - constraint) < 0.5)
        {
            return _headerFormatted;
        }

        var fontSize = Math.Clamp(Bounds.Height - 10, MinFontSize, MaxFontSize);
        var formatted = new FormattedText(
            _headerText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            brush)
        {
            Trimming = TextTrimming.CharacterEllipsis
        };

        if (double.IsFinite(constraint))
        {
            formatted.MaxTextWidth = constraint;
        }

        _headerFormatted = formatted;
        _headerBrush = brush;
        _headerWidthConstraint = constraint;
        return formatted;
    }

    private FormattedText GetCountText(ImmutableSolidColorBrush brush, double rowHeight)
    {
        if (_itemCount == _countValue && ReferenceEquals(_countBrush, brush) && _countFormatted is not null)
        {
            return _countFormatted;
        }

        var text = _itemCount.ToString("N0", CultureInfo.CurrentCulture);
        var fontSize = Math.Clamp(rowHeight - 12, MinFontSize - 2, MaxFontSize - 2);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            brush);

        _countFormatted = formatted;
        _countBrush = brush;
        _countValue = _itemCount;
        return formatted;
    }
}
