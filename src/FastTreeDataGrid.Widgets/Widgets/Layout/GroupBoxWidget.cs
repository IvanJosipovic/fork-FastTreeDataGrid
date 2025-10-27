using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class GroupBoxWidget : HeaderedContentControlWidget
{
    static GroupBoxWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(GroupBoxWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not GroupBoxWidget groupBox)
                {
                    return;
                }

                var border = theme.Palette.Border;
                var layout = theme.Palette.Layout;
                var text = theme.Palette.Text;

                if (groupBox.BorderBrush is null)
                {
                    groupBox.BorderBrush = border.ControlBorder.Get(WidgetVisualState.Normal);
                }

                if (groupBox.BorderThickness == default)
                {
                    groupBox.BorderThickness = new Thickness(1);
                }

                if (groupBox.Background is null)
                {
                    groupBox.Background = new ImmutableSolidColorBrush(Colors.Transparent);
                }

                if (groupBox.CornerRadius == default)
                {
                    groupBox.CornerRadius = layout.ControlCornerRadius;
                }

                if (groupBox.Padding == default)
                {
                    groupBox.Padding = layout.ContentPadding;
                }

                if (groupBox.HeaderTextWidget is { } headerText)
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

    public GroupBoxWidget()
    {
        HeaderText = "Group";
    }

}
