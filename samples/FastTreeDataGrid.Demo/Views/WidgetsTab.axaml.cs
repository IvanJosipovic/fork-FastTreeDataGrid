using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Demo.ViewModels.Widgets;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

public partial class WidgetsTab : UserControl
{
    public WidgetsTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("WidgetsGrid") is { } grid)
        {
            ConfigureWidgetColumns(grid);
        }
    }

    private static void ConfigureWidgetColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 36;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Name",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 160,
            ValueKey = WidgetGalleryNode.KeyName,
            IsHierarchy = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Description",
            SizingMode = ColumnSizingMode.Star,
            MinWidth = 220,
            ValueKey = WidgetGalleryNode.KeyDescription,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Icon",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 80,
            MinWidth = 70,
            ValueKey = WidgetGalleryNode.KeyIcon,
            WidgetFactory = (_, _) => new IconWidget
            {
                Key = WidgetGalleryNode.KeyIcon,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                Padding = 10,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Geometry",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 100,
            MinWidth = 90,
            ValueKey = WidgetGalleryNode.KeyGeometry,
            WidgetFactory = (_, _) => new GeometryWidget
            {
                Key = WidgetGalleryNode.KeyGeometry,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(28, 124, 172)),
                Padding = 10,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Button",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 110,
            ValueKey = WidgetGalleryNode.KeyButton,
            WidgetFactory = (_, _) => new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyButton,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40)),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(242, 242, 242)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "CheckBox",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 90,
            MinWidth = 80,
            ValueKey = WidgetGalleryNode.KeyCheckBox,
            WidgetFactory = (_, _) => new CheckBoxWidget
            {
                Key = WidgetGalleryNode.KeyCheckBox,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Toggle",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 110,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyToggle,
            WidgetFactory = (_, item) =>
            {
                var toggle = new ToggleSwitchWidget
                {
                    Key = WidgetGalleryNode.KeyToggle,
                };

                if (item is WidgetGalleryNode node)
                {
                    toggle.Toggled += (_, args) =>
                    {
                        var isOn = args.NewValue;
                        if (node.ToggleValue is ToggleSwitchWidgetValue current)
                        {
                            node.ToggleValue = current with { IsOn = isOn };
                        }
                        else
                        {
                            node.ToggleValue = new ToggleSwitchWidgetValue(isOn);
                        }
                    };
                }

                return toggle;
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Badge",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyBadge,
            WidgetFactory = (_, _) => new BadgeWidget
            {
                Key = WidgetGalleryNode.KeyBadge,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Progress",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = WidgetGalleryNode.KeyProgress,
            WidgetFactory = (_, _) => new ProgressWidget
            {
                Key = WidgetGalleryNode.KeyProgress,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Custom Draw",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = WidgetGalleryNode.KeyCustom,
            WidgetFactory = (_, _) => new CustomDrawWidget
            {
                Key = WidgetGalleryNode.KeyCustom,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Layout",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 220,
            MinWidth = 200,
            ValueKey = WidgetGalleryNode.KeyLayout,
            WidgetFactory = (provider, item) =>
            {
                if (item is WidgetGalleryNode node && node.LayoutFactory is not null)
                {
                    return node.LayoutFactory();
                }

                if (provider?.GetValue(item, WidgetGalleryNode.KeyLayout) is Func<Widget> builder && builder is not null)
                {
                    return builder();
                }

                return null;
            },
        });
    }
}
